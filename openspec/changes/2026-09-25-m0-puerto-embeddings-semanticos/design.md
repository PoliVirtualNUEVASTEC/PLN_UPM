# Design: M0 — Puerto de embeddings semánticos de oraciones (`ISentenceEmbedder`)

## Technical Approach

Extensión **puramente aditiva** de `NpcAi.Core`, mismo molde que v2 (`IClinicalResponder`) y
v3 (`IRequirementResponder`): un puerto y un tipo de valor nuevos; `Contract.Version` `3 → 4`;
entrada `## v4` en el changelog. Ningún tipo de v1–v3 cambia un byte (Opción 2 de la
exploración: puerto separado, `IIntentClassifier`/`IntentResult` intactos).

La implementación real (M2: `BertIntentClassifier`, `train.py`, re-export del `.onnx`) y el
consumo (M15, luego M16) son cambios SDD posteriores de un solo módulo cada uno.

### Unidad de módulo y grafo de referencias (regla `rules.design`)

    NpcAi.Core        ← único ensamblado de runtime tocado (M0)
    NpcAi.Core.Tests  ← único ensamblado de pruebas tocado

Sin cambios de referencias. Los dos tipos nuevos son C# puro (`float[]`, `string`, `bool`,
`System.BitConverter`): `noEngineReferences: true` se mantiene y `CoreAssemblyPurityTests` pasa
sin edición. `NpcAi.CoreChannels` **no** gana canal (sin consumidor, igual que AD8 de v3).

## Architecture Decisions

