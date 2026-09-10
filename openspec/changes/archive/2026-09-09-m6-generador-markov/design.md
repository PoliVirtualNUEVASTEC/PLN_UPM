# Design: M6 — Generador de diálogo por cadenas de Markov

## Technical Approach

Un adaptador nuevo (`MarkovDialogueGenerator`) detrás del puerto `IDialogueGenerator` que ya
existe. Nada de entrenamiento ni redes neuronales: una cadena de Markov de palabras (bigramas)
construida a partir de un corpus semilla de frases de ejemplo por personalidad y por estado de
receptividad. El corpus semilla se carga una vez (al construir el generador) y se convierte en
tablas de transición; cada `Generate` hace un paseo aleatorio sobre la tabla que corresponde al
`(PersonalityId, Receptivity)` recibido.

### Unidad de módulo y grafo de referencias

    NpcAi.Dialogue -> [NpcAi.Core]   (sin cambio)

`Runtime/Dialogue` ya tiene `noEngineReferences: true` (no depende de `UnityEngine`, a
diferencia de `NpcAi.Nlu` con Sentis) — este cambio no lo modifica: una cadena de Markov de
palabras es lógica pura de C#, no necesita ningún paquete de Unity. Ningún otro ensamblado gana
ni pierde referencias.

`Data/Dialogue/` vive fuera de cualquier `.asmdef`: son archivos `.json` de datos, cargados en
runtime como `TextAsset` (mismo patrón que M3 usa para `Data/Corpus/*.json` y M5 para sus
`.asset`), no código compilado.

## Architecture Decisions

| # | Decisión | Elección | Alternativas rechazadas | Razón |
|---|---|---|---|---|
| AD1 | Técnica de generación | Cadena de Markov de palabras, orden 2 (bigramas) | Modelo de lenguaje neuronal entrenado; n-gramas de orden 3+ | Ya fijado en la propuesta de trabajo de grado; con un corpus semilla chico, orden 2 da variedad razonable sin necesitar volúmenes de texto que no existen todavía |
| AD2 | Indexación del corpus semilla | Por `(PersonalityId, Receptivity)` — 12 bloques (4×3) | Un único bloque global sin distinguir personalidad/receptividad | Misma dimensionalidad que ya prueba `DialogueGeneratorContract` (`Un_NPC_no_receptivo_no_responde_igual_que_uno_receptivo`, `Funciona_sin_personalidad_asignada`); menos bloques perdería la garantía de que `Receptivo` y `NoReceptivo` difieren de forma consistente por diseño, no por azar |
| AD3 | `IntentResult` en esta primera entrega | Se acepta en la firma, no condiciona el bloque semilla ni la generación | Indexar también por `Intent` (7 valores → 84 bloques) | Multiplicar la matriz de corpus semilla por 7 antes de validar que el enfoque de Markov funciona en absoluto no es prudente (ver `proposal.md` → Out of Scope); la firma ya está lista para una extensión futura sin romper el contrato |
| AD4 | Ubicación del corpus semilla | `Data/Dialogue/<personalidad>.json`, uno por personalidad, con las 3 listas de receptividad adentro | Un solo archivo con las 4 personalidades mezcladas; o carpetas por receptividad en vez de por personalidad | Espeja el patrón ya establecido en `Data/Personalities/<personalidad>.asset` (M5): un archivo por personalidad es la unidad natural de edición para quien ajuste el tono de un personaje |
| AD5 | Reintento y respaldo ante paseo degenerado | Reintentar un número acotado de veces; si sigue vacío, devolver una frase semilla verbatim al azar del mismo bloque | Lanzar excepción; devolver siempre un string fijo tipo `"..."` | El contrato exige `Nunca_devuelve_texto_vacio` sin excepción — un respaldo determinista a una frase semilla real (no un placeholder) mantiene la respuesta creíble incluso en el peor caso |
| AD6 | Metadatos (`EmotionTag`/`AnimationCue`) | Tabla fija por `Receptivity`, igual que el doble actual — no generados por la cadena de Markov | Generarlos también por Markov o derivarlos del texto generado | Son señales de control (qué animación reproducir), no texto: no tiene sentido generarlos estocásticamente, y mantenerlos fijos preserva el comportamiento ya validado en `ScriptedDialogueGenerator` |
| AD7 | Validación de aceptación | Verde EditMode humano (`DialogueGeneratorContract` heredado) + corrida manual de 1000 llamadas sin texto vacío | Compuerta de CI | Igual criterio que el resto del repo: no hay CI; la prueba de 1000 llamadas cubre lo que EditMode no cubre por sí solo (que el respaldo ante degeneración realmente dispara con corpus chico) |

