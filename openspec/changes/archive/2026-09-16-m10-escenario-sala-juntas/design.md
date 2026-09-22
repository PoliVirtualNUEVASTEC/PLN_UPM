# Design: M10 — Escenario Sala de Juntas (`Runtime/Scenarios/Boardroom/`)

## Technical Approach

Adaptador nuevo detrás de un puerto **ya mergeado**. `IScenarioObjective` y
`ScenarioObjectiveContract` no cambian un byte: M10 es la **segunda** implementación real del
puerto, hermana de `TriageScenarioObjective` (M9).

Forma general = **espejo estructural de M9** (`Runtime/Scenarios/Emergency/`): POCO de proyección
del caso (`RequirementChecklist` ↔ `TriageKey`), cargador `string → POCO`
(`RequirementChecklistLoader` ↔ `TriageKeyLoader`), POCO de pesos en `Config/`
(`BoardroomObjectiveSettings` ↔ `TriageObjectiveSettings`), objetivo real con **superficie
aditiva sobre la clase concreta**, y doble determinista en `Fakes/`.

Dos piezas que M9 **no** tiene, y que son la razón de ser de M10:

1. **Tres vías de progreso en vez de dos** — cobertura del catálogo, cierre explícito y fiel, y
   trato sostenido. La aritmética completa está abajo en *Aritmética del progreso*.
2. **`Unity/BoardroomObjectiveSettingsAsset.cs`** — los pesos son dato editable en el Inspector
   (regla 7), no constantes C#. M9 dejó sus pesos solo como constantes; M10 cierra ese hueco
   sin tocar a M9.

### Unidad de módulo y grafo de referencias (regla `rules.design`)

    NpcAi.Scenarios.Boardroom -> [NpcAi.Core, NpcAi.Core.Channels]

Un solo ensamblado de runtime, un solo módulo (regla 1). El `asmdef` **ya existe y no se
modifica**: sigue referenciando solo `NpcAi.Core` + `NpcAi.Core.Channels`, y en particular **no**
`NpcAi.RequirementResponse` ni `NpcAi.Scenarios.Emergency` (regla 3). `noEngineReferences: false`
porque `RequirementChecklistLoader` usa `UnityEngine.JsonUtility` y el asset es un
`ScriptableObject`. El objetivo, el checklist y los settings son C# puro.

M10 lee el catálogo de M16 **por archivo**, nunca por ensamblado: precedente literal M9/M15 con
`Data/Cases/*.json`.

### Namespaces: `Config/` es carpeta, `Unity/` es sub-namespace

Inconsistencia heredada del repo, replicada a propósito para no inventar una tercera convención:

| Archivo | Namespace | Precedente |
|---|---|---|
| `Config/BoardroomObjectiveSettings.cs` | `NpcAi.Scenarios.Boardroom` | `Config/TriageObjectiveSettings.cs` (M9) — "carpeta, no sub-namespace" |
| `Unity/BoardroomObjectiveSettingsAsset.cs` | `NpcAi.Scenarios.Boardroom.Unity` | `Unity/ReceptivityProfileAsset.cs` (M4) |

## Aritmética del progreso

Tres vías, una sola fórmula, un solo lugar donde vive (AD7).

| Vía | Símbolo | Rango | Entra por | Estado que la sostiene |
|---|---|---|---|---|
| Cobertura | `c` | `[0,1]` fracción | `RegisterDisclosure` | `HashSet<RequirementId> _revelados` / `_checklist.Count` |
| Cierre | `k` | `{0,1}` | `PresentSummary` | `HashSet<RequirementId> _resumen` (o `null`) |
| Trato | `t` | `[0,1]` fracción | `Notify` (el puerto) | `int _trato ∈ [0, PasosDeTrato]` |

**Con caso asignado** (`HasCase == true`):

    Progress01 = clamp01( PesoDeTrato·t + PesoDeLevantamiento·(PesoDeCobertura·c + PesoDeCierre·k) )

**Sin caso asignado** (`HasCase == false`, que es exactamente lo que construye
`ScenarioObjectiveContract.CreateSubject()`):

    Progress01 = clamp01( t )        ← renormalización: el trato es el 100 %

Con los valores confirmados (`PesoDeTrato = 0.2`, `PesoDeCobertura = 0.75`, y los dos
complementos exactos `PesoDeLevantamiento = 0.8`, `PesoDeCierre = 0.25`) la fórmula se aplana a:

    Progress01 = 0.20·t + 0.60·c + 0.20·k

**Por qué ese reparto.** El peso del trato lo fijó Jefferson (decisión 3, ≈0.2) y este diseño lo
toma literal: 0.2. El 0.8 restante es el levantamiento en sí, y dentro de él la cobertura manda
3-a-1 sobre el cierre porque la habilidad evaluada es *saber qué preguntar*; el cierre es una
sola compuerta binaria y no debe valer tanto como cubrir un catálogo de 4 a 6 requerimientos.
0.60/0.20 deja el cierre pesando igual que el trato, que es el orden de importancia correcto:
cubrir ≫ cerrar ≈ tratar bien.

**Tabla de sanidad** (la fija `RequirementsProgresoTests`):

| Escenario | `t` | `c` | `k` | `Progress01` | `IsComplete` |
|---|---|---|---|---|---|
| Recién construido, sin caso | 0 | — | — | **0.00** | no |
| `AssignCase` y nada más | 1 | 0 | 0 | **0.20** | no |
| Sesión perfecta | 1 | 1 | 1 | **1.00** | **sí** |
| Cubre todo, no cierra | 1 | 1 | 0 | **0.80** | no |
| Cierre temprano honesto (mitad del catálogo) | 1 | 0.5 | 1 | **0.70** | no |
| Cubre y cierra, maltrató toda la sesión | 0 | 1 | 1 | **0.80** | no |
| Cubre, cierra, un solo `Worsened` sin recuperar | 0.8 | 1 | 1 | **0.96** | no |

La última fila es la penalización mínima observable: **0.04 por cada empeoramiento no
recuperado**. Eso es exactamente "la puerta de M16 pasa a ser penalización, no progresión"
(decisión 1) hecho número.

### El trato: un libro mayor simétrico, no una racha

`_trato` es un entero en `[0, PasosDeTrato]` con **una sola regla de movimiento**:

- Construcción y `Reset()`: `_trato = 0`.
- `AssignCase` exitoso: `_trato = PasosDeTrato` (crédito sembrado lleno — el cliente llega
  dispuesto).
- `Notify`: `Improved` ⇒ `_trato++` saturado en `PasosDeTrato`; `Worsened` ⇒ `_trato--` con piso
  en `0`. `!change.Changed` ⇒ no-op (cubre `default(ReceptivityChange)`).