| # | Decisión | Elección | Alternativas rechazadas | Razón |
|---|---|---|---|---|
| AD1 | Dimensión del embedding | **Definida por la implementación**: el contrato no declara constante; el vector expone su `Length` en runtime. Invariante exigible: **consistencia** — todo vector no vacío de una misma instancia tiene la misma `Length` (implica: mismo texto ⇒ misma `Length`) | Constante fija en `Core` (768); propiedad `int Dimension` en el puerto | Mismo principio que v1/v2/v3: lo que es dato o detalle de un módulo no se congela en `Core` ("el número de casos clínicos es dato de M14, nunca un cambio de contrato"). `hidden_size` depende del `--encoder` de cada reentrenamiento (768 DistilBERT, 384 MiniLM): fijarlo convertiría un reentrenamiento en un cambio de contrato. `Dimension` en el puerto es redundante con `Length` y no tiene valor definido cuando `IsReady == false`. Lo que un consumidor necesita para coseno es que dos vectores de la misma instancia sean comparables, no un valor absoluto |
| AD2 | Determinismo | **Total, bit a bit**: mismo texto en la misma instancia ⇒ vector con los mismos bits en cada componente | Tolerancia (`ε` por componente o coseno ≥ 1-ε); sin cláusula (como `Confidence`) | `Confidence`/`LatencyMs` se eximieron en v1 porque son metadatos de medición; el embedding es la **señal primaria** que un consumidor determinista (`IClinicalResponder.Respond`, determinismo total desde v2) usaría para emparejar. Con tolerancia, un umbral de similitud cerca del borde podría cambiar `Handled` entre turnos y el determinismo aguas abajo sería inalcanzable. La tolerancia además exige elegir `ε` sin datos. Mitigación ya existente: `BertIntentClassifier` fuerza `BackendType.CPU` para evitar el orden de reducción no garantizado de GPU; el mismo patrón aplica. Alcance: por instancia (mismo modelo cargado, mismo dispositivo), **no** entre dispositivos (PC vs Quest) |
| AD3 | Forma del puerto | Puerto nuevo `ISentenceEmbedder` con `bool IsReady` y `SentenceEmbedding Embed(string text)` | Campo en `IntentResult`; puerto combinado Intent+Tone+Embedding | Ver exploración (Opciones 1 y 3): extender `IntentResult` acopla a M4/M6/M15/M16 y obliga a los dobles de M2 a fabricar vectores; el puerto combinado agrega superficie sin resolver nada que el adaptador no resuelva cacheando |
| AD4 | Parámetro de entrada | `string text` | `Utterance` | Espeja `IIntentClassifier.Classify(string)`. Decisivo: el consumidor necesita embeber **también** los hechos del caso (M14/`Data/`), que son `string`, no enunciados transcritos |
| AD5 | "No listo" y entrada vacía | `IsReady` nunca lanza; `Embed` nunca lanza y devuelve `SentenceEmbedding.Empty` si `IsReady == false` (DEBE) o si el texto es `null`/vacío/solo espacios | Lanzar; `DEBERIA` como en `Classify` | Mismo molde que todos los puertos (`IntentResult.Unknown`, `ClinicalResponse.NoAplica`). Se eleva a DEBE porque un vector "de relleno" (p. ej. ceros) sería indistinguible de uno real para un consumidor de coseno |
| AD6 | Tipo portador | `readonly struct SentenceEmbedding` que **copia** el arreglo al construir y nunca lo expone: `Length`, indexador de solo lectura, `IsEmpty`, `Empty`, `ToArray()` (copia defensiva) | `float[]` crudo; `ReadOnlyMemory<float>`; `IReadOnlyList<float>` | `float[]` es un tipo de referencia mutable: si M2 cachea el vector por texto (mitigación prevista del doble `Schedule()`), un consumidor que lo mute corrompe llamadas posteriores y rompe AD2 en silencio. `ReadOnlyMemory<float>` se "desenvuelve" con `MemoryMarshal.TryGetArray` y no tiene precedente en `Core`. `IReadOnlyList<float>` implica despacho por interfaz en el bucle de coseno. El `readonly struct` sigue la convención de DTO del repo; la única desviación (campo **privado** en vez de `public readonly`) es obligada, porque un campo `public readonly float[]` sigue exponiendo el contenido mutable |
| AD7 | Igualdad | `IEquatable<SentenceEmbedding>` completa (`Equals`, `GetHashCode`, `==`, `!=`) **bit a bit** (`BitConverter.SingleToInt32Bits`); `Empty == default` | Igualdad estructural por defecto (política G12 de DTO) | La igualdad por defecto de un struct con un campo arreglo compara **referencias**: dos vectores idénticos serían "distintos", una trampa activa. Bit a bit es AD2 hecho ejecutable (y distingue `0f` de `-0f`, coherente con "mismos bits"). Precedente: los tipos con identidad de valor (`PersonalityId`, `ClinicalCaseId`, `RequirementId`) ya tienen igualdad completa |
| AD8 | Normalización y finitud | Componentes **finitos** (sin `NaN`/`±Inf`) cuando el vector no está vacío. Normalización L2 **no** exigida | Exigir norma 1; no decir nada | Un `NaN` envenena cualquier coseno. Exigir norma 1 restringe a M2 sin necesidad: el consumidor calcula coseno completo |
| AD9 | Ubicación | `Runtime/Core/SentenceEmbedding.cs` (archivo propio); puerto en `Ports.cs`, sección "Comprension", **inmediatamente después** de `IIntentClassifier` | Dentro de `Dtos.cs`; sección nueva al final | Los tipos con igualdad completa viven en archivo propio (`ClinicalCaseId.cs`, `RequirementId.cs`). El puerto es de M2 y se lee junto al otro puerto de M2; inserción pura, nada se reordena |
| AD10 | Metadatos de medición | Sin `LatencyMs` ni confianza en `SentenceEmbedding` | Copiar `LatencyMs` de `IntentResult` | Un campo de medición no determinista dentro del tipo haría falsa la igualdad bit a bit de AD7. M2 puede medir latencia por su cuenta (M13) |
| AD11 | Cómo se ejerce la base sin M2 | Dos stubs locales (no-listo y mínimo-listo) + subclase concreta, en el mismo archivo | Solo stub no-listo | Igual que AD9 de v3: sin stub listo, los casos protegidos por `Assume` quedan omitidos, no verdes |

## Data Flow

    (futuro, NO es de este cambio)
    M2 impl. real ── implementa ──► ISentenceEmbedder
                                        │ Embed(texto del turno)   ─► SentenceEmbedding
                                        │ Embed(hecho del caso)    ─► SentenceEmbedding
                                        ▼
                     M15/M16: coseno(a, b) >= umbral (dato en Data/) ─► Handled / Outcome

