# Propuesta: M10 — Escenario Sala de Juntas (`Runtime/Scenarios/Boardroom/`)

## Intent

`Runtime/Scenarios/Boardroom/` hoy contiene **una sola clase**: `Fakes/ScriptedScenarioObjective.cs`,
copia byte a byte del doble de M9 (contador ±1 sobre 4 pasos fijos). No mide nada propio de una
sala de juntas: no sabe qué caso se está levantando, no sabe qué requerimientos obtuvo el
estudiante y no tiene forma de cerrar la sesión. Es el único módulo de escenario sin
implementación real y el único puerto de M0 con dos dueños donde uno ya entregó (M9,
`TriageScenarioObjective`) y el otro no.

El bloqueo real ya se levantó: M16 (`Runtime/RequirementResponse/`, PRs #45-#53) está en `main`
con el catálogo `Data/Requirements/caso-juntas-0{1..4}.json`, el respondedor real y el
`RequirementId` explícito en `Core.RequirementResponse`. Ese identificador es exactamente lo que
faltaba para que M10 mida **qué requerimiento específico** se reveló, sin caer en el hueco
`campoDe(clin)` que sigue abierto entre M9 y M15.

Tercer y último eslabón de la cadena **M0 (mergeado) → M16 (mergeado) → M10 (este cambio)**.

## Scope

### In Scope

- `RequirementsScenarioObjective.cs`: implementación real de `IScenarioObjective` para sala de
  juntas. `Progress01` mezcla **tres vías** (cobertura del catálogo, cierre explícito y fiel,
  trato sostenido), igual que `TriageScenarioObjective` mezcla dos.
- **Superficie aditiva sobre la clase concreta** (patrón M9/M2/M7, sin tocar el puerto):
  `AssignCase(RequirementCaseId)`, `RegisterDisclosure(Core.RequirementResponse)`,
  `PresentSummary(IReadOnlyCollection<RequirementId>)`, `Reset()`, más lecturas
  (`HasCase`, `RequirementCount`, `RequirementsDisclosed`, `SummaryIsFaithful`).
- `RequirementChecklist.cs` + `RequirementChecklistLoader.cs`: **proyección mínima** del catálogo
  de M16 — solo `id` y `requerimientos[].id`, nada de `respuesta` ni `receptividadMinima`.
  Espejo exacto de `TriageKey`/`TriageKeyLoader`, que M9 escribió para leer el bloque `clave` de
  `Data/Cases/*.json` sin referenciar `NpcAi.ClinicalResponse`.
- `Config/BoardroomObjectiveSettings.cs`: POCO puro con los pesos y umbrales (espejo de
  `TriageObjectiveSettings`), más `Unity/BoardroomObjectiveSettingsAsset.cs` (`ScriptableObject`
  que proyecta al POCO, sin tomar ninguna decisión — espejo de `ReceptivityProfileAsset`).
- `Data/Scenarios/`: `README.md` + `Boardroom.asset` con los pesos reales. Los valores de
  *tuning* viven en el `.asset` (regla 7); las constantes C# quedan solo como red de seguridad
  para cuando el asset falta, igual que `PersonalityStyleBank.Fallback` en M16 (AD10).
- **Rediseño del doble** `Fakes/ScriptedScenarioObjective.cs`: deja de ser el calco de M9 y pasa
  a espejar la superficie aditiva real con una lista de requerimientos embebida y los 4 ids de
  caso conocidos, sin leer `Data/` — espejo de `ScriptedRequirementResponder` (regla 4).
- Pruebas EditMode en `Tests/EditMode/Scenarios/Boardroom/`:
  - `RequirementsScenarioObjectiveTests : ScenarioObjectiveContract` (real).
  - `ScriptedScenarioObjectiveTests : ScenarioObjectiveContract` (doble, ya existe).
  - `RequirementsProgresoTests` (las tres vías por separado y combinadas).
  - `RequirementsSuperficieAditivaTests` (`AssignCase` / `RegisterDisclosure` / `PresentSummary`).
  - `ResetParityTests` (`Reset()` real y doble: mismo estado, idempotente).
  - `RequirementChecklistLoaderTests` + validación de los 4 `caso-juntas-0N.json` reales.
  - `BoardroomObjectiveSettingsAssetTests` (proyección asset → POCO, y `Fallback` sin asset).
- `Docs/MODULES.md`: reescritura de la sección M10 (hoy dice "solo doble").

### Out of Scope

- **`NpcAi.Core`, `NpcAi.Core.Channels` y `Tests/EditMode/Core/` no cambian.** `IScenarioObjective`
  queda byte-idéntico y `ScenarioObjectiveContract` se **hereda sin modificar** (regla 2). Por eso
  `Reset()` se prueba en el módulo y no ampliando la base compartida con M9.
- **M16 no se toca.** `Data/Requirements/` es solo lectura para este cambio; M10 lee el archivo,
  no referencia el ensamblado `NpcAi.RequirementResponse` (regla 3).
- **M9 no se toca y no se reusa código suyo.** Son módulos hermanos; el patrón se imita, las
  clases no se comparten (regla 1).
- **El compositor de sala de juntas** (quién elige caso y personalidad, quién llama a `Respond`
  y reenvía el resultado a M10, quién dispara el cierre): es el cambio de M11, no este.
- **Los perfiles de receptividad propios de Boardroom** (cliente que arranca en `Receptivo`):
  cambio aparte — ver la recomendación en *Approach*.
- **De la voz del estudiante a una lista de `RequirementId`.** `PresentSummary` recibe ids, no
  texto libre. Convertir un resumen hablado en ids es trabajo del compositor.
- **Evaluación semántica del resumen.** Se compara el conjunto de ids, nunca la redacción.
- `Runtime/Speech/`, `Runtime/Nlu/`, `Runtime/Dialogue/`, `Runtime/Presentation/`,
  `Runtime/SessionLog/`, `Data/Personalities/`, `Data/Corpus/`: sin cambio.

## Capabilities

### New Capabilities

- `escenario-sala-juntas-m10` (nueva): formaliza que M10 satisface `contrato-nucleo-m0`
  heredando `ScenarioObjectiveContract` sin modificarla, más sus requisitos propios — las tres
  vías de progreso y su renormalización sin caso; `RegisterDisclosure` acredita solo
  `Outcome == Revelado` de un `RequirementId` del caso asignado y es idempotente; el cierre
  acredita solo si el resumen coincide exactamente con lo revelado; `Reset()` como API real y
  probada; ningún peso ni umbral vive como constante C# de tuning.

### Modified Capabilities

- Ninguna. `escenario-emergencia-m9`, `respondedor-requerimientos-m16` y
  `catalogo-requerimientos-m16` quedan intactas.

## Approach

**El seam ya existe.** `RequirementsScenarioObjective` es un adaptador nuevo detrás de un puerto
mergeado hace semanas. Ningún otro módulo cambia una línea.

**Tres vías de progreso, todas con peso en dato.**

| Vía | Qué mide | Cómo entra | Crédito |
|---|---|---|---|
| Cobertura | requerimientos revelados / total del caso | `RegisterDisclosure(Core.RequirementResponse)` | fracción |
| Cierre | el estudiante presenta el resumen y es fiel | `PresentSummary(ids)` | 1 o 0 |
| Trato | receptividad sostenida a lo largo de la sesión | `Notify(ReceptivityChange)` (el puerto) | fracción |

**Renormalización sin caso (AD calcada de M9).** Con `HasCase == false` — que es exactamente lo
que construye `ScenarioObjectiveContract.CreateSubject()` — el trato pasa a ser el 100 % del
progreso. Así la base de contrato de M0 se satisface con el único lever que tiene (`Notify`) y
ningún `[Test]` queda omitido por `Assume`.

**M10 es quien recuerda; M16 no puede.** `RequirementResponderContract` exige determinismo para
la misma tupla, así que `Respond` es sin estado entre turnos por construcción. El acumulado de
"qué se reveló ya" es responsabilidad de M10: un `HashSet<RequirementId>` sembrado por
`AssignCase` con el checklist del caso. Que el compositor llame a `Respond` hasta tres veces por
turno es inocuo: `RegisterDisclosure` es **idempotente por id** (mismo criterio que
`RegisterRedFlag(int)` en M9) y descarta lo que no pertenece al caso asignado.

**`RegisterDisclosure` recibe el DTO completo, no un `RequirementId` pelado.** `Core.RequirementResponse`
vive en `NpcAi.Core`, así que M10 lo acepta sin referenciar M16. El compositor reenvía los tres
resultados del turno a ciegas y **la regla de negocio "solo `Revelado` cuenta" queda adentro de
M10**, donde pertenece, en vez de duplicarse en cada compositor.

**El cierre acredita fidelidad, no cantidad.** El componente de cierre vale 1 si y solo si el
conjunto presentado es **exactamente** el conjunto revelado en la sesión: ni uno de menos
(olvidar lo que el cliente sí dijo) ni uno de más (inventar un requerimiento que el cliente nunca
dio). Ambos son modos de falla reales del levantamiento de requerimientos y ninguno necesita dato
nuevo. Cerrar antes de cubrir el catálogo **está permitido y no lanza**: es un cierre temprano
honesto que gana su componente mientras la cobertura queda por debajo de 1, así que
`Progress01 < 1`. Sobrescribible (gana la última llamada) y sin efecto antes de `AssignCase`,
igual que `DeclareTriage`.

**Asignación de caso: mismo patrón que M9, sin azar adentro.** `AssignCase(RequirementCaseId)`
recibe el id; el compositor elige (fijo, rotado o con RNG sembrado, como ya hace
`SessionDirector.SiguienteCaso()`) y pasa **el mismo id** a M16 y a M10, tal como
`SessionDirector.Arrancar` ya hace con `ClinicalCaseId` para M9 y M15. El JSON entra por un
`Func<RequirementCaseId, string>` inyectado, que **puede ser `null`** (se trata como "ningún caso
carga", nunca propaga): mismo contrato defensivo de `TriageScenarioObjective` y de
`RequirementResponder` (AD9). Un id desconocido deja `HasCase == false`.

**Perfiles de receptividad de Boardroom: cambio aparte (recomendación).** Que el cliente arranque
en `Receptivo` sin importar la personalidad **no es código de M10**: sale de
`ReceptivityProfileAsset.puntajeInicial` (dato de M5, `Data/Personalities/`), lo consume
`ReceptivityEngine.Reset(personality)` (M4) y lo compone el anfitrión con `BuildCatalog(assets)`.
Meterlo acá violaría la regla 1 (dos módulos en un cambio) e inflaría un diff que ya está sobre
presupuesto. M10 nunca lee ni fija la receptividad: solo recibe `ReceptivityChange`, así que es
correcto sin importar dónde arranque. Recomendación: un cambio de datos pequeño y propio
(`Data/Personalities/Boardroom/{grosero,histerico,introvertido,empatico}.asset` + extensión de
`PersonalityProfilesDataTests`), mismo dueño, en paralelo o después. **Implicación que hay que
confirmar primero** (ver *Preguntas abiertas* 1): con el cliente en `Receptivo` desde el turno 1,
la puerta `receptividadMinima` de M16 queda abierta de entrada y solo muerde hacia abajo.

**Nombre.** Se elige `RequirementsScenarioObjective`, que nombra la **actividad medida** igual
que `TriageScenarioObjective` nombra el triaje y no la sala. Se descartan
`BoardroomScenarioObjective` (nombra el cuarto, no lo que mide, y repite el namespace) y
`ElicitationScenarioObjective` (preciso pero opaco para el equipo). Superficie pública en inglés
como la de M9; helpers privados en español como en M16 (`RequirementMatcher.Normalizar`).

## Affected Areas

| Área | Impacto | Descripción |
|---|---|---|
| `Runtime/Scenarios/Boardroom/RequirementsScenarioObjective.cs` | Nuevo | Implementación real + superficie aditiva |
| `Runtime/Scenarios/Boardroom/RequirementChecklist.cs`, `RequirementChecklistLoader.cs` | Nuevo | Proyección mínima del catálogo de M16 |
| `Runtime/Scenarios/Boardroom/Config/BoardroomObjectiveSettings.cs` | Nuevo | POCO de pesos (C# puro) |
| `Runtime/Scenarios/Boardroom/Unity/BoardroomObjectiveSettingsAsset.cs` | Nuevo | `ScriptableObject` → POCO, sin lógica |
| `Runtime/Scenarios/Boardroom/Fakes/ScriptedScenarioObjective.cs` | Modificado | Rediseño: deja de ser calco de M9 |
| `Data/Scenarios/` | Nuevo | `README.md` + `Boardroom.asset` |
| `Tests/EditMode/Scenarios/Boardroom/` | Nuevo/Mod. | 7 archivos de prueba |
| `Docs/MODULES.md` | Modificado | Sección M10 |
| `NpcAi.Scenarios.Boardroom.asmdef` | Sin cambio | Ya referencia solo `NpcAi.Core` + `NpcAi.Core.Channels` |
| `Runtime/Core/`, `Runtime/CoreChannels/`, `Tests/EditMode/Core/` | Sin cambio | Contrato intacto; la base se hereda |
| `Runtime/RequirementResponse/`, `Data/Requirements/` | Sin cambio | M16 se lee, no se toca |
| `Runtime/Scenarios/Emergency/`, `Data/Personalities/` | Sin cambio | Otro dueño / otro cambio |

## Risks

| Riesgo | Prob. | Mitigación |
|---|---|---|
| **Presupuesto de revisión.** M9 solo (objetivo + settings + lector de clave + pruebas) ya pasó de 400 líneas; M10 agrega tres vías, el asset, el rediseño del doble y 7 archivos de prueba | Alta | `sdd-tasks` emite el pronóstico con el conteo real. Corte sugerido en **4 PRs encadenados**: (1) settings POCO + asset + `Data/Scenarios/` + sus pruebas; (2) checklist + loader + validación de los 4 JSON reales; (3) `RequirementsScenarioObjective` + `: ScenarioObjectiveContract` + pruebas de progreso y superficie aditiva; (4) rediseño del doble + paridad de `Reset()` + `Docs/MODULES.md`. Estrategia de entrega: `ask-on-risk` |
| **La vía de trato es ineficaz si el cliente arranca en `Receptivo`.** Una racha de mejoras al estilo M9 es inalcanzable en una sesión perfecta: sin caídas no hay `Improved`, y `Notify` solo llega en transiciones — el progreso quedaría topado por debajo de 1 jugando bien | Alta | La vía de trato se lee como **crédito preservado**: `AssignCase` lo siembra lleno (el cliente llega dispuesto), cada `Worsened` lo baja y cada `Improved` lo recupera. La vía sin caso conserva la racha estilo M9 para que `ScenarioObjectiveContract` siga pasando limpio. La aritmética exacta y su prueba se fijan en `design.md`; el precedente de "`AssignCase` puede subir el progreso" ya existe (AD11 de M9) |
| **Acoplamiento por archivo con el esquema de M16** (M10 lee `caso-juntas-0N.json` sin referenciar el ensamblado) | Media | Precedente exacto M9/M15 con `Data/Cases/`. Se proyectan solo `id` y `requerimientos[].id`, la parte más estable del esquema; `RequirementCasesDataTests` (M16) ya valida el archivo completo y una prueba de M10 valida los 4 reales contra la proyección |
| **Denominador variable**: los casos traen entre 4 y 6 requerimientos (`caso-juntas-03` tiene 4) | Media | La cobertura es fracción sobre el total del caso, nunca contador absoluto contra una constante. Checklist vacío o inválido ⇒ `HasCase == false` y renormalización |
| **`Reset()` no está en el puerto** y `ScenarioObjectiveContract` es de M0 (solo lectura) | Baja | Paridad real/doble probada en `Tests/EditMode/Scenarios/Boardroom/ResetParityTests`, sin ampliar la base compartida con M9 (eso sería cambio de contrato) |
| **Del resumen hablado a una lista de `RequirementId`** no hay pieza que lo resuelva hoy | Media | Fuera de alcance por diseño. En el primer corte el arnés de M11 (escritorio, sin VR) presenta el checklist y el estudiante selecciona; el reconocimiento por voz es refinamiento posterior |
| Reversibilidad e `IsComplete` no pegajosa con tres componentes y clamp | Media | Heredadas de la base de contrato; clamp explícito en `Progress01` y pesos complementarios que topan la mezcla en exactamente 1 (AD4 de M9) |
| Unity trae NUnit 3.5: `Is.AnyOf` no compila y tumba la assembly de pruebas | Baja | Gotcha conocida del repo (M5); comparaciones planas |
| El cliente en `Receptivo` desde el turno 1 vacía la revelación progresiva de M16 | Media | Confirmado por Jefferson (pregunta 1): lectura deliberada, la puerta de M16 pasa a penalización. Perfiles de Boardroom quedan pospuestos hasta M11 (pregunta 4) |

## Rollback Plan

M10 es implementación nueva detrás de un puerto que ya existe en `main`, casi enteramente
aditiva. Revertir los commits borra `RequirementsScenarioObjective.cs`, `RequirementChecklist*.cs`,
`Config/`, `Unity/`, `Data/Scenarios/` y `Tests/EditMode/Scenarios/Boardroom/` (salvo
`ScriptedScenarioObjectiveTests.cs`, que ya existía), **restaura el doble anterior** — único
archivo modificado, no creado — y devuelve la sección M10 de `Docs/MODULES.md` a "solo doble".
`IScenarioObjective` queda con una sola implementación real (M9), exactamente como hoy. Ningún
módulo depende de `RequirementsScenarioObjective` por nombre hasta que exista el compositor de
sala de juntas. Sin migración de datos: `Data/Requirements/` no se toca.

## Dependencies

- **M16 mergeado a `main`** (PRs #45-#53): `Data/Requirements/caso-juntas-0{1..4}.json` con
  `requerimientos[].id` estables, y `Core.RequirementResponse` poblando `RequirementId`. M10 no
  tiene denominador sin esto.
- **`2026-09-16-m0-puerto-requerimientos-juntas` mergeado** (PRs #39-#42, contrato v3):
  `RequirementCaseId`, `RequirementId`, `RequirementOutcome`, `RequirementResponse`. Ese cambio
  **todavía no está archivado**, así que la spec de M10 debe referenciarlo desde la carpeta del
  cambio o archivarse M0 primero — mismo cabo suelto que ya señaló la propuesta de M16.
- `contrato-nucleo-m0` (`IScenarioObjective` + `ScenarioObjectiveContract`) y
  `escenario-emergencia-m9` (archivada) como referencia de redacción, no de código.
- M4/M5 para `ReceptivityChange`. El compositor de sala de juntas (M11) es **consumidor**, no
  prerrequisito. Sin dependencias de red, GPU ni servicios. Sin co-revisión obligatoria de M0:
  este cambio no toca el contrato.

## Success Criteria

- [ ] `RequirementsScenarioObjectiveTests : ScenarioObjectiveContract` y
      `ScriptedScenarioObjectiveTests : ScenarioObjectiveContract` pasan los **7 `[Test]`
      heredados** sin modificar la base y **sin ninguno omitido por `Assume`**.
- [ ] Con caso asignado y las tres vías satisfechas, `Progress01 == 1` (tolerancia `1e-4`) e
      `IsComplete`; con cualquiera incompleta, `Progress01 < 1`.
- [ ] Sin caso asignado, la vía de trato es el 100 % del progreso (renormalización).
- [ ] `RegisterDisclosure` es idempotente por `RequirementId`; un `Outcome != Revelado` o un
      `RequirementId` ajeno al caso no mueven el progreso; nunca lanza, ni antes de `AssignCase`.
- [ ] `PresentSummary` acredita el cierre con el conjunto exacto de lo revelado — **aun si es
      parcial** — y no lo acredita si falta o sobra un id. Nunca lanza con `null` ni vacío.
- [ ] `Reset()` deja real y doble en el estado recién construido y es idempotente, probado en
      ambos.
- [ ] Cambiar un peso, el archivo del caso o agregar un caso **no requiere tocar ninguna clase
      C#**.
- [ ] `NpcAi.Scenarios.Boardroom.asmdef` sigue sin referenciar `NpcAi.RequirementResponse` ni
      `NpcAi.Scenarios.Emergency`.
- [ ] Sin `Debug.Log` en runtime.
- [ ] `Docs/MODULES.md` describe M10 como implementación real.
- [ ] El diff no toca `Runtime/Core/`, `Runtime/CoreChannels/`, `Tests/EditMode/Core/`,
      `Runtime/RequirementResponse/`, `Runtime/Scenarios/Emergency/`, `Data/Requirements/` ni
      `Data/Personalities/`.

## Decisiones del usuario (Jefferson, ya confirmadas)

1. Cadena **M0 → M16 → M10**; los dos primeros ya están en `main`, este es el cierre.
2. **Pesos y umbrales como dato externo** (`ScriptableObject` en `Data/`), nunca constantes C# de
   tuning — regla 7.
3. **Progreso = catálogo por caso**, no contador plano vía `Intent.AportaInformacion`. La
   recomendación barata del agente se descartó a propósito: el trabajo de grado debe mostrar
   identificación específica de requerimientos por caso.
4. **M16 provee el catálogo, el matcher y el respondedor.** M10 los consume por archivo y por
   DTO, no los reimplementa.
5. **`Reset()` pasa a ser API real y probada**, no el código muerto que es hoy en ambos dobles.
6. **Cierre explícito del escenario**, análogo a `DeclareTriage` en M9: el objetivo no se completa
   pasivamente al llegar `Progress01 == 1`.

## Preguntas abiertas (ronda de propuesta) — RESUELTAS por Jefferson (2026-09-21)

1. **Regla de negocio — el cliente que arranca `Receptivo` abre la puerta de M16 de entrada.**
   **Confirmado: es correcto y deliberado.** El cliente llega dispuesto y la habilidad que se
   evalúa es *saber qué preguntar*, no apaciguar a nadie; la puerta de M16 pasa a ser
   penalización, no progresión. El NPC arranca en `Receptivo` sin importar la personalidad.
2. **Fidelidad del cierre: ¿exacta o por cobertura?** **Confirmado: coincidencia exacta.**
   `PresentSummary` acredita el cierre solo si el conjunto presentado es exactamente el
   revelado — ni falta ni sobra. Hace visible tanto olvidar un dato real como inventar uno que
   el cliente nunca dio.
3. **¿La vía de trato debe pesar?** **Confirmado: peso bajo, ≈0.2.** La receptividad no puede
   premiar mucho más allá del punto de partida (el cliente ya arranca en `Receptivo`), pero
   sigue castigando el maltrato. `design.md` fija el valor exacto y la fórmula de mezcla con
   este peso como insumo.
4. **Perfiles de receptividad de Boardroom = cambio aparte.** **Confirmado: se pospone** hasta
   que exista el compositor de sala de juntas (M11) — sin consumidor todavía, no se abre como
   cambio de datos ahora.
5. **Confirmación de nombres.** `RequirementsScenarioObjective`, `RegisterDisclosure`,
   `PresentSummary`, `Data/Scenarios/Boardroom.asset` — sin objeción, quedan confirmados.