Lo que hace que esto funcione con **las dos** lecturas (crédito preservado con caso, racha estilo
M9 sin caso) es que **la única diferencia es el valor inicial**, no la regla. Verificación contra
los 7 `[Test]` heredados, todos sobre el sujeto **sin caso** (`_trato` arranca en 0):

| `[Test]` heredado | Por qué pasa |
|---|---|
| `Arranca_sin_progreso_y_sin_completar` | `_trato = 0` ⇒ `0/5 = 0f` exacto |
| `El_progreso_siempre_esta_entre_cero_y_uno` | `_trato` clampeado en `[0,5]` ⇒ `t ∈ [0,1]` |
| `Completar_implica_progreso_total` | 5 `Mejora()` seguidas ⇒ `_trato = 5` ⇒ `1f` exacto |
| `El_progreso_no_baja_de_cero_por_maltrato` | 10 `Empeora()` desde 0, piso en 0 ⇒ `0f` exacto |
| `IsComplete_es_verdadero_si_y_solo_si_el_progreso_es_total` | `t = _trato/5` es exacto en `float`; `IsComplete` lo lee del mismo valor |
| `La_completitud_es_reversible` | llega a 1 (no hay `Assume` omitido), y un `Empeora()` baja a `4/5 = 0.8` |
| `Notify_con_el_valor_por_defecto_es_no_op` | guarda `if (!change.Changed) return;` |

**Divergencia consciente frente a M9 (AD6 de M9).** `TriageScenarioObjective` pone la racha en
**0** ante un `Worsened` ("reinicio total"). M10 resta **1**. Es el punto exacto que el riesgo
alto de la propuesta pedía resolver: con el cliente arrancando en `Receptivo`, un reinicio total
haría irrecuperable un solo gesto malo, y el estudiante quedaría topado por debajo de 1 sin
manera de volver. El decremento simétrico es lo que hace que "cada `Improved` lo recupera" sea
literalmente cierto.

## Architecture Decisions

| # | Decisión | Elección | Alternativas rechazadas | Razón |
|---|---|---|---|---|
| AD1 | Cómo se lee la vía de trato | **Libro mayor simétrico** `_trato ∈ [0, PasosDeTrato]`, sembrado lleno por `AssignCase` y en 0 al construir | Racha de mejoras estilo M9 (`Worsened ⇒ 0`); contar `Improved` acumulados sin techo | Cierra el riesgo alto de la propuesta. Con el cliente en `Receptivo` desde el turno 1 (decisión 1) una racha de mejoras es **inalcanzable en una sesión perfecta**: sin caídas no hay `Improved` y `Notify` solo llega en transiciones, así que el progreso quedaría topado por debajo de 1 jugando bien. El sembrado lleno tiene precedente exacto en AD11 de M9 ("`AssignCase` puede subir el progreso") |
| AD2 | Una sola regla de movimiento para las dos lecturas | El valor **inicial** distingue "crédito preservado" (con caso) de "racha desde cero" (sin caso); la regla `±1` es idéntica | Dos campos separados (`_racha` para la base de contrato, `_credito` para la sesión real) | Dos campos significan dos aritméticas que hay que mantener en paridad a mano y una prueba que nadie escribe. Un campo con dos valores iniciales satisface los 7 `[Test]` heredados **sin ningún `Assume` omitido** (tabla de arriba) y deja la renormalización como un `if` de una línea, igual que `if (_clave == null) return Clamp01((float)r);` en M9 |
| AD3 | Pesos complementarios, no sueltos | `PesoDeLevantamiento = 1f - PesoDeTrato` y `PesoDeCierre = 1f - PesoDeCobertura`, propiedades derivadas | Tres pesos independientes que el autor del asset debe hacer sumar 1 | Copia literal de AD4 de M9. El techo de la mezcla es **exactamente 1.0f** por construcción, no por disciplina del editor, y desaparece el caso patológico de pesos que suman 0. El `clamp01` final queda como red de seguridad, no como mecanismo |
| AD4 | Denominador de la cobertura | Fracción sobre `_checklist.Count` del caso cargado. **Checklist vacío o inválido ⇒ `HasCase == false`** y renormalización | Constante C# de "requerimientos esperados"; tratar el checklist vacío como cobertura satisfecha (AD11 de M9) | Los casos reales traen entre 4 y 6 requerimientos (`caso-juntas-03` tiene 4): un contador absoluto contra una constante daría progreso distinto por caso para el mismo desempeño. Rechazar el checklist vacío en vez de darlo por satisfecho es **más estricto que M9 a propósito**: un caso sin requerimientos no es un caso con cero pendientes, es un caso que no cargó, y M9 no podía distinguirlos porque `banderasRojas` sí puede legítimamente venir vacío |
| AD5 | `RegisterDisclosure` recibe el DTO completo | `void RegisterDisclosure(Core.RequirementResponse response)` — la regla "solo `Revelado` cuenta" vive **adentro** de M10 | `RegisterDisclosure(RequirementId)` pelado; `RegisterDisclosure(RequirementId, RequirementOutcome)` | `Core.RequirementResponse` vive en `NpcAi.Core`, así que M10 lo acepta sin referenciar M16 (regla 3). El compositor reenvía los tres resultados del turno **a ciegas** y no puede equivocarse al filtrar; con un `RequirementId` pelado, cada compositor futuro duplicaría el `if (Outcome == Revelado)` y el primero que lo olvide acredita un `AunNoRevelado` como si fuera un dato entregado |
| AD6 | Escribir `Core.RequirementResponse` calificado | Siempre calificado en firmas y cuerpos, aunque **hoy compile sin el prefijo** | `RequirementResponse` pelado apoyándose en `using NpcAi.Core;` | Hoy compila porque `NpcAi.Scenarios.Boardroom` no referencia el ensamblado de M16. Pero la búsqueda de nombres de C# sube por los namespaces contenedores hasta `NpcAi`, donde `NpcAi.RequirementResponse` **es** un miembro llamado `RequirementResponse` y gana contra un tipo importado por `using`. El día que alguien agregue esa referencia, todas las firmas se rompen de golpe. Mismo prefijo y misma razón que documenta `RequirementResponder.cs` en M16 |
| AD7 | Dónde vive la fórmula de mezcla | Método `Mezclar(bool hayCaso, float cobertura, float cierre, float trato)` **en `BoardroomObjectiveSettings`**; lo llaman la implementación real y el doble | Duplicar la aritmética en el doble; clase estática nueva `BoardroomProgress.cs` | La fórmula es la razón de ser de los pesos, así que vive con ellos: `TriageObjectiveSettings` ya calcula derivados (`PesoClinico`, `PesoDeTriaje`), esto es el paso siguiente natural y **no agrega ningún archivo** al presupuesto. Que el doble la llame hace la paridad **estructural**, no aspiracional — mismo criterio que `ScriptedRequirementResponder` reusando `RequirementMatcher` en M16. Una clase aparte sería un 7.º archivo de runtime más su archivo de prueba (`strict_tdd`), ~120 líneas por cero beneficio |
| AD8 | Fidelidad del cierre: igualdad de conjuntos **recalculada**, no congelada | `_resumen` se guarda tal cual y `k` se recalcula en cada lectura de `Progress01` como `_resumen.SetEquals(_revelados)` | Congelar `k` en el momento de `PresentSummary`; invalidar `_resumen` dentro de `RegisterDisclosure` | Revelar algo **después** de cerrar debe hacer que el cierre deje de ser fiel hasta volver a presentarlo, y eso es exactamente la completitud reversible y no pegajosa que exige el puerto. Recalcular lo da gratis, sin estado extra y sin que `RegisterDisclosure` tenga que saber que existe un cierre |
| AD9 | Cierre con **cero** revelaciones no acredita | `k = 1` exige `_resumen != null && _revelados.Count > 0 && _resumen.SetEquals(_revelados)` | Igualdad de conjuntos pura (vacío == vacío ⇒ `k = 1`) | No reabre la decisión 2 (la coincidencia sigue siendo **exacta**): solo descarta el caso degenerado. Con igualdad pura, cerrar en el turno 1 sin preguntar nada daría `0.20 (trato) + 0.20 (cierre) = 0.40` — el 40 % del objetivo por levantarse de la mesa. Un resumen de nada no es un cierre, es abandonar la reunión. **Ver R1**: `sdd-spec` corre en paralelo y debe reflejar este borde |
| AD10 | Proyección mínima del catálogo de M16 | `RequirementChecklistLoader` declara **solo** `id` y `requerimientos[].id` en sus clases `Raw*`; `JsonUtility` ignora el resto | Referenciar `NpcAi.RequirementResponse` y reusar `RequirementCase`; proyectar el archivo entero | Referenciar M16 viola la regla 3. Precedente literal: `TriageKeyLoader` declara solo `clave` para leer `Data/Cases/*.json` sin referenciar `NpcAi.ClinicalResponse` (AD1 de M9). `id` y `requerimientos[].id` son la parte **más estable** del esquema — `respuesta`, `receptividadMinima`, `ejemplosDePregunta` y los tags pueden cambiar sin mover un byte de M10 |
| AD11 | `RequirementChecklist` guarda `RequirementId`, no `string` | Lista ordenada de `RequirementId` + `HashSet<RequirementId>` interno para `Contains` | `IReadOnlyList<string>` como `TriageKey.BanderasRojas`; índices `int` como `RegisterRedFlag(int)` | Divergencia deliberada frente a M9, y es una mejora: el constructor de `RequirementId` ya normaliza (trim + minúsculas), así que la comparación queda normalizada **en los dos lados** sin escribir un normalizador. Además el contrato público de `RegisterDisclosure` y `PresentSummary` es por id, no por índice: el hueco `campoDe(clin)` que sigue abierto entre M9 y M15 aquí no existe |
| AD12 | `RequirementChecklistLoader` **no** exige el mínimo de 4 requerimientos | Acepta cualquier checklist con ≥ 1 id válido; rechaza vacío | Replicar `MinimoRequerimientos = 4` de `RequirementCaseLoader` | Precedente literal de AD3 de M9: `TriageKeyLoader` "valida un contrato distinto sobre el mismo archivo" y por eso no exige los 8 `hechos` que sí exige `ClinicalCaseLoader`. M10 solo necesita un denominador no nulo; el mínimo de 4 es invariante de M16 y ya lo hacen cumplir `RequirementCaseLoader` y `RequirementCasesDataTests`. Duplicarlo haría que M10 fallara por una razón que no es suya |
| AD13 | Pesos en el `.asset`, constantes C# solo como red | `Data/Scenarios/Boardroom.asset` manda; las `const` de `BoardroomObjectiveSettings` son el valor por defecto del constructor cuando no hay asset | Solo constantes (lo que hizo M9); solo asset sin defaults | Regla 7 es literal: umbrales son dato. Pero el constructor sin parámetros tiene que producir un sujeto válido porque es lo que usa `ScenarioObjectiveContract.CreateSubject()`. Misma división y misma razón que `PersonalityStyleBank.Fallback` en M16 (AD10): el dato de tuning es externo, la garantía del contrato vive en código |