`NpcAi.Core` solo define formas; el cableado es de M11 y de los cambios consumidores.

## File Inventory

| Archivo | Acción | Rol |
|---|---|---|
| `Runtime/Core/SentenceEmbedding.cs` (+ `.meta`) | Crear | `readonly struct` inmutable (AD6/AD7) |
| `Runtime/Core/Ports.cs` | Modificar | `+ interface ISentenceEmbedder` tras `IIntentClassifier` |
| `Runtime/Core/Contract.cs` | Modificar | `Version` `3 → 4` |
| `Docs/CONTRACT-CHANGELOG.md` | Modificar | `+ ## v4` (borrador abajo) |
| `Tests/EditMode/Core/SentenceEmbedderContract.cs` (+ `.meta`) | Crear | Base abstracta + 2 stubs + subclase concreta |
| `Tests/EditMode/Core/ContractTypeTests.cs` | Modificar | `+6` casos; pin `Version_del_contrato_es_tres` → `..._es_cuatro` |

## Interfaces / Contracts

```csharp
// Runtime/Core/SentenceEmbedding.cs (esqueleto; XML doc en espanol, sin tildes)
public readonly struct SentenceEmbedding : IEquatable<SentenceEmbedding>
{
    private readonly float[] _values;                     // nunca se expone
    public SentenceEmbedding(float[] values)              // null o longitud 0 => Empty
    { _values = values == null || values.Length == 0 ? null : (float[])values.Clone(); }
    public static readonly SentenceEmbedding Empty = default;
    public int   Length  => _values?.Length ?? 0;
    public bool  IsEmpty => Length == 0;
    public float this[int index] => _values[index];       // fuera de rango lanza (semantica C#)
    public float[] ToArray() => _values == null ? Array.Empty<float>() : (float[])_values.Clone();
    // Equals: misma Length y SingleToInt32Bits igual en cada componente.
    // GetHashCode: combinacion de los bits de todos los componentes; Empty => 0. ==, !=.
}

// Runtime/Core/Ports.cs, seccion "Comprension", tras IIntentClassifier
/// <summary>M2 — de texto libre a un vector de embedding de oracion.</summary>
public interface ISentenceEmbedder
{
    bool IsReady { get; }                 // leer NO DEBE lanzar
    SentenceEmbedding Embed(string text); // invariantes: ver changelog v4
}
```

## Borrador de `## v4` para `Docs/CONTRACT-CHANGELOG.md`

Se inserta encima de `## v3`, sin tildes (convención del archivo):

