# Design: M9 — Escenario Sala de Emergencia (implementación real de `IScenarioObjective`)

## Technical Approach

Una implementación nueva detrás del puerto congelado `IScenarioObjective`, más un lector propio del
bloque `clave` de `Data/Cases/*.json`. Tres capas, espejo exacto de M15
(`ClinicalResponder` / `ClinicalCaseLoader` / `ClinicalCase`):

| Capa | Tipo | `UnityEngine` |
|---|---|---|
| Dato cargado | `TriageKey` (POCO, propiedades en español) | No |
| Lector | `TriageKeyLoader` (`TryParse` + `Raw*` privados anidados) | Sí (`JsonUtility`) |
| Puntaje | `TriageScenarioObjective : IScenarioObjective` + superficie aditiva | No |
| Pesos | `TriageObjectiveSettings` (POCO inyectable) | No |

El objetivo no lee disco: recibe un `Func<ClinicalCaseId,string> cargarJson` inyectado, copia
literal de `ClinicalResponder` (línea 25 y su comentario AD3). `Progress01` se deriva en lectura a
partir de cuatro piezas de estado (racha receptiva, clave cargada, conjunto de índices
registrados, triaje declarado); no hay valor cacheado que invalidar.

### Unidad de módulo y grafo de referencias

    NpcAi.Scenarios.Emergency -> [NpcAi.Core, NpcAi.Core.Channels]   (sin cambio)

`Runtime/Scenarios/Emergency/NpcAi.Scenarios.Emergency.asmdef` **no se modifica**: ya referencia
exactamente esos dos ensamblados con `noEngineReferences: false` (lo necesita `JsonUtility`).
Los tipos nuevos usan solo `NpcAi.Core` (`ClinicalCaseId`, `ReceptivityChange`,
`IScenarioObjective`); `NpcAi.Core.Channels` queda referenciado para el cableado futuro de M11 y
ningún tipo nuevo lo toca.

`Tests/EditMode/Scenarios/Emergency/NpcAi.Scenarios.Emergency.Tests.asmdef` **tampoco se
modifica**: ya referencia `NpcAi.Core`, `NpcAi.Core.Tests` y `NpcAi.Scenarios.Emergency`. A
diferencia del defecto de M7 PR3 (`NpcAi.VrInput.Tests.asmdef` sin `NpcAi.Core.Channels`), las
pruebas de M9 no tocan ningún canal, así que no falta ninguna referencia.

**Toda la superficie nueva es `public`**: no hace falta
`Runtime/Scenarios/Emergency/Properties/AssemblyInfo.cs` ni `InternalsVisibleTo` (ver AD2).

## Architecture Decisions