## Data Flow

    (compositor de sala de juntas — M11, NO es de este cambio)
              │  elige RequirementCaseId + PersonalityId; provee cargarJson y los pesos
              ▼
    new RequirementsScenarioObjective(asset.ToSettings(), cargarJson)
              │
              ▼
    objetivo.AssignCase(caseId)                          ← una vez por sesión
              │   cargarJson(caseId) → JSON → RequirementChecklistLoader.TryParse
              │   éxito  ⇒ _checklist poblado, _revelados/_resumen limpios, _trato = PasosDeTrato
              │   fallo  ⇒ HasCase == false, renormalización (nunca lanza, nunca propaga)
              ▼
    ┌── por turno, el compositor reenvía a ciegas ─────────────────────────────┐
    │                                                                          │
    │  motor M4 ──ReceptivityChange──► objetivo.Notify(change)                 │
    │                                    _trato ±1, saturado en [0, PasosDeTrato]
    │                                                                          │
    │  M16.Respond(...) ──Core.RequirementResponse──► objetivo.RegisterDisclosure(r)
    │                                    Outcome != Revelado          ⇒ descarta
    │                                    !_checklist.Contains(r.Id)   ⇒ descarta
    │                                    si no                        ⇒ _revelados.Add(r.Id)
    │                                                                  (idempotente: HashSet)
    └──────────────────────────────────────────────────────────────────────────┘
              │
              ▼
    objetivo.PresentSummary(ids)                         ← cierre explícito, sobrescribible
              │   _resumen = new HashSet<RequirementId>(ids ?? vacío)
              │   sin efecto antes de AssignCase (igual que DeclareTriage)
              ▼
    objetivo.Progress01
              │   c = _revelados.Count / _checklist.Count
              │   k = (_resumen != null && _revelados.Count > 0
              │        && _resumen.SetEquals(_revelados)) ? 1 : 0      ← recalculado (AD8/AD9)
              │   t = _trato / PasosDeTrato
              ▼
        _pesos.Mezclar(HasCase, c, k, t)   → [0,1]       ← único lugar de la fórmula (AD7)
              │
              ▼
        IsComplete  ⇔  |Progress01 - 1| <= 1e-4          ← reversible, no pegajosa

## File Inventory

