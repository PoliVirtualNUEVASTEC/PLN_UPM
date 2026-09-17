# Tasks: M9 — Escenario Sala de Emergencia (implementación real de `IScenarioObjective`)

## Review Workload Forecast

| Field | Value |
|---|---|
| Estimated changed lines | ~840-950 (runtime+tests ~770-870, `Docs/MODULES.md` ~50-70); spec.md al archivar (~205) excluido del riesgo, copia idéntica ya revisada, mismo trato que M7 PR4 |
| 400-line budget risk | Alto frente al default del skill (400); frente al presupuesto de sesión (800) es más ajustado: el rango cruza 800, no queda cómodamente debajo |
| Chained PRs recommended | Sí (sugerido, no forzado por hábito — ver nota) |
| Suggested split | 3 unidades ligeras: lector → objetivo+superficie aditiva → docs/cierre; alternativa: `size:exception` en un solo PR si el equipo acepta el excedente moderado |
| Delivery strategy | ask-on-risk |
| Chain strategy | pending — el usuario debe elegir |

Decision needed before apply: Yes
Chained PRs recommended: No — decidido con el usuario 2026-09-17: un solo PR con `size:exception`
aceptado explícitamente, dado el excedente moderado (~840-950 vs. 800) y que el módulo es una
unidad cohesiva (una clase + su lector + su config, todo relacionado).
Chain strategy: no aplica (un solo PR)
400-line budget risk: High frente al default de 400 del skill; aceptado como size:exception frente
al presupuesto de sesión de 800

**A diferencia de M7** (~1200-1400 líneas, 50%+ sobre cualquier presupuesto): M9 no tiene capa de
envoltura `MonoBehaviour`, así que el excedente es más chico. No califica como "cómodamente bajo
800": el estimado cruza esa línea, así que se marca el riesgo en vez de aceptar un PR único en
silencio. La decisión de cadena vs. `size:exception` queda para el usuario/orquestador.

### Suggested Work Units

| Unit | Goal | PR | Focused test command | Runtime harness | Rollback boundary |
|---|---|---|---|---|---|
| 1 | Lector de `clave`: `TriageKey`, `TriageKeyLoader`, `TriageObjectiveSettings` (~295 líneas) | PR1 | EditMode: `TriageKeyLoaderTests` | N/A — C# puro, JSON en memoria, sin escena | Borrar los 3 archivos + su prueba; nada más depende de ellos todavía |
| 2 | `TriageScenarioObjective`: contrato heredado + mezcla + superficie aditiva (~545 líneas) | PR2 | EditMode: `TriageScenarioObjectiveTests` + `TriageProgresoTests` + `TriageSuperficieAditivaTests` | N/A — C# puro, sin escena/VR/audio | Borrar `TriageScenarioObjective.cs` + sus 3 pruebas; M9 vuelve a "solo doble" |
| 3 | Docs, spec y cierre (~70 líneas autoría + 205 copia excluida) | PR3 | Manual: revisión de que `Docs/MODULES.md` queda consistente | N/A — sin código | Revertir el commit de docs/spec; no toca `Runtime/` |

## Phase 0: Guardrails (leer antes de escribir)