```markdown
## v4 — 2026-09-25 — Puerto de embeddings semanticos de oraciones (M2)

Tercer cambio de contrato despues del congelamiento de v1. Extension **puramente aditiva**:
ningun enum, DTO ni puerto de v1/v2/v3 cambia (nombre, valor, orden, cardinalidad, firma).
Agrega la superficie para que M2 exponga un vector de embedding por oracion y que modulos
consumidores (M15, M16, M9) comparen texto por similitud en vez de por palabras.

### Tipos nuevos

- **DTO inmutables:** `SentenceEmbedding` (los DTO pasan de 10 a 11).
- **Puertos:** `ISentenceEmbedder` (los puertos pasan de 9 a 10).

Ambos son C# puro (`float[]` privado, `System.BitConverter`): `NpcAi.Core` mantiene
`noEngineReferences: true` y cero referencias `NpcAi.*` ajenas (`CoreAssemblyPurityTests`).

### `SentenceEmbedding` — vector inmutable por construccion

`readonly struct` que copia el arreglo recibido y nunca lo expone: `Length`, indexador de solo
lectura, `IsEmpty`, `ToArray()` (copia defensiva). `null` o longitud `0` ⇒ `Empty`;
`Empty == default`, `Length == 0`, `GetHashCode() == 0`. Igualdad completa **bit a bit**
(`Equals`, `GetHashCode`, `operator ==`, `operator !=`): misma `Length` y mismos bits en cada
componente (`0f` y `-0f` son distintos). La dimension NO es parte del contrato: depende del
encoder de cada reentrenamiento de M2 (768 hoy, 384 con MiniLM), **nunca** un cambio de contrato.

### Invariantes de `ISentenceEmbedder`

Toda implementacion real y todo doble DEBE heredar `NpcAi.Core.Tests.SentenceEmbedderContract`
y pasar el 100% de sus `[Test]`, sin escena de Unity ni entorno de VR.

- **`IsReady`**: leer NO DEBE lanzar en ningun estado.
- **`Embed(string)`**: NO DEBE lanzar en ningun estado (`IsReady == false`, `null`, vacio, solo
  espacios, simbolos, numeros, cadenas de 5000 caracteres). Con `IsReady == false` o texto
  `null` / vacio / solo espacios DEBE devolver `SentenceEmbedding.Empty`. Con `IsReady == true`
  y texto con contenido DEBE devolver un vector no vacio de componentes finitos (sin `NaN` ni
  `±Inf`). Todo vector no vacio de una misma instancia DEBE tener la misma `Length`. La
  normalizacion L2 NO esta obligada.

### Asimetria de determinismo

`ISentenceEmbedder.Embed` **DEBE** ser determinista **bit a bit** para el mismo texto en la
misma instancia (mismo modelo cargado, mismo dispositivo); no se garantiza igualdad entre
dispositivos. A diferencia de `IntentResult.Confidence` / `LatencyMs` (metadatos de medicion,
exentos desde v1), el embedding es la senal primaria de emparejamiento de consumidores
deterministas como `IClinicalResponder.Respond`: sin esta clausula su determinismo seria
inalcanzable. `SentenceEmbedding` no lleva latencia por esa razon.

### Advertencia de calidad

El `.onnx` vigente usa `distilbert-base-multilingual-cased` (masked-LM generico, sin objetivo
de similitud). Este contrato no garantiza que sus embeddings sirvan para emparejar hechos: es
una pregunta empirica de los cambios consumidores.

### Decisiones y proceso (regla 10)

- **Enfoque**: puerto nuevo y separado; `IIntentClassifier` / `IntentResult` intactos.
- AD1 (dimension definida por la implementacion) y AD2 (determinismo bit a bit) resueltas en
  el diseno de `2026-09-25-m0-puerto-embeddings-semanticos`.
- **Co-revision de M0** (regla 2): PENDIENTE — <quien, cuando, donde>.
- **Ciclo SDD**: cambio `2026-09-25-m0-puerto-embeddings-semanticos`. Ejecucion Unity 6
  EditMode; ningun agente corre Unity: el verde es compuerta humana.
- **Diferido**: implementacion real en M2 y consumo en M15 / M16, un cambio SDD por modulo.

El pin `ContractTypeTests.Version_del_contrato_es_tres()` se renombra a
`Version_del_contrato_es_cuatro()` con valor `4` en el mismo commit (mismo criterio que v3).
```

## Testing Strategy

### `Tests/EditMode/Core/SentenceEmbedderContract.cs` (nuevo)

`public abstract class SentenceEmbedderContract`, `protected abstract ISentenceEmbedder
CreateSubject();`, `[Test]` no abstractos. Frases compartidas: `FraseDePrueba = "me duele el
pecho desde ayer"`, `FraseDeControl = "que clima hace hoy"`. Los caminos que exigen sujeto
listo usan `Assume.That(IsReady)`.