| Archivo | Acción | Rol |
|---|---|---|
| `Runtime/Scenarios/Boardroom/RequirementsScenarioObjective.cs` | Crear | `IScenarioObjective` real + superficie aditiva |
| `Runtime/Scenarios/Boardroom/RequirementChecklist.cs` | Crear | POCO: id del caso + ids de requerimiento ordenados + `Contains` |
| `Runtime/Scenarios/Boardroom/RequirementChecklistLoader.cs` | Crear | `TryParse(string, out RequirementChecklist)` + clases `Raw*` mínimas |
| `Runtime/Scenarios/Boardroom/Config/BoardroomObjectiveSettings.cs` | Crear | POCO de pesos + `Mezclar` (AD7). C# puro |
| `Runtime/Scenarios/Boardroom/Unity/BoardroomObjectiveSettingsAsset.cs` | Crear | `ScriptableObject` → POCO. Sin lógica |
| `Runtime/Scenarios/Boardroom/Fakes/ScriptedScenarioObjective.cs` | **Modificar** | Rediseño: deja de ser el calco de M9 |
| `Data/Scenarios/README.md` | Crear | Esquema de pesos, rangos válidos, efecto de cada uno |
| `Data/Scenarios/Boardroom.asset` | Crear | Los pesos reales (`0.2` / `0.75` / `5`) |
| `Tests/EditMode/Scenarios/Boardroom/RequirementsScenarioObjectiveTests.cs` | Crear | `: ScenarioObjectiveContract` sobre la real |
| `Tests/EditMode/Scenarios/Boardroom/ScriptedScenarioObjectiveTests.cs` | Sin cambio | Ya existe y ya hereda la base |
| `Tests/EditMode/Scenarios/Boardroom/RequirementsProgresoTests.cs` | Crear | Las tres vías, sueltas y combinadas (tabla de sanidad) |
| `Tests/EditMode/Scenarios/Boardroom/RequirementsSuperficieAditivaTests.cs` | Crear | `AssignCase` / `RegisterDisclosure` / `PresentSummary` |
| `Tests/EditMode/Scenarios/Boardroom/ResetParityTests.cs` | Crear | `Reset()` real y doble: mismo estado, idempotente |
| `Tests/EditMode/Scenarios/Boardroom/RequirementChecklistLoaderTests.cs` | Crear | Unidad del cargador contra JSON en memoria |
| `Tests/EditMode/Scenarios/Boardroom/RequirementChecklistDataTests.cs` | Crear | Los 4 `caso-juntas-0N.json` **reales** contra la proyección |
| `Tests/EditMode/Scenarios/Boardroom/BoardroomObjectiveSettingsTests.cs` | Crear | `Mezclar`: complementos, clamp, renormalización |
| `Tests/EditMode/Scenarios/Boardroom/BoardroomObjectiveSettingsAssetTests.cs` | Crear | Proyección asset → POCO; defaults sin asset |
| `Docs/MODULES.md` | Modificar | Sección M10: de "solo doble" a implementación real |
| `Runtime/Scenarios/Boardroom/NpcAi.Scenarios.Boardroom.asmdef` | Sin cambio | Ya referencia lo correcto |
| `Tests/EditMode/Scenarios/Boardroom/NpcAi.Scenarios.Boardroom.Tests.asmdef` | Sin cambio | Ya existe con `includePlatforms: ["Editor"]` |
| `Runtime/Core/`, `Runtime/CoreChannels/`, `Tests/EditMode/Core/` | Sin cambio | Contrato intacto; la base se hereda |
| `Runtime/RequirementResponse/`, `Data/Requirements/` | Sin cambio | M16 se lee por archivo, no se toca |
| `Runtime/Scenarios/Emergency/`, `Data/Personalities/` | Sin cambio | Otro dueño / otro cambio |

Los `.meta` de todo archivo y carpeta nuevos **se versionan** (convención vigente del repo).

## Interfaces / Contracts

### `Runtime/Scenarios/Boardroom/Config/BoardroomObjectiveSettings.cs`

```csharp
namespace NpcAi.Scenarios.Boardroom
{
    /// <summary>
    /// Pesos y umbrales de <see cref="RequirementsScenarioObjective"/>, inyectables.
    /// <c>Config/</c> es una carpeta, no un sub-namespace (mismo patron que M9).
    /// Dos pares de fracciones complementarias (AD3): el techo de la mezcla es siempre
    /// exactamente 1.0f y no existe el caso patologico de pesos sueltos que sumen 0.
    /// Las constantes son la red de seguridad para cuando no hay asset (AD13); el valor
    /// de tuning vive en Data/Scenarios/Boardroom.asset (regla 7).
    /// </summary>
    public sealed class BoardroomObjectiveSettings
    {
        /// <summary>Decision de negocio de Jefferson (2026-09-21, pregunta 3): peso bajo.</summary>
        public const float PesoDeTratoPorDefecto     = 0.2f;
        /// <summary>Dentro de la mitad de levantamiento. Cobertura manda 3-a-1 sobre cierre.</summary>
        public const float PesoDeCoberturaPorDefecto = 0.75f;
        /// <summary>Empeoramientos que hace falta acumular para vaciar el credito de trato.</summary>
        public const int   PasosDeTratoPorDefecto    = 5;

        /// <summary>[0,1]. El complemento es <see cref="PesoDeLevantamiento"/>.</summary>
        public float PesoDeTrato { get; }

        /// <summary>[0,1] dentro del levantamiento. El complemento es <see cref="PesoDeCierre"/>.</summary>
        public float PesoDeCobertura { get; }

        /// <summary>Pasos del libro mayor de trato. Minimo 1 (elimina la division por cero).</summary>
        public int PasosDeTrato { get; }

        /// <summary>AD3: complemento exacto de <see cref="PesoDeTrato"/>.</summary>
        public float PesoDeLevantamiento => 1f - PesoDeTrato;

        /// <summary>AD3: complemento exacto de <see cref="PesoDeCobertura"/>.</summary>
        public float PesoDeCierre => 1f - PesoDeCobertura;

        public BoardroomObjectiveSettings(
            float pesoDeTrato     = PesoDeTratoPorDefecto,
            float pesoDeCobertura = PesoDeCoberturaPorDefecto,
            int   pasosDeTrato    = PasosDeTratoPorDefecto);

        /// <summary>
        /// AD7: UNICO lugar donde viven las tres vias mezcladas. La llaman la implementacion
        /// real y el doble, asi la paridad no depende de copiar aritmetica a mano.
        /// Sin caso (<paramref name="hayCaso"/> falso) renormaliza: el trato es el 100 %.
        /// Devuelve siempre [0,1].
        /// </summary>
        public float Mezclar(bool hayCaso, float cobertura, float cierre, float trato);
    }
}
```

### `Runtime/Scenarios/Boardroom/Unity/BoardroomObjectiveSettingsAsset.cs`