| # | Decisión | Elección | Alternativas rechazadas | Razón |
|---|---|---|---|---|
| AD1 | Lector propio de `clave` | `TriageKeyLoader` con `Raw*` privados anidados que declaran **solo** `clave` | Referenciar `NpcAi.ClinicalResponse`; extender `ClinicalCaseLoader` | Regla dura 3: no hay referencia entre módulos, y los `RawCase`/`RawHecho` de M15 son `private sealed`, inaccesibles incluso si la hubiera. Declarar solo `clave` hace que `JsonUtility` ignore `paciente`/`hechos`, igual que `ClinicalCaseLoader` ignora `clave` |
| AD2 | Visibilidad de la superficie aditiva | Constructor rico y los cuatro mutadores `public` | `internal` + `InternalsVisibleTo` (patrón de M1/M7/M8) | El precedente `internal` de M7 existe porque `SpatialPhysicalActionSource` expone `ISpatialSampler`, un tipo interno del módulo. Aquí la firma solo usa `Func<ClinicalCaseId,string>` (M0) y `TriageObjectiveSettings` (POCO público), y el llamador real es **M11, otro ensamblado**: `internal` bloquearía al consumidor previsto. Espejo exacto de `ClinicalResponder`, que por eso no tiene `AssemblyInfo.cs` |
| AD3 | Validación del lector independiente de M15 | `TryParse` exige `clave` bien formada y **no** exige `id`, `paciente` ni el mínimo de 8 `hechos` | Reusar el criterio completo de `ClinicalCaseLoader` | Un caso con tabla de hechos incompleta sigue siendo calificable por M9. Que M15 rechace un caso y M9 lo acepte es correcto: validan contratos distintos sobre el mismo archivo |
| AD4 | Forma de los pesos | **Dos fracciones complementarias** (`PesoDeReceptividad`, `PesoDeBanderasRojas`) y no cuatro pesos sueltos | Cuatro `float` con renormalización por suma | Con complementos, `clínico = 1f - receptividad` y `triaje = 1f - banderas` son exactos en `float`, así que el máximo de la mezcla es **exactamente 1.0f** y no existe el caso patológico "suma de pesos 0" ni `IsComplete` inalcanzable por configuración (ver "Aritmética") |
| AD5 | `Progress01` derivado en lectura | Función pura del estado, recalculada en cada `get` | Recalcular y guardar en cada mutador | Cuatro mutadores (`Notify`/`AssignCase`/`RegisterRedFlag`/`DeclareTriage`) más `Reset` son cinco puntos de invalidación; derivar elimina el estado rancio. El costo son ~6 operaciones de punto flotante por lectura |
| AD6 | Receptividad **sostenida** por racha saturada | `min(racha / RachaParaReceptividadPlena, 1)`, con `racha` incrementada de forma saturada | Paso fijo ±1 (el doble actual); media móvil de los últimos N cambios | La regla del escenario es propia: `Worsened` reinicia la racha a 0 (perder la cooperación del paciente obliga a reconstruir la relación). Saturar el incremento evita desbordar en sesiones largas y hace que `racha == N` dé exactamente `1.0f` |
| AD7 | `RachaParaReceptividadPlena = 5` | 5 mejoras consecutivas dan crédito receptivo pleno | 3 o 4 (el doble completa en 4); 8-10 | 5 es del orden del doble actual y cabe con holgura en el presupuesto de 50 iteraciones de las pruebas contractuales. El motivo decisivo es 5 > 3: `Notify_con_el_valor_por_defecto_es_no_op` notifica 3 mejoras y compara antes/después, así que con N ≤ 3 el progreso ya estaría saturado en 1 y un defecto que sumara racha en `default` quedaría invisible tras el `min`. Con N = 5 la comparación ocurre en 0.6 y el defecto se ve. **Valor razonado, no validado** (ver Open Questions) |
| AD8 | Semántica de `Reset()` | Vuelve al estado **recién construido**: racha 0, clave descartada (`HasKey == false`), banderas y triaje limpios. Idempotente | Limpiar solo el estado clínico y conservar la clave; limpiar clínico y clave pero conservar la racha | Confirma el supuesto del agente de spec. `Reset()` es operación de **sesión**, igual que `IReceptivityEngine.Reset` y `IClinicalResponder.AssignCase`: una sesión nueva no hereda rapport. Decisivo: `Fakes/ScriptedScenarioObjective.Reset()` ya pone a cero el progreso derivado de receptividad — dos clases en la misma carpeta con un `Reset()` de significado distinto sería una trampa. La frase de la spec "la racha no está atada al caso asignado" sigue siendo verdadera donde importa: **`AssignCase` no toca la racha** |
| AD9 | `RegisterRedFlag` por índice, en un `HashSet<int>` | Índice validado contra `BanderasRojas.Count` | Texto libre con similitud; `bool[]` del tamaño de la clave | El conjunto da idempotencia gratis y sobrevive a la reasignación de caso sin redimensionar. El texto libre de las banderas rojas no comparte llave con `hechos[]` y exigiría similitud semántica (fuera de alcance) |
| AD10 | Normalización del triaje | `Trim()` + `ToUpperInvariant()`, comparación `Ordinal`, en ambos lados | Comparar crudo; aceptar equivalentes numéricos ("2" → "II") | `"ii"` y `" II "` son la misma declaración clínica; `"2"` es otra notación y aceptarla sería un mapeo inventado que ningún documento de M14 respalda |
| AD11 | `banderasRojas` vacío | Ese componente cuenta como satisfecho (fracción = 1) | Contarlo como 0 | Un caso sin banderas rojas debe seguir siendo completable; con 0 el techo quedaría en 0.79 y `IsComplete` sería inalcanzable para ese caso |