| # | `[Test]` | Qué fija |
|---|---|---|
| 1 | `Reporta_si_esta_listo_sin_lanzar` | Leer `IsReady` no lanza |
| 2 | `Embed_no_lanza_en_ningun_estado` | `null`, `""`, espacios, símbolos, números, 5000 caracteres; sujeto y stub no-listo |
| 3 | `Sin_estar_listo_Embed_devuelve_vector_vacio` | Stub no-listo, y sujeto si `!IsReady` ⇒ `Empty` |
| 4 | `Texto_nulo_vacio_o_solo_espacios_devuelve_vector_vacio` | Sin `Assume`: vale en cualquier estado |
| 5 | `Listo_y_con_texto_devuelve_vector_no_vacio` | `!IsEmpty` |
| 6 | `Todos_los_vectores_no_vacios_tienen_la_misma_longitud` | AD1: frase de prueba, de control y 5000 caracteres |
| 7 | `Los_componentes_son_finitos` | AD8: ningún `NaN`/`±Inf` |
| 8 | `Es_determinista_bit_a_bit_para_el_mismo_texto` | AD2: 3 llamadas ⇒ `==` y `SingleToInt32Bits` igual por componente |
| 9 | `Mutar_la_copia_devuelta_no_altera_llamadas_posteriores` | AD6: mutar `ToArray()` no cambia el siguiente `Embed` |
| 10 | `Textos_distintos_no_dan_el_mismo_vector` | No degenerado: prueba ≠ control |

Stubs `private sealed` en el mismo archivo:

- `EmbebedorDeOracionesNoListo` — `IsReady => false`, `Embed => SentenceEmbedding.Empty`.
- `EmbebedorDeOracionesDePrueba` — siempre listo; texto en blanco ⇒ `Empty`; si no, hashing de
  palabras en minúsculas a 8 cubetas con conteos enteros (bit-exacto trivial, finito, distinto
  para las dos frases). Andamio, no implementación de M2.

Más `public sealed class SentenceEmbedderContractStubTests : SentenceEmbedderContract`.

### `ContractTypeTests.cs` (aditivo, sección `// --- v4: embeddings de oraciones ---`)

`SentenceEmbedding_Empty_es_el_valor_por_defecto_y_hashea_a_cero`,
`_con_null_o_arreglo_vacio_es_Empty`, `_copia_el_arreglo_recibido`,
`_ToArray_devuelve_una_copia`, `_compara_bit_a_bit` (incluye `0f` vs `-0f`),
`_iguales_hashean_igual`; más el renombre del pin. `Solo_PersonalityId_implementa_IEquatable_en_la_v1`
**no se edita** (lista cerrada de v1, igual que en v3).

### Sin cambio

`ContractVersionChangelogTests` (ata `Version` al mayor `## v<N>`: pasa si bump y `## v4` van
juntos) y `CoreAssemblyPurityTests` (solo mira referencias de ensamblado; `System.*` no cuenta).

## Migration / Rollout — corte en unidades

| Unidad | Contenido | Estado esperado |
|---|---|---|
| **U1 (tipo)** | `SentenceEmbedding.cs` + `.meta`, `+6` casos en `ContractTypeTests` | Verde con `Version == 3` y sin `## v4`: el tipo no referencia el puerto |
| **U2 (puerto)** | `ISentenceEmbedder` en `Ports.cs`, `SentenceEmbedderContract.cs` + `.meta`, `Contract.cs` `3→4`, `## v4`, pin | Bump, changelog y pin en **un solo commit** |

Orden TDD en U2: base de contrato + stubs en rojo → puerto → bump/changelog/pin atómico. Con
presupuesto de 800 líneas, un PR único es plausible (precedente comparable ≈610 L con tres
PR); `sdd-tasks` pronostica y decide. Sin migración de datos; rollback = revertir.

## Threat Matrix

N/A — sin enrutado, shell, subprocesos, automatización de VCS/PR, clasificación de archivos
ejecutables ni integración de procesos: tipos C# puros en un ensamblado sin referencias externas.

## Open Questions / Riesgos del diseño

- **R1** — `BitConverter.SingleToInt32Bits` debe existir en el perfil de API de Unity 6
  (.NET Standard 2.1). Si no, salida sin `unsafe`: struct `[StructLayout(Explicit)]` con
  `float`/`int` superpuestos. Mismo contrato.
- **R2** — Determinismo por instancia, no entre dispositivos: un consumidor que precalcule
  embeddings de hechos en PC y compare en Quest queda fuera de la garantía.
- **R3** — Las frases de prueba acoplan suavemente la base con el doble de M2 (`Assume` omite,
  no falla).
- **R4** — Tamaño del artefacto: excede las 800 palabras del skill por el borrador del changelog
  pedido; sigue la escala del diseño precedente de v3.