```csharp
namespace NpcAi.Scenarios.Boardroom.Unity
{
    /// <summary>
    /// Pesos del objetivo de sala de juntas, editables en el Inspector. Es SOLO dato:
    /// <see cref="ToSettings"/> proyecta al POCO puro y no toma una sola decision
    /// (espejo exacto de ReceptivityProfileAsset.ToProfile, M4). Aqui vive todo el
    /// contacto de la configuracion de M10 con UnityEngine.
    /// </summary>
    [CreateAssetMenu(
        fileName = "BoardroomObjective",
        menuName = "NpcAi/Escenarios/Objetivo de sala de juntas")]
    public sealed class BoardroomObjectiveSettingsAsset : ScriptableObject
    {
        [Tooltip("Peso de la via de trato. El complemento es el levantamiento.")]
        [Range(0f, 1f)] public float pesoDeTrato = 0.2f;

        [Tooltip("Peso de la cobertura DENTRO del levantamiento. El complemento es el cierre.")]
        [Range(0f, 1f)] public float pesoDeCobertura = 0.75f;

        [Tooltip("Empeoramientos seguidos que vacian el credito de trato.")]
        [Min(1)] public int pasosDeTrato = 5;

        /// <summary>Proyeccion al tipo puro. Sin logica: copia.</summary>
        public BoardroomObjectiveSettings ToSettings() =>
            new BoardroomObjectiveSettings(pesoDeTrato, pesoDeCobertura, pasosDeTrato);
    }
}
```

### `Runtime/Scenarios/Boardroom/RequirementChecklist.cs`

```csharp
namespace NpcAi.Scenarios.Boardroom
{
    /// <summary>
    /// Proyeccion MINIMA del catalogo de M16 (AD10): el id del caso y los ids de sus
    /// requerimientos, nada mas. POCO sin UnityEngine; quien lo pobla es
    /// <see cref="RequirementChecklistLoader"/>. Espejo de TriageKey (M9), pero guarda
    /// RequirementId y no string (AD11): el constructor del id ya normaliza, asi que la
    /// comparacion queda normalizada en los dos lados sin escribir un normalizador.
    /// </summary>
    public sealed class RequirementChecklist
    {
        /// <summary>Id del caso, p. ej. "caso-juntas-03". Solo diagnostico.</summary>
        public string Id { get; }

        /// <summary>Lista ORDENADA y estable, sin duplicados ni ids vacios.</summary>
        public IReadOnlyList<RequirementId> Requerimientos { get; }

        /// <summary>Denominador de la via de cobertura. Nunca 0 en un checklist valido (AD4).</summary>
        public int Count => Requerimientos.Count;

        /// <summary>O(1). Base de "ese requerimiento no pertenece al caso asignado".</summary>
        public bool Contains(RequirementId id);

        public RequirementChecklist(string id, IReadOnlyList<RequirementId> requerimientos);
    }
}
```

### `Runtime/Scenarios/Boardroom/RequirementChecklistLoader.cs`

```csharp
namespace NpcAi.Scenarios.Boardroom
{
    /// <summary>
    /// De JSON (esquema de Data/Requirements/README.md, M16) a
    /// <see cref="RequirementChecklist"/>. Deliberadamente NO mapea "cliente",
    /// "respuesta", "receptividadMinima", "ejemplosDePregunta" ni los tags: las clases
    /// Raw* de abajo declaran unicamente lo minimo, asi que JsonUtility ignora el resto
    /// del archivo (AD10, espejo de TriageKeyLoader). NO exige el minimo de 4
    /// requerimientos que exige RequirementCaseLoader: valida un contrato distinto sobre
    /// el mismo archivo (AD12). Recibe string, nunca File.ReadAllText.
    /// </summary>
    public static class RequirementChecklistLoader
    {
        /// <summary>
        /// <c>true</c> solo si hay "id" no vacio y al menos un "requerimientos[].id" valido.
        /// Descarta ids vacios y duplicados conservando el orden del archivo (gana el
        /// primero). Nunca lanza: JSON nulo/vacio/malformado devuelve <c>false</c>.
        /// </summary>
        public static bool TryParse(string json, out RequirementChecklist checklist);

        // Espejo MINIMO del esquema de M16. Nombres en minusculas a proposito: JsonUtility
        // empareja por nombre exacto. [Serializable] es obligatorio en toda clase anidada.
        [Serializable] private sealed class RawCase
        {
            public string id;
            public RawRequerimiento[] requerimientos;
        }

        [Serializable] private sealed class RawRequerimiento
        {
            public string id;   // lo UNICO que M10 necesita de cada requerimiento
        }
    }
}
```

### `Runtime/Scenarios/Boardroom/RequirementsScenarioObjective.cs`

```csharp
namespace NpcAi.Scenarios.Boardroom
{
    /// <summary>
    /// M10 — implementacion real de <see cref="IScenarioObjective"/> para la sala de juntas
    /// (levantamiento de requerimientos). Mezcla TRES vias: cobertura del catalogo del caso,
    /// cierre explicito y fiel, y trato sostenido. Sin caso asignado
    /// (<see cref="HasCase"/> falso) los pesos se renormalizan y el trato es el 100 % del
    /// progreso, que es lo que ejercita ScenarioObjectiveContract via el constructor sin
    /// parametros. Superficie publica en ingles como M9; helpers privados en espanol.
    /// </summary>
    public sealed class RequirementsScenarioObjective : IScenarioObjective
    {
        /// <summary>Sujeto sin caso: solo la via de trato. Lo usa la gemela de contrato.</summary>
        public RequirementsScenarioObjective();

        /// <param name="pesos">Null cae en los valores por defecto (AD13).</param>
        /// <param name="cargarJson">
        /// De un RequirementCaseId al JSON del caso, o null. PUEDE ser null: se trata como
        /// "ningun caso carga" y AssignCase nunca lanza (mismo contrato defensivo que
        /// TriageScenarioObjective y que RequirementResponder/AD9 de M16).
        /// </param>
        public RequirementsScenarioObjective(
            BoardroomObjectiveSettings pesos, Func<RequirementCaseId, string> cargarJson);

        // --- Lecturas aditivas ---

        /// <summary><c>true</c> solo si hay un checklist cargado con al menos un id (AD4).</summary>
        public bool HasCase { get; }

        /// <summary>Total de requerimientos del caso asignado; 0 sin caso. Denominador.</summary>
        public int RequirementCount { get; }

        /// <summary>Ids distintos ya acreditados como revelados. Numerador.</summary>
        public int RequirementsDisclosed { get; }

        /// <summary>
        /// <c>true</c> si hay un resumen presentado, se revelo al menos un requerimiento
        /// y el conjunto presentado es EXACTAMENTE el revelado (AD8/AD9). Se recalcula en
        /// cada lectura: revelar algo despues de cerrar lo vuelve infiel hasta volver a
        /// presentar.
        /// </summary>
        public bool SummaryIsFaithful { get; }

        // --- IScenarioObjective (puerto congelado) ---

        public float Progress01 { get; }
        public bool IsComplete { get; }

        /// <summary>
        /// Libro mayor simetrico (AD1): Improved suma 1 saturado en PasosDeTrato, Worsened
        /// resta 1 con piso en 0. Divergencia consciente frente a AD6 de M9, que reinicia a 0.
        /// Notify(default) es no-op via !change.Changed.
        /// </summary>
        public void Notify(ReceptivityChange change);

        // --- Superficie aditiva de M10 ---

        /// <summary>
        /// Vincula un caso y carga su checklist. Descarta SIEMPRE el progreso anterior
        /// (revelados y resumen), incluso si la carga falla. Siembra el credito de trato
        /// LLENO: el cliente llega dispuesto (decision 1 de Jefferson). Precedente de
        /// "AssignCase puede subir el progreso": AD11 de M9. Id desconocido, cargador null
        /// o JSON invalido dejan HasCase en false. Nunca lanza, nunca propaga.
        /// </summary>
        public void AssignCase(RequirementCaseId caseId);

        /// <summary>
        /// Acredita SOLO Outcome == Revelado de un RequirementId que pertenezca al caso
        /// asignado (AD5). Idempotente por id (HashSet), mismo criterio que RegisterRedFlag
        /// en M9. Sin efecto antes de AssignCase. Nunca lanza.
        /// "Core." calificado a proposito (AD6).
        /// </summary>
        public void RegisterDisclosure(Core.RequirementResponse response);

        /// <summary>
        /// Cierre explicito, analogo a DeclareTriage en M9. Sobrescribible: la ultima
        /// llamada gana. null o coleccion vacia se tratan como resumen vacio, nunca lanzan.
        /// Sin efecto antes de AssignCase. Cerrar antes de cubrir el catalogo esta PERMITIDO
        /// y acredita su componente: es un cierre temprano honesto, con Progress01 < 1.
        /// </summary>
        public void PresentSummary(IReadOnlyCollection<RequirementId> requirementIds);

        /// <summary>
        /// Vuelve al estado recien construido: checklist descartado, revelados y resumen
        /// limpios, credito de trato en 0. Idempotente. API real y probada, no codigo
        /// muerto (decision 5 de Jefferson); su paridad con el doble se prueba en el
        /// modulo, no ampliando ScenarioObjectiveContract (regla 2).
        /// </summary>
        public void Reset();
    }
}
```