## Interfaces / Contracts

Sin cambios en `NpcAi.Core`. Superficie nueva, toda en `namespace NpcAi.Scenarios.Emergency`
(el `Config/` es carpeta, no sub-namespace — patrón de `Runtime/VrInput/Config/`).

```csharp
// Runtime/Scenarios/Emergency/TriageKey.cs
// Espejo del bloque "clave" de Data/Cases/*.json (verificado contra caso-01.json).
public sealed class TriageKey
{
    public string TriajeEsperado { get; }              // "I".."V", ya normalizado
    public string TiempoAtencion { get; }              // texto libre: "< 30 min"
    public IReadOnlyList<string> BanderasRojas { get; } // lista ORDENADA y estable
    public string CierreEsperado { get; }              // párrafo libre, no se califica

    public TriageKey(string triajeEsperado, string tiempoAtencion,
                     IReadOnlyList<string> banderasRojas, string cierreEsperado);
    // nulos -> string.Empty / Array.Empty<string>(), igual que ClinicalCase/Hecho
}

// Runtime/Scenarios/Emergency/TriageKeyLoader.cs
public static class TriageKeyLoader
{
    /// <summary>
    /// true solo si hay bloque "clave" bien formado con triajeEsperado en {"I".."V"}.
    /// Nunca lanza: JSON nulo/vacio/malformado o "clave" ausente devuelven false.
    /// </summary>
    public static bool TryParse(string json, out TriageKey clave);

    private static readonly string[] TriajesValidos = { "I", "II", "III", "IV", "V" };

    [Serializable] private sealed class RawCase { public RawClave clave; }
    [Serializable] private sealed class RawClave
    {
        public string triajeEsperado;
        public string tiempoAtencion;
        public string[] banderasRojas;
        public string cierreEsperado;
    }
}

// Runtime/Scenarios/Emergency/Config/TriageObjectiveSettings.cs
public sealed class TriageObjectiveSettings
{
    public const float PesoDeReceptividadPorDefecto   = 0.3f;  // clinico = 1 - este = 0.7f
    public const float PesoDeBanderasRojasPorDefecto  = 0.7f;  // triaje  = 1 - este = 0.3f
    public const int   RachaParaReceptividadPlenaPorDefecto = 5;

    public float PesoDeReceptividad { get; }          // [0,1]
    public float PesoDeBanderasRojas { get; }         // [0,1] dentro de la mitad clinica
    public int   RachaParaReceptividadPlena { get; }  // >= 1

    public float PesoClinico  => 1f - PesoDeReceptividad;   // AD4: complemento exacto
    public float PesoDeTriaje => 1f - PesoDeBanderasRojas;  // AD4: complemento exacto

    public TriageObjectiveSettings(
        float pesoDeReceptividad  = PesoDeReceptividadPorDefecto,
        float pesoDeBanderasRojas = PesoDeBanderasRojasPorDefecto,
        int   rachaParaReceptividadPlena = RachaParaReceptividadPlenaPorDefecto);
    // Clamp01 en los dos pesos; Math.Max(1, racha) elimina la division por cero
}

// Runtime/Scenarios/Emergency/TriageScenarioObjective.cs
public sealed class TriageScenarioObjective : IScenarioObjective
{
    /// <summary>Sujeto sin caso: solo la via receptiva. Lo usa la gemela de contrato.</summary>
    public TriageScenarioObjective();                  // = new(new TriageObjectiveSettings(), null)

    /// <summary>AD2 public: M11 vive en otro ensamblado. cargarJson PUEDE ser null.</summary>
    public TriageScenarioObjective(TriageObjectiveSettings pesos,
                                   Func<ClinicalCaseId, string> cargarJson);

    // --- IScenarioObjective (puerto congelado) ---
    public float Progress01 { get; }
    public bool  IsComplete { get; }
    public void  Notify(ReceptivityChange change);

    // --- Superficie aditiva de M9 (decision 1 del usuario) ---
    public void AssignCase(ClinicalCaseId caseId);
    public void RegisterRedFlag(int index);
    public void DeclareTriage(string category);
    public void Reset();                               // AD8

    public bool   HasKey { get; }                      // _clave != null
    public int    RedFlagsFound { get; }               // indices distintos registrados
    public int    RedFlagCount { get; }                // total de la clave; 0 sin clave
    public string ExpectedClosure { get; }             // clave.cierreEsperado; "" sin clave
}
```