- [x] 0.1 Confirmar frontera del diff: solo `Runtime/Scenarios/Emergency/`, `Tests/EditMode/Scenarios/Emergency/`, `Docs/` y `openspec/`. Nada en `Runtime/Core/`, `Runtime/CoreChannels/`, `Runtime/ClinicalResponse/`, `Runtime/Scenarios/Boardroom/`, `Data/`.
  <!-- apply Fases 0-3 (2026-09-17): confirmado por inspeccion antes de escribir. Este batch solo
  toco Runtime/Scenarios/Emergency/*.cs (+ Config/), Tests/EditMode/Scenarios/Emergency/*.cs y este
  tasks.md; ver stat real de `git diff --cached --stat` en el reporte de retorno al orquestador. -->
- [x] 0.2 Confirmar que NO es cambio de contrato: `IScenarioObjective`/`ReceptivityChange` (M0) no cambian firma.
  <!-- apply Fases 0-3 (2026-09-17): confirmado leyendo Runtime/Core/Ports.cs y Dtos.cs completos
  antes de escribir; ninguno de los dos archivos se toco ni una linea. -->
- [x] 0.3 Confirmar que `Tests/EditMode/Core/ScenarioObjectiveContract.cs` no se modifica.
  <!-- apply Fases 0-3 (2026-09-17): confirmado; solo se leyo para heredar de ella
  (TriageScenarioObjectiveTests). Cero escrituras sobre ese archivo. -->
- [x] 0.4 Confirmar que `Fakes/ScriptedScenarioObjective.cs` y ambos `.asmdef` (runtime y tests) quedan intactos — ya referencian `NpcAi.Core` + `NpcAi.Core.Channels`, ninguna referencia nueva.
  <!-- apply Fases 0-3 (2026-09-17): confirmado leyendo los 2 archivos .asmdef reales (no solo
  confiando en design.md, per instruccion explicita del batch): NpcAi.Scenarios.Emergency.asmdef
  referencia NpcAi.Core + NpcAi.Core.Channels; NpcAi.Scenarios.Emergency.Tests.asmdef referencia
  NpcAi.Core + NpcAi.Core.Tests + NpcAi.Scenarios.Emergency. Ninguno necesito cambio: los tipos
  nuevos de M9 no usan ningun canal (NpcAi.Core.Channels queda para el cableado futuro de M11,
  sin tocar), y las 4 clases de prueba nuevas no referencian nada fuera de esos 3 ensamblados ya
  presentes. A diferencia del defecto real de M7 PR3 (asmdef de pruebas sin NpcAi.Core.Channels),
  aqui no hay gap: NpcAi.Scenarios.Emergency.Tests.asmdef ya tenia todo lo necesario.
  ScriptedScenarioObjective.cs no se toco. -->

## Phase 1: TDD — Lector de `clave` (PR1)

- [x] 1.1 RED: `Tests/EditMode/Scenarios/Emergency/TriageKeyLoaderTests.cs` — caso feliz; `clave` ausente; JSON malformado; `triajeEsperado` "VI"/vacío; `banderasRojas` ausente/`[]`; JSON con `hechos` insuficientes (AD3: igual carga). JSON embebido como constante `string`, sin tocar `Data/Cases/`.
  <!-- apply Fases 0-3 (2026-09-17): creado con 8 metodos de prueba (10 ejecuciones: uno de ellos,
  el de triajeEsperado fuera de rango, esta parametrizado con 3 TestCase, incluyendo "2" para
  cubrir tambien el rechazo de notacion numerica que AD10 descarta para DeclareTriage). Cubre los
  6 casos nombrados en la nota mas 2 adicionales (banderasRojas ausente vs [] explicito, cada uno
  con su propia prueba en vez de una sola combinada). No compila hasta 1.2/1.3 (referencia TriageKey/TriageKeyLoader,
  que no existian aun) -- misma restriccion de unidad de compilacion por ensamblado que M7 PR1
  documento; razonado y escrito completo contra el pseudocodigo exacto de design.md ANTES de
  escribir TriageKeyLoader.cs. Escrito y creido correcto por inspeccion; confirmacion real en
  Test Runner queda pendiente como compuerta humana (tarea 4.1). -->
- [x] 1.2 GREEN: `Runtime/Scenarios/Emergency/TriageKey.cs` — POCO, nulos → vacío (espejo de `Hecho`).
  <!-- apply Fases 0-3 (2026-09-17): creado, espejo exacto de Hecho/ClinicalCase (M15): 4
  propiedades de solo lectura, nulos normalizados con `??` a string.Empty / Array.Empty<string>()
  en el constructor. Sin UnityEngine. -->
- [x] 1.3 GREEN: `Runtime/Scenarios/Emergency/TriageKeyLoader.cs` — `TryParse` + `RawCase`/`RawClave` privados anidados (AD1), valida `triajeEsperado` contra `{"I".."V"}` hasta verde en 1.1.
  <!-- apply Fases 0-3 (2026-09-17): creado, espejo exacto del patron de ClinicalCaseLoader
  (try/catch alrededor de JsonUtility.FromJson, RawCase/RawClave [Serializable] privados
  anidados que declaran UNICAMENTE "clave" asi JsonUtility ignora id/paciente/hechos). Validacion
  con Array.IndexOf contra TriajesValidos = {"I","II","III","IV","V"}; null/vacio -> IndexOf(null)
  devuelve -1 sin lanzar. Unico archivo de este PR que referencia UnityEngine (JsonUtility), como
  exige la frontera de UnityEngine del batch. -->

## Phase 2: `TriageObjectiveSettings` (PR1)

- [x] 2.1 GREEN: `Runtime/Scenarios/Emergency/Config/TriageObjectiveSettings.cs` — dos fracciones complementarias (`PesoDeReceptividad=0.3f`, `PesoDeBanderasRojas=0.7f`, AD4), `RachaParaReceptividadPlena=5` (AD7), `Clamp01` en el constructor. Sin prueba dedicada (no está en el File Inventory de `design.md`): su corrección se verifica indirectamente en 3.5 vía los techos exactos 0.3/0.7 de `Progress01`.
  <!-- apply Fases 0-3 (2026-09-17): creado en Runtime/Scenarios/Emergency/Config/ (carpeta) pero
  namespace NpcAi.Scenarios.Emergency (no .Config) -- mismo patron que design.md fija
  explicitamente ("el Config/ es carpeta, no sub-namespace") y que M7 PR2 ya siguio para
  VrInputSettings/VrInputSettingsAsset. Constantes PesoDeReceptividadPorDefecto=0.3f,
  PesoDeBanderasRojasPorDefecto=0.7f, RachaParaReceptividadPlenaPorDefecto=5; propiedades
  complementarias PesoClinico/PesoDeTriaje calculadas como 1f - peso correspondiente; Clamp01 en
  ambos pesos y Math.Max(1, racha) en el constructor. Verificacion indirecta confirmada en 3.2
  (TriageProgresoTests: techos exactos 0.3f/0.7f/1.0f). -->

## Phase 3: TDD — `TriageScenarioObjective` (PR2)

- [x] 3.1 RED: `TriageScenarioObjectiveTests.cs : ScenarioObjectiveContract` — `CreateSubject()` = constructor sin parámetros; las 7 pruebas heredadas deben pasar vía renormalización (`HasKey==false`). No modifica la clase base.
  <!-- apply Fases 0-3 (2026-09-17): creado, espejo literal de ScriptedScenarioObjectiveTests
  (misma carpeta): 3 lineas, CreateSubject() => new TriageScenarioObjective(). ScenarioObjectiveContract.cs
  no se toco (confirmado, tarea 0.3). -->
- [x] 3.2 RED: `TriageProgresoTests.cs` — techos exactos con caso asignado: 0.3 (solo receptividad), 0.7 (solo clínica), 1.0 (ambas, `IsComplete`); crédito proporcional (2/4 banderas); racha crece y satura en N=5; `Worsened` la reinicia a 0; `Notify(default)`/`From==To` no-op; reversión por `Worsened` y por triaje incorrecto tras acertar.
  <!-- apply Fases 0-3 (2026-09-17): creado con 10 pruebas (una mas de las nombradas: se separo
  "Notify(default)" de "From==To" en 2 pruebas independientes, y se agrego una prueba dedicada de
  crecimiento monotono de la racha hasta su techo 0.3 con caso asignado, que la nota de tasks.md
  agrupa junto a "racha crece y satura"). JSON de 4 banderas rojas embebido, para que el credito
  proporcional de 2/4 sea exactamente 0.245 (0.7*0.5*0.7 = 0.245, calculado y documentado en el
  comentario de la prueba). No compila hasta 3.4 (referencia TriageScenarioObjective /
  TriageObjectiveSettings, que no existian aun) -- misma restriccion de compilacion por
  ensamblado que 1.1 y que M7 PR1/PR3 documentaron; razonado completo contra el pseudocodigo
  exacto de Progress01 en design.md antes de escribir produccion. -->
- [x] 3.3 RED: `TriageSuperficieAditivaTests.cs` — `RegisterRedFlag` idempotente por índice; índice 99/negativo no lanza; sin efecto antes de `AssignCase`; `DeclareTriage` sobrescribible y normalizada (`"ii"`, `" II "`); `cargarJson` nulo/que lanza/caso sin `clave` → `HasKey` falso sin excepción; `AssignCase(ClinicalCaseId.None)`; reasignar caso descarta banderas/triaje previos; `Reset()` vuelve al estado recién construido (AD8) y es idempotente.
  <!-- apply Fases 0-3 (2026-09-17): creado con 17 pruebas -- todas las nombradas en la tarea mas:
  2 pruebas dedicadas de RedFlagCount (con y sin clave, para que el rango observable de
  RegisterRedFlag sea verificable desde otro ensamblado, tal como justifica 3.4), 2 pruebas de
  "sin efecto antes de AssignCase" (RegisterRedFlag y DeclareTriage por separado), y
  DeclareTriage_correcto_contribuye_el_peso_completo_de_esa_mitad (nombrada literal en la
  traza de spec.md, cubierta aqui en vez de en TriageProgresoTests porque es superficie aditiva
  pura sin Notify de por medio). Mismo problema de compilacion por ensamblado que 3.2: no
  compila hasta 3.4. -->
- [x] 3.4 GREEN: `Runtime/Scenarios/Emergency/TriageScenarioObjective.cs` — 2 constructores, `Notify`/`AssignCase`/`RegisterRedFlag`/`DeclareTriage`/`Reset`, `Progress01`/`IsComplete` exactos (pseudocódigo de `design.md`), `HasKey`/`RedFlagsFound`/`RedFlagCount`/`ExpectedClosure`. Incluir `RedFlagCount` aunque la spec no lo nombra: sin él el rango válido de `RegisterRedFlag` es inobservable desde otro ensamblado (decisión de este tasks.md, costo cero); promoverlo a la spec al archivar si el usuario lo confirma.
  <!-- apply Fases 0-3 (2026-09-17): creado, pseudocodigo de design.md copiado literal
  (acumulacion en double, conversion a float solo en el resultado final de Progress01; Clamp01
  con System.Math nunca Mathf; AssignCase limpia SIEMPRE antes de intentar cargar, incluso si
  caseId.IsNone o _cargar es null; try/catch alrededor de _cargar(caseId) para que una excepcion
  del compositor nunca se propague). Constructor sin parametros delega en el rico con
  `new TriageObjectiveSettings()` y cargarJson=null (AD2). Sin UnityEngine, sin Debug.Log. -->
- [x] 3.5 Confirmar en verde 3.1-3.3, incluidos los techos exactos `0f`/`0.3f`/`0.7f`/`1.0f` de la tabla "Aritmética" de `design.md` (verificación indirecta de 2.1).
  <!-- apply Fases 0-3 (2026-09-17): verificado por inspeccion aritmetica manual contra el
  pseudocodigo de design.md (no ejecucion real -- eso es la compuerta humana 4.1): con r=f=t=0,
  Progress01=0f exacto; con caso asignado y r=1 (racha saturada), f=t=0, Progress01=0.3f exacto;
  con r=0, f=t=1, Progress01=0.7f exacto; con r=f=t=1, Progress01=1.0f exacto e IsComplete
  verdadero. Los 4 techos coinciden byte a byte con la tabla "Aritmetica" de design.md porque
  0.3f+0.7f==1.0f es exacto en IEEE-754 binario32 (AD4). Confirmacion real en Test Runner de
  Unity queda pendiente como compuerta humana (tarea 4.1). -->

## Phase 4: Compuerta humana

- [ ] 4.1 MANUAL (Unity Editor — la ejecuta y la registra el usuario, no el agente): Test Runner > EditMode > Run All con las Fases 1-3 completas; registrar el total en `apply-progress.md`. **No aplica compuerta física/hardware**: M9 es lógica de servidor, cero VR/XR/audio/`MonoBehaviour`.

## Phase 5: Documentación (PR3)

- [ ] 5.1 `Docs/MODULES.md` (sección M9) — cerrar "solo doble"; documentar el consumo real de `clave` que `Data/Cases/README.md`, la sección M14 y `ClinicalCase.cs` ya daban por hecho; nombrar lo diferido: evaluación de `cierreEsperado`, integración rica con M15 (índice/`campo` de *match*), cableado de M11.
- [ ] 5.2 Confirmar frontera de diff completa (Fases 0-5): nada fuera de `Runtime/Scenarios/Emergency/`, `Tests/EditMode/Scenarios/Emergency/`, `Docs/` y `openspec/`.

## Phase 6: Cierre (acciones del autor)

- [ ] 6.1 `git add` solo de las carpetas de este cambio; `git diff --cached` antes de cualquier commit.
- [ ] 6.2 Checklist "Antes de mergear" del `README.md`: pruebas propias en verde, diff acotado, `ScriptedScenarioObjective` sigue pasando el contrato sin cambios, rama al día con `main`, decisiones registradas en Engram y en el documento de contexto (regla 10).
- [ ] 6.3 `openspec/specs/escenario-emergencia-m9/spec.md` — copia directa del spec de este cambio (capacidad nueva, sin spec previa que reconciliar, mismo patrón que M7 PR4); mover este directorio a `openspec/changes/archive/` y escribir `archive-report.md`.
- [ ] 6.4 PR(s) final(es) mergeado(s) a `main` por el autor (regla 8, self-merge).

## Notas

- `RachaParaReceptividadPlena=5` y los pesos 0.3/0.7 · 0.7/0.3 son juicio pedagógico, sin validar
  con el asesor todavía; viven en `TriageObjectiveSettings` inyectable, cambiarlos no toca lógica
  de puntaje (Open Questions de `design.md`).
- `RedFlagCount` es un añadido de este `tasks.md` más allá de la spec original (ver 3.4).
- `design.md` (Open Questions) todavía describe como pendiente la discrepancia "techo 0.4" de
  `proposal.md` líneas 166/206 — esas dos frases ya fueron corregidas a "techo 0.3" antes de este
  cambio; la nota de `design.md` quedó obsoleta y puede limpiarse en un cambio posterior o al
  archivar, no bloquea esta entrega.
- Única compuerta humana: EditMode (4.1). A diferencia de M1/M2/M7, M9 no tiene compuerta física.