### `Runtime/Scenarios/Boardroom/Fakes/ScriptedScenarioObjective.cs` (rediseño)

Deja de ser el contador `±1` sobre 4 pasos fijos copiado de M9. Pasa a **espejar la superficie
aditiva real**, firma por firma: `AssignCase`, `RegisterDisclosure`, `PresentSummary`, `Reset`,
más `HasCase` / `RequirementCount` / `RequirementsDisclosed` / `SummaryIsFaithful`.

- **Sin IO y sin `Data/`**: construye sus `RequirementChecklist` con el constructor del POCO, no
  con el cargador — así el doble queda C# puro, sin `JsonUtility` (mismo criterio que
  `ScriptedRequirementResponder`, que es puro y sin IO).
- **Reconoce los 4 ids reales** (`caso-juntas-01` … `caso-juntas-04`) contra una tabla embebida de
  4 `RequirementId` por caso; cualquier otro id deja `HasCase == false`.
- **Reusa `BoardroomObjectiveSettings.Mezclar`** (AD7): la paridad aritmética con la
  implementación real es estructural, no replicada a mano.
- Mismo libro mayor simétrico de trato, así que `ScriptedScenarioObjectiveTests` sigue pasando
  los 7 `[Test]` heredados **sin cambiar ni una línea del archivo de prueba** (ya existe, 11
  líneas, y no entra en este diff).

## Esquema de datos — `Data/Scenarios/`

Carpeta **nueva** (hoy `Data/` tiene `Cases`, `Corpus`, `Dialogue`, `Personalities`,
`Presentation`, `Requirements`, `Speech`, `VrInput`). Un `.asset` por escenario; M9 podrá sumar
el suyo después sin tocar este cambio.

`Data/Scenarios/Boardroom.asset` — instancia de `BoardroomObjectiveSettingsAsset`:

| Campo | Valor | Rango válido | Qué controla |
|---|---|---|---|
| `pesoDeTrato` | `0.2` | `[0,1]` | Cuánto del progreso total depende del trato. El complemento (`0.8`) es el levantamiento |
| `pesoDeCobertura` | `0.75` | `[0,1]` | Reparto **dentro** del `0.8`: cobertura `0.6`, cierre `0.2` |
| `pasosDeTrato` | `5` | `>= 1` | Granularidad del castigo: cada `Worsened` no recuperado cuesta `pesoDeTrato / pasosDeTrato` = `0.04` |

`Data/Scenarios/README.md` documenta la tabla de arriba, la fórmula aplanada
(`0.20·t + 0.60·c + 0.20·k`), la tabla de sanidad, y la regla de oro: **subir `pesoDeTrato` por
encima de `0.5` invierte la intención del escenario** — el ejercicio pasaría a premiar apaciguar
al cliente por encima de levantar sus requerimientos, que es exactamente lo que la decisión 1 de
Jefferson descartó.

## Testing Strategy