`RedFlagCount` no aparece en la spec y es un añadido de este diseño: sin él, ni M11 ni una prueba
pueden conocer el rango válido de índices que exige `RegisterRedFlag` (ver Open Questions).

### Estado y mutadores (pseudocódigo)

```
_pesos   : TriageObjectiveSettings   // nunca null
_cargar  : Func<ClinicalCaseId,string>  // PUEDE ser null
_clave   : TriageKey = null
_racha   : int = 0
_banderas: HashSet<int> = new()
_triaje  : string = null             // null = nada declarado; "" = declaracion vacia

Notify(change):
    if (!change.Changed) return                       // cubre default(ReceptivityChange)
    if (change.Improved && _racha < _pesos.RachaParaReceptividadPlena) _racha++   // AD6 saturado
    else if (change.Worsened) _racha = 0              // reinicio total, no -1

AssignCase(caseId):                                   // espejo de ClinicalResponder.AssignCase
    _clave = null; _banderas.Clear(); _triaje = null  // limpia SIEMPRE, antes de intentar cargar
    if (caseId.IsNone || _cargar == null) return
    try { json = _cargar(caseId) } catch (Exception) { return }
    if (json != null && TriageKeyLoader.TryParse(json, out var clave)) _clave = clave
    // la racha NO se toca: no esta atada al caso (AD8)

RegisterRedFlag(index):
    if (_clave == null) return                        // sin efecto antes de AssignCase
    if (index < 0 || index >= _clave.BanderasRojas.Count) return   // fuera de rango, sin lanzar
    _banderas.Add(index)                              // idempotente por construccion

DeclareTriage(category):
    if (_clave == null) return
    _triaje = Normalizar(category)                    // AD10; ultima llamada gana
    // null/vacio -> "" : declaracion que no coincide, no no-op

Reset():                                              // AD8
    _clave = null; _banderas.Clear(); _triaje = null; _racha = 0

Normalizar(s) => s == null ? "" : s.Trim().ToUpperInvariant()
```

### `Progress01` e `IsComplete` (pseudocódigo exacto)

```
Progress01:
    r = _pesos.RachaParaReceptividadPlena <= 0
        ? 1.0
        : Math.Min(1.0, (double)_racha / _pesos.RachaParaReceptividadPlena)     // AD6

    if (_clave == null)                               // renormalizacion: receptividad = 100%
        return Clamp01((float)r)

    f = _clave.BanderasRojas.Count == 0
        ? 1.0                                                                   // AD11
        : (double)_banderas.Count / _clave.BanderasRojas.Count

    t = (_triaje != null && _triaje == _clave.TriajeEsperado) ? 1.0 : 0.0

    clinico = _pesos.PesoDeBanderasRojas * f + _pesos.PesoDeTriaje * t
    total   = _pesos.PesoDeReceptividad  * r + _pesos.PesoClinico  * clinico
    return Clamp01((float)total)

IsComplete => Math.Abs(Progress01 - 1f) <= 1e-4f      // misma tolerancia que el contrato

Clamp01(x) => x < 0f ? 0f : (x > 1f ? 1f : x)         // System.Math, nunca Mathf
```

Con los valores por defecto, para `HasKey == true`:

    Progress01 = 0.3·r + 0.7·(0.7·f + 0.3·t)
               = 0.3·r + 0.49·f + 0.21·t        (peso sobre el total, informativo)

### Aritmética: ¿`Progress01` llega exactamente a 1?