## Data Flow

    Data/Dialogue/<personalidad>.json  (nuevo, este cambio)
              │
              ▼  MarkovDialogueGenerator (constructor)
    ┌─────────────────────────────────────────┐
    │ 1. Cargar los 4 archivos de personalidad │
    │ 2. Por cada (personalidad, receptividad) │
    │    construir tabla de transicion de      │
    │    bigramas (MarkovChainBuilder)          │
    └─────────────────────────────────────────┘
              │  (una vez, al construir el generador)
              ▼
    Generate(personalidad, receptividad, intent)
              │
              ▼
    ┌─────────────────────────────────────────┐
    │ 1. Elegir tabla por (personalidad,       │
    │    receptividad); None -> bloque neutro  │
    │ 2. Paseo aleatorio acotado sobre la tabla │
    │ 3. Si degenera: reintentar; si sigue      │
    │    vacio, frase semilla verbatim al azar  │
    │ 4. EmotionTag/AnimationCue: tabla fija    │
    │    por Receptivity                        │
    └─────────────────────────────────────────┘
              │
              ▼
    NpcReply  →  M8 (Presentador, sin cambios)

## File Inventory

| Archivo | Rol |
|---|---|
| `Data/Dialogue/README.md` | Explica el propósito del corpus semilla y en qué se diferencia de `Data/Corpus/` (M3) |
| `Data/Dialogue/grosero.json` | Frases semilla del personaje `grosero`, por receptividad |
| `Data/Dialogue/histerico.json` | Frases semilla del personaje `histerico`, por receptividad |
| `Data/Dialogue/introvertido.json` | Frases semilla del personaje `introvertido`, por receptividad |
| `Data/Dialogue/empatico.json` | Frases semilla del personaje `empatico`, por receptividad |
| `Runtime/Dialogue/MarkovChainBuilder.cs` | Construye la tabla de transiciones de bigramas a partir de una lista de frases |
| `Runtime/Dialogue/MarkovDialogueGenerator.cs` | Implementación real de `IDialogueGenerator` |
| `Tests/EditMode/Dialogue/MarkovDialogueGeneratorTests.cs` | Hereda `DialogueGeneratorContract` |
| `openspec/specs/generador-dialogo-m6/spec.md` | Primera spec formal de M6 |

## Interfaces / Contracts

Sin cambios en `NpcAi.Core`. Superficie nueva, interna a `NpcAi.Dialogue`:

```csharp
// Runtime/Dialogue/MarkovChainBuilder.cs
public sealed class MarkovChainBuilder
{
    public MarkovChainBuilder(IReadOnlyList<string> frasesSemilla); // corpus de un bloque
    public string Walk(System.Random rng, int largoMaximoPalabras); // "" si degenera
}

// Runtime/Dialogue/MarkovDialogueGenerator.cs
public sealed class MarkovDialogueGenerator : IDialogueGenerator
{
    public MarkovDialogueGenerator(IReadOnlyDictionary<string, TextAsset> corpusPorPersonalidad);
    public NpcReply Generate(PersonalityId personality, Core.Receptivity receptivity, IntentResult intent);
}
```

## Testing Strategy

| Archivo | Qué fija |
|---|---|
| `DialogueGeneratorContract` (sin cambios) | Comportamiento observable del puerto: nunca vacío, tags nunca nulos, funciona sin personalidad, `Receptivo` ≠ `NoReceptivo`, determinismo NO exigido |
| `MarkovDialogueGeneratorTests` (nuevo) | Hereda `DialogueGeneratorContract` sin modificarla; `CreateSubject()` carga el corpus semilla real de `Data/Dialogue/` |
| `MarkovChainBuilderTests` (nuevo, unidad) | `Walk` nunca lanza con una lista de una sola frase o con frases de una sola palabra; con una lista vacía, `Walk` devuelve `""` (caso que `MarkovDialogueGenerator` debe cubrir con el respaldo de AD5) |
| Prueba manual de 1000 llamadas | Para las 4 personalidades × 3 estados, ninguna de 1000 llamadas a `Generate` devuelve texto vacío — cubre el caso de corpus semilla chico que EditMode no ejercita por volumen |

Ejecución EditMode: Test Runner de Unity 6, igual que el resto del repo.

## Migration / Rollout

Sin migración de datos ni de contrato. `MarkovDialogueGenerator` es una implementación nueva del
mismo puerto: se integra reemplazando el punto donde hoy se instancia
`ScriptedDialogueGenerator` en cualquier composición real (M11 Harness cuando exista su escena),
sin tocar la interfaz.

Rollback: revertir los commits de este cambio deja `ScriptedDialogueGenerator` (plantillas
fijas) como implementación real, igual que hoy.

## Open Questions

- [ ] ¿Cuántas frases semilla por bloque `(personalidad, receptividad)` son suficientes para que
      el texto generado deje de sentirse repetitivo? No hay una cifra objetivo todavía — se
      decide con el equipo mirando la salida real, no a priori en este documento.
- [ ] ¿El corpus semilla lo escribe el mismo equipo que etiqueta `Data/Corpus/` (M3), o es
      trabajo separado? Afecta el cronograma de la actividad 3.3 pero no el diseño técnico de
      este cambio.