| Archivo | Qué fija |
|---|---|
| `ScenarioObjectiveContract` (heredada, **sin tocar**) | Los 7 `[Test]` del puerto. `CreateSubject()` usa el constructor sin parámetros ⇒ siempre la vía renormalizada. La tabla de *Aritmética* arriba verifica uno por uno que **ninguno queda omitido por `Assume`** |
| `RequirementsScenarioObjectiveTests : ScenarioObjectiveContract` | Hereda los 7 sobre la implementación real. ~20 líneas, espejo de `TriageScenarioObjectiveTests` |
| `ScriptedScenarioObjectiveTests : ScenarioObjectiveContract` | **Ya existe, no se toca.** Los mismos 7 sobre el doble rediseñado: paridad real (regla 4) |
| `RequirementsProgresoTests` | La tabla de sanidad completa, fila por fila, con tolerancia `1e-4`; las tres vías sueltas; renormalización sin caso; `clamp01`; `IsComplete` reversible con las tres vías activas; penalización de `0.04` por `Worsened` no recuperado |
| `RequirementsSuperficieAditivaTests` | `AssignCase`: id desconocido / cargador `null` / `Func` que lanza / JSON basura ⇒ `HasCase == false` sin lanzar; reasignar limpia el progreso anterior y resiembra el trato. `RegisterDisclosure`: idempotente por id; `AunNoRevelado` y `NoAplica` no mueven nada; `RequirementId` ajeno al caso se descarta; antes de `AssignCase` es no-op; `default(Core.RequirementResponse)` no lanza. `PresentSummary`: conjunto exacto acredita; falta uno ⇒ no; sobra uno ⇒ no; parcial exacto ⇒ **sí** (cierre temprano honesto); `null` y vacío no lanzan; sobrescribible; revelar después de cerrar vuelve el cierre infiel (AD8); resumen vacío con cero revelaciones **no** acredita (AD9) |
| `ResetParityTests` | `Reset()` en la real y en el doble: deja el mismo estado que recién construido (`Progress01 == 0`, `HasCase == false`, contadores en 0), idempotente, y vuelve a permitir `AssignCase` después |
| `RequirementChecklistLoaderTests` | Carga válida; `null`/vacío/basura/`"{}"`/`"[]"` ⇒ `false` sin lanzar; `id` vacío ⇒ `false`; `requerimientos` ausente o vacío ⇒ `false`; ids vacíos descartados; duplicados colapsados conservando orden; campos extra del esquema de M16 ignorados sin romper (AD10); **no** rechaza un caso de 1 requerimiento (AD12) |
| `RequirementChecklistDataTests` | Sobre los 4 `.json` **reales** (patrón `ClinicalCasesDataTests` / `RequirementCasesDataTests`: `AssetDatabase.FindAssets("t:TextAsset")` filtrado por `Data/Requirements/` + prefijo `caso-juntas-`): (1) están los 4; (2) cada uno proyecta con `TryParse`; (3) `checklist.Id` == nombre de archivo; (4) `Count >= 1` y cada `RequirementId` no es `None`; (5) sin duplicados dentro del caso; (6) el denominador variable real (4 a 6) queda registrado como aserción explícita |
| `BoardroomObjectiveSettingsTests` | `Mezclar`: complementos exactos (`PesoDeLevantamiento`, `PesoDeCierre`); techo exactamente `1f` con las tres vías en 1; renormalización con `hayCaso == false`; `clamp01` con entradas fuera de rango; `PasosDeTrato` mínimo 1; pesos fuera de `[0,1]` clampeados en el constructor |
| `BoardroomObjectiveSettingsAssetTests` | `ScriptableObject.CreateInstance` → `ToSettings()` copia los tres campos sin alterarlos; los defaults del asset coinciden con las `const` del POCO (AD13) |

Ejecución: Unity 6 Test Runner, EditMode, sin escena y sin VR. `AssetDatabase` obliga a
`includePlatforms: ["Editor"]` en el asmdef de pruebas — **ya lo está**. **Ningún agente ejecuta
Unity: el verde es compuerta humana.**

Gotchas heredadas del repo: Unity trae NUnit 3.5 — `Is.AnyOf` (3.6+) no compila y tumba la
assembly entera (M5); usar comparaciones planas. Nada de `Debug.Log` en runtime. Nunca
hacer merge a mano de YAML de Unity (`Boardroom.asset`): ante conflicto, descartar y rehacer.

## Threat Matrix

N/A — este cambio no toca enrutado de procesos, comandos de shell, subprocesos, automatización de
VCS/PR, clasificación de archivos ejecutables ni integración de procesos. Es un ensamblado de C#
puro + `JsonUtility` + un `ScriptableObject`, sobre archivos de datos versionados en el repo.

## Migration / Rollout — presupuesto de revisión y corte de PRs

Sin migración de datos: M10 es aditivo sobre un puerto que ya está en `main`, y el único archivo
**modificado** es el doble.

### Estimación de líneas, anclada contra el par homólogo real de M9

| Pieza | Homólogo M9 (líneas reales) | Est. M10 | Por qué difiere |
|---|---|---|---|
| `Config/BoardroomObjectiveSettings.cs` | `TriageObjectiveSettings.cs` (46) | **75** | 3 pesos en vez de 2 + `Mezclar` (AD7) |
| `Unity/BoardroomObjectiveSettingsAsset.cs` | `ReceptivityProfileAsset.cs` (107) | **45** | 3 campos y una proyección; sin structs anidados ni `BuildCatalog` |
| `RequirementChecklist.cs` | `TriageKey.cs` (37) | **55** | Suma `HashSet` interno + `Contains` + `Count` |
| `RequirementChecklistLoader.cs` | `TriageKeyLoader.cs` (64) | **75** | Suma dedupe y descarte de ids vacíos |
| `RequirementsScenarioObjective.cs` | `TriageScenarioObjective.cs` (163) | **200** | 3 vías en vez de 2; 4 mutadores y 4 lecturas |
| `Fakes/ScriptedScenarioObjective.cs` | actual: 31 (se reescribe) | **110** (+79 netas) | Superficie aditiva completa + tabla embebida de 4 casos |
| **Runtime** | | **~560** | |
| `RequirementChecklistLoaderTests.cs` | `TriageKeyLoaderTests.cs` (119) | **115** | Esquema más chico, más casos de borde |
| `RequirementsProgresoTests.cs` | `TriageProgresoTests.cs` (183) | **210** | Tabla de sanidad de 7 filas + 3 vías |
| `RequirementsSuperficieAditivaTests.cs` | `TriageSuperficieAditivaTests.cs` (237) | **230** | Mismo tamaño; 3 mutadores en vez de 3 |
| `RequirementsScenarioObjectiveTests.cs` | `TriageScenarioObjectiveTests.cs` (18) | **20** | Idéntico |
| `ResetParityTests.cs` | — (M9 no lo tiene) | **75** | Pieza nueva (decisión 5) |
| `BoardroomObjectiveSettingsTests.cs` | — (M9 no lo tiene) | **90** | `strict_tdd`: `Mezclar` es producción |
| `BoardroomObjectiveSettingsAssetTests.cs` | — (M9 no lo tiene) | **70** | `strict_tdd`: el asset es producción |
| `RequirementChecklistDataTests.cs` | — (vive en M15/M16) | **95** | Los 4 JSON reales |
| **Pruebas** | | **~905** | |
| `Data/Scenarios/README.md` | `Data/Requirements/README.md` (~70) | **55** | 3 campos, una fórmula |
| `Data/Scenarios/Boardroom.asset` | — | **20** | YAML de Unity |
| `Docs/MODULES.md` | — | **25** | |
| **Total autorado** | | **~1.565** | |

Más ~110 líneas de `.meta` generados por Unity (≈ 14 archivos y carpetas nuevos × 8), que
aparecen en el diff pero no son texto autorado.

**≈ 3,9 × el presupuesto de 400 líneas. Un PR único no es viable.**

- `Decision needed before apply: Yes`
- `Chained PRs recommended: Yes`
- `400-line budget risk: High`

### Corte propuesto — 4 PRs encadenados

Cada corte compila solo, tiene sus pruebas en verde por sí mismo y revertirlo no deja nada a
medias. El encadenamiento sigue el patrón ya usado en M0 y M16: PR1 apunta a la rama de la
feature y cada hijo apunta a la rama del PR anterior.