Sí, y sin depender de la tolerancia. `0.3f` y `0.7f` son valores `float` concretos cuya suma es
exactamente `1.0`: la forma complementaria de AD4 (`1f - 0.3f == 0.7f` y `1f - 0.7f == 0.3f` son
exactos en IEEE-754 binario32) garantiza que cada par sume exactamente 1. Con `r = f = t = 1`:

    clinico = 0.7 · 1 + 0.3 · 1 = 1.0   (exacto)
    total   = 0.3 · 1 + 0.7 · 1 = 1.0   (exacto)

La acumulación es en `double` y solo el resultado final se convierte a `float`, así que tampoco hay
error intermedio. La tolerancia `1e-4` queda como holgura para fracciones intermedias (p. ej.
`3/7`), no para el máximo. Techos verificables, todos exactos: sin clave y sin racha, `0f` (lo
exige `Assert.AreEqual(0f, ...)` sin tolerancia en dos pruebas contractuales); con caso asignado y
solo receptividad plena, **0.3**; con caso asignado y solo corrección clínica plena, **0.7**.

## Data Flow

    Data/Cases/*.json (M14, solo lectura)
        │  bloque "clave"
        ▼
    Func<ClinicalCaseId,string> cargarJson        ← lo inyecta el compositor (M11) o la prueba
        │  string
        ▼
    TriageKeyLoader.TryParse  ──false──►  HasKey = false  (sin excepcion)
        │ true
        ▼
    TriageKey { TriajeEsperado, TiempoAtencion, BanderasRojas[], CierreEsperado }
        │
        ▼
    ┌─────────────────── TriageScenarioObjective ───────────────────┐
    │  _clave   (TriageKey)      _banderas (HashSet<int>)           │
    │  _racha   (int saturado)   _triaje   (string normalizado)     │
    └───────────────────────────────────────────────────────────────┘
        │ puerto                                    │ lecturas aditivas
        ▼                                           ▼
    Progress01 / IsComplete              HasKey / RedFlagsFound /
                                         RedFlagCount / ExpectedClosure
                                              │
                                              ▼
                                    M11 o M13 lo muestran a quien evalua
                                    (cierreEsperado NO mueve Progress01)

### Flujo multi-módulo (quién llama a quién)

    M4 Receptivity            M0 Channels                M11 (futuro)         M9
    ──────────────            ───────────                ────────────         ──
    Evaluate(...)
      └─► ReceptivityChange ─► ReceptivityChangeChannel ─► Subscribe ─────► Notify(change)
                                  (cableado por Inspector)

    M11 (futuro)                                                             M9
    ────────────                                                             ──
    inicio de sesion ──────────────────────────────────────────────────► AssignCase(caseId)
    "esta bandera roja se superficio" ─────────────────────────────────► RegisterRedFlag(i)
    "la enfermera declaro triaje X" ───────────────────────────────────► DeclareTriage("X")

M9 nunca llama a M4 ni a M15: la mitad receptiva entra por el puerto (canal mediante, regla dura 3)
y la mitad clínica por llamada directa a la clase concreta. Cómo se entera el compositor de que una
bandera roja se superficia es diseño de M11, no de esta entrega.

## File Inventory

| Archivo | Acción | Rol |
|---|---|---|
| `Runtime/Scenarios/Emergency/TriageKey.cs` | Crear | POCO del bloque `clave`, nulos → vacío |
| `Runtime/Scenarios/Emergency/TriageKeyLoader.cs` | Crear | `TryParse` + `Raw*` privados; único punto con `JsonUtility` |
| `Runtime/Scenarios/Emergency/Config/TriageObjectiveSettings.cs` | Crear | Dos fracciones complementarias + longitud de racha (AD4) |
| `Runtime/Scenarios/Emergency/TriageScenarioObjective.cs` | Crear | Puerto + superficie aditiva + puntaje |
| `Tests/EditMode/Scenarios/Emergency/TriageScenarioObjectiveTests.cs` | Crear | Gemela de `ScenarioObjectiveContract` |
| `Tests/EditMode/Scenarios/Emergency/TriageKeyLoaderTests.cs` | Crear | Casos borde del lector con JSON en memoria |
| `Tests/EditMode/Scenarios/Emergency/TriageProgresoTests.cs` | Crear | Mezcla, racha, reversibilidad por ambas vías |
| `Tests/EditMode/Scenarios/Emergency/TriageSuperficieAditivaTests.cs` | Crear | `AssignCase`/`RegisterRedFlag`/`DeclareTriage`/`Reset` |
| `openspec/specs/escenario-emergencia-m9/spec.md` | Crear | Primera spec formal de M9 (se promueve al archivar) |
| `Docs/MODULES.md` (sección M9) | Modificar | Cerrar "solo doble"; nombrar lo diferido |
| `Runtime/Scenarios/Emergency/NpcAi.Scenarios.Emergency.asmdef` | Sin cambio | Referencias ya correctas |
| `Tests/.../NpcAi.Scenarios.Emergency.Tests.asmdef` | Sin cambio | Ya tiene `Core`, `Core.Tests`, `Emergency` |
| `Runtime/Scenarios/Emergency/Fakes/ScriptedScenarioObjective.cs` | Sin cambio | Sigue siendo el doble del puerto |
| `Tests/EditMode/Core/ScenarioObjectiveContract.cs` | Sin cambio | Clase base intacta |
| `Runtime/Core/`, `Runtime/CoreChannels/`, `Runtime/ClinicalResponse/`, `Runtime/Scenarios/Boardroom/`, `Data/` | Sin cambio | Contrato congelado y otros módulos (reglas duras 1 y 2) |

Cada archivo nuevo necesita su `.meta` en el mismo commit: es el defecto que M7 tuvo que corregir
en `chore(m7): agregar .meta faltantes de los archivos de PR2`.

## Testing Strategy

Todo EditMode, sin escena, sin VR, sin audio, sin `MonoBehaviour`. El JSON va **embebido como
constante `string`** en cada clase de prueba y entra por el `Func<ClinicalCaseId,string>`
inyectado; se copia el patrón de `ClinicalResponderTests` (líneas 17-50: `JsonDeCasoUno` +
`CargarJsonDePrueba`, donde solo `"caso-01"` resuelve y cualquier otro id devuelve `null`).
Ninguna prueba toca `Data/Cases/` ni `File.ReadAllText`.

| Archivo | Qué fija | Cómo |
|---|---|---|
| `TriageScenarioObjectiveTests : ScenarioObjectiveContract` | Las 7 pruebas del puerto, ninguna inconclusa | `CreateSubject() => new TriageScenarioObjective()` (constructor sin parámetros). `HasKey` es siempre falso ahí, así que la renormalización hace que 5 mejoras completen: `Completar_implica_progreso_total` y `La_completitud_es_reversible` pasan el `Assume.That` de la línea 87 en lugar de quedar vacías. **La clase base no se toca** |
| `TriageKeyLoaderTests` | Caso feliz; `clave` ausente; JSON malformado; `triajeEsperado` `"VI"`/vacío; `banderasRojas` ausente y `[]`; JSON con `hechos` insuficientes (AD3: igual carga) | `TriageKeyLoader.TryParse` directo, sin construir el objetivo |
| `TriageProgresoTests` | Receptividad sola con caso = **0.3**; clínica sola = **0.7**; ambas = **1.0** e `IsComplete`; crédito proporcional (2 de 4 → `0.7·0.7·0.5 = 0.245`); racha crece monótona y satura; `Worsened` la reinicia a 0; `Notify(default)` y `From == To` no mueven nada; reversión por `Worsened` y por triaje incorrecto tras acertar | Constructor rico con `cargarJson` en memoria; `Assert.AreEqual(esperado, o.Progress01, 1e-4f)` |
| `TriageSuperficieAditivaTests` | `RegisterRedFlag` idempotente por índice; índice 99 y negativo no lanzan; sin efecto antes de `AssignCase`; `DeclareTriage` sobrescribible y normalizada (`"ii"`, `" II "`); `cargarJson` nulo / que lanza / caso sin `clave` → `HasKey` falso sin excepción; `AssignCase(ClinicalCaseId.None)`; reasignar caso descarta banderas y triaje; `Reset()` deja `Progress01 == 0f`, `HasKey` falso, `RedFlagsFound == 0` y es idempotente (AD8) | Mismo constructor rico; `Assert.DoesNotThrow` para los bordes |

**Orden TDD** (`strict_tdd: true`): el lector primero (es la dependencia de todo lo demás), luego
el puntaje sin clave (que es lo que hace pasar el contrato heredado), luego la mezcla con clave.

**Compuerta humana**: Unity Test Runner > EditMode > Run All en verde, con el total registrado por
el usuario en `apply-progress.md`. **No aplica compuerta física**: M9 no tiene dependencia de VR,
XR, audio ni escena.

## Threat Matrix

N/A — no hay enrutamiento, shell, subproceso, automatización de VCS/PR, clasificación de archivos
ejecutables ni integración de procesos. El cambio es aritmética en proceso más un `JsonUtility`
sobre una cadena que el llamador provee.

## Migration / Rollout

Sin migración. Todo es aditivo sobre un puerto existente; el doble no cambia y sigue disponible
para M11. Sin `.asset` nuevo, sin carpeta nueva en `Data/`, sin dependencia nueva en
`package.json`, sin cambio de `Contract.Version`, sin cambio de `.asmdef`. Revertir los commits
deja M9 en el estado "solo doble" que ya está en `main`: ningún módulo depende de
`TriageScenarioObjective` por nombre, solo de `IScenarioObjective`.

## Open Questions

- [ ] **Discrepancia en la propuesta que el orquestador debe verificar.** La tabla de la sección
      Approach (líneas 149-153) ya coincide con la decisión 3 (`0.3` / `0.7 × 0.7` / `0.7 × 0.3`),
      pero la prosa de las líneas 166 y 206 sigue diciendo "techo en 0.4" / "techo 0.4", que es un
      residuo del borrador `0.4/0.6`. Con los pesos finales, el techo sin caso asignado bajo pesos
      fijos sería **0.3**, no 0.4. Este diseño usa 0.3/0.7 y 0.7/0.3 en todas partes (decisión 3
      manda); corregir esas dos frases de `proposal.md` es una edición de una línea cada una que
      **no** cambia ninguna conclusión del argumento de renormalización.
- [ ] `RachaParaReceptividadPlena = 5` (AD7) es un valor razonado, no validado con el asesor —
      mismo estatus que los umbrales de grados y metros de M7. Vive en
      `TriageObjectiveSettings`, inyectable, y cambiarlo no toca una línea de puntaje. **No bloquea
      el diseño ni las pruebas** (cada prueba construye su propia configuración).
- [ ] Igual que arriba para `0.3/0.7` y `0.7/0.3`: juicio pedagógico acordado con el usuario el
      2026-09-17, sin validar todavía con el asesor.
- [ ] `RedFlagCount` es una lectura que este diseño agrega y que la spec no nombra. Se justifica
      porque sin ella el rango válido de `RegisterRedFlag` es inobservable desde otro ensamblado.
      Decidir si se añade una línea a la spec o se acepta como detalle de implementación.
- [ ] Exponer los **textos** de `BanderasRojas` (hoy solo se expone el conteo). La propuesta dice
      que M13 registra el texto, no el índice, así que alguien tendrá que leerlo de M9; pero la
      integración con M13 está fuera de alcance y exponerlo después es aditivo y trivial.
- [ ] Tensión con `rules.design` de `openspec/config.yaml` ("thresholds are data under `Data/`,
      never new classes"): la propuesta decidió explícitamente dejar los pesos como valores por
      defecto de un POCO inyectable en vez de crear `Data/Scenarios/`, porque son tres números
      estables y no umbrales calibrables por hardware. Se respeta el espíritu de la regla (ningún
      número aparece en la lógica de puntaje, todo entra por `_pesos`), y promoverlos a
      `ScriptableObject` queda como cambio posterior trivial. Anotado para que nadie lo lea como
      un descuido.