| PR | Rama | Contenido | Est. | Cierre verificable |
|---|---|---|---|---|
| **PR1** | `feat/m10-pesos-y-asset` → rama de la feature | `Config/BoardroomObjectiveSettings.cs`, `Unity/BoardroomObjectiveSettingsAsset.cs`, `Data/Scenarios/README.md`, `Data/Scenarios/Boardroom.asset`, `BoardroomObjectiveSettingsTests.cs`, `BoardroomObjectiveSettingsAssetTests.cs` | **~355** | La fórmula existe y está probada aislada: complementos exactos, techo `1f`, renormalización, clamp. Ningún objetivo todavía |
| **PR2** | `feat/m10-checklist-y-lector` → rama de PR1 | `RequirementChecklist.cs`, `RequirementChecklistLoader.cs`, `RequirementChecklistLoaderTests.cs`, `RequirementChecklistDataTests.cs` | **~340** | La proyección del catálogo de M16 está probada contra JSON en memoria **y** contra los 4 archivos reales. El acoplamiento por archivo queda cubierto antes de que nadie dependa de él |
| **PR3** | `feat/m10-objetivo-real` → rama de PR2 | `RequirementsScenarioObjective.cs`, `RequirementsScenarioObjectiveTests.cs`, `RequirementsSuperficieAditivaTests.cs` | **~450** | La implementación real pasa los **7 `[Test]` heredados sin `Assume` omitido** y toda su superficie aditiva. `IScenarioObjective` ya tiene su segunda implementación real |
| **PR4** | `feat/m10-doble-y-progreso` → rama de PR3 | `Fakes/ScriptedScenarioObjective.cs` (reescritura), `RequirementsProgresoTests.cs`, `ResetParityTests.cs`, `Docs/MODULES.md` | **~420** | Doble y real en paridad de superficie, de aritmética y de `Reset()`; la tabla de sanidad de las tres vías queda fijada fila por fila |

PR3 (~450) y PR4 (~420) quedan ~10 % arriba del presupuesto. **Válvula de alivio si el conteo
real lo confirma por encima:** partir PR3 en `feat/m10-objetivo-real` (clase + contrato heredado,
~220) y `feat/m10-superficie-aditiva` (~230), dando 5 PRs. **`sdd-tasks` confirma con el conteo
real; esta tabla es un pronóstico, no un compromiso.** Estrategia de entrega de la sesión:
`ask-on-risk`.

### Rollback

Revertir los commits borra `RequirementsScenarioObjective.cs`, `RequirementChecklist.cs`,
`RequirementChecklistLoader.cs`, `Config/`, `Unity/`, `Data/Scenarios/` y los 7 archivos nuevos de
`Tests/EditMode/Scenarios/Boardroom/`, **restaura el doble anterior** (único archivo modificado, no
creado, y su prueba de 11 líneas nunca se tocó) y devuelve la sección M10 de `Docs/MODULES.md` a
"solo doble". `IScenarioObjective` queda con una sola implementación real (M9), exactamente como
hoy. Ningún módulo depende de `RequirementsScenarioObjective` por nombre hasta que exista el
compositor de sala de juntas (M11). Sin migración de datos: `Data/Requirements/` no se toca.

## Open Questions / Riesgos del diseño

- **R1 — AD9 es un borde que la propuesta no resolvió y `sdd-spec` corre en paralelo.** La
  decisión 2 de Jefferson fija "coincidencia exacta" y la propuesta dice que el cierre acredita
  "aun si es parcial". Ninguna de las dos cubre el caso degenerado **resumen vacío con cero
  revelaciones**, donde la igualdad de conjuntos pura daría `k = 1` y cerrar en el turno 1 sin
  preguntar nada valdría `0.40`. Este diseño lo cierra con AD9 (`_revelados.Count > 0` como
  precondición). **No reabre la decisión 2**: la coincidencia sigue siendo exacta. Si el
  `spec.md` que se está escribiendo en paralelo escribió el escenario vacío-acredita, gana AD9 y
  hay que reconciliar un escenario antes de `sdd-tasks`.
- **R2 — AD1 diverge de AD6 de M9 y eso es visible en revisión.** M9 reinicia la racha a 0 ante
  `Worsened`; M10 resta 1. Un revisor que conozca M9 lo leerá como inconsistencia. Está
  documentado en el XML doc de `Notify` y en este diseño, pero es el punto que más probablemente
  genere conversación en PR3/PR4.
- **R3 — Presupuesto de revisión.** ~1.565 líneas autoradas ≈ 3,9× el budget. Ver el corte de 4
  PRs y su válvula de 5. `Decision needed before apply: Yes` (lo formaliza `sdd-tasks`).
- **R4 — Acoplamiento por archivo con el esquema de M16.** M10 lee `caso-juntas-0N.json` sin
  referenciar el ensamblado. Mitigado por AD10 (solo `id` + `requerimientos[].id`, la parte más
  estable) y por `RequirementChecklistDataTests`. Si M16 renombra `requerimientos` o
  `requerimientos[].id`, M10 deja de cargar **en silencio** (`HasCase == false`) hasta que esa
  prueba corra. Mismo acoplamiento que M9 ya tiene con `Data/Cases/`.
- **R5 — De dónde salen los bytes fuera del Editor (Quest).** Heredada tal cual de R4 de M16 y de
  AD3 de M15: `TryParse(string)` es agnóstico y `AD9`/`Func<...>` deja la estrategia afuera, pero
  nadie ha decidido si es `Resources/`, `StreamingAssets` o lectura de `Packages/`. **No bloquea
  M10** (las pruebas usan `AssetDatabase` en el Editor y JSON en memoria). Le toca a M11.
- **R6 — Acoplamiento suave doble ↔ catálogo real.** El doble reconoce `caso-juntas-01..04` como
  ids existentes. Si el catálogo cambia de nomenclatura hay que mover el doble en el mismo
  commit. Mismo acoplamiento que `ScriptedRequirementResponder` y `ScriptedClinicalResponder` ya
  tienen.
- **R7 — `Data/Scenarios/` nace con un solo `.asset`.** M9 sigue con sus pesos como constantes C#,
  así que la regla 7 queda cumplida para M10 y pendiente para M9. Es deuda registrada de M9, no
  de este cambio: meterla acá violaría la regla 1 (dos módulos en un cambio).
- **R8 — De la voz del estudiante a una lista de `RequirementId`** no hay pieza que lo resuelva
  hoy. Fuera de alcance por diseño (`PresentSummary` recibe ids, no texto). En el primer corte el
  arnés de M11 presenta el checklist y el estudiante selecciona.
- **R9 — Dependencia de archivo de la spec.** `2026-09-16-m0-puerto-requerimientos-juntas`
  todavía **no está archivado**, así que la spec de M10 debe referenciarlo desde la carpeta del
  cambio o archivarse M0 primero. Mismo cabo suelto que ya señalaron M16 y la propuesta de M10.
