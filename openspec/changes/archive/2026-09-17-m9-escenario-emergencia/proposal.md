# Propuesta: M9 — Escenario Sala de Emergencia (implementación real de `IScenarioObjective`)

## Intent

`Docs/MODULES.md` (líneas 317-325) documenta hoy a M9 como **"solo doble"**:
`Runtime/Scenarios/Emergency/` contiene únicamente `Fakes/ScriptedScenarioObjective.cs` —
`_pasos++` en `Improved`, `_pasos--` en `Worsened`, saturado en `[0,4]`. Esa lógica es
**byte-idéntica a la del doble de M10 (Sala de Juntas)**, y el propio `Docs/MODULES.md` (línea
339) lo dice: "la misma lógica de paso fijo ±1 que la de M9". Un objetivo que mide exactamente lo
mismo en una sala de triaje que en una reunión de levantamiento de requerimientos **no mide el
escenario**: mide cortesía genérica. `Specs formales: no existe`.

La desviación concreta que este cambio cierra es que el bloque `clave` de `Data/Cases/*.json`
existe, está poblado en los tres casos y **nadie lo lee**, aunque tres documentos distintos lo
declaran propiedad exclusiva de M9:

- `Data/Cases/README.md` línea 103: "`clave` es exclusiva de M9 (`IScenarioObjective`), que la usa
  para medir el progreso de la enfermera y evaluar el cierre".
- `Docs/MODULES.md` sección M14: "dato de evaluación exclusivo de M9, que M15 tiene
  contractualmente prohibido leer".
- `Runtime/ClinicalResponse/ClinicalCase.cs` líneas 9-12: `clave` se omite del POCO de M15 "porque
  ese dato es de evaluación exclusiva de M9".

Este cambio entrega la primera implementación real de `IScenarioObjective` para el escenario de
emergencia: lee `clave`, y mide `Progress01`/`IsComplete` como una **mezcla de receptividad
sostenida y corrección clínica** (banderas rojas descubiertas + triaje declarado correcto), no una
de las dos sola. **No cierra M9 por completo y no debe leerse así**: cierra el estado "solo doble"
con una señal clínica que el compositor ya puede entregar hoy, y deja la evaluación del
`cierreEsperado` y la integración fina con M15 explícitamente fuera (ver Scope Out).

Decisiones ya fijadas antes de este cambio (confirmadas con el usuario el 2026-09-17): ver
"Decisiones del usuario" al final.

## Scope

### In Scope

- `Runtime/Scenarios/Emergency/TriageScenarioObjective.cs` — implementación real de
  `IScenarioObjective`. Mismo patrón de nombre "técnica + puerto" que `BertIntentClassifier`,
  `MarkovDialogueGenerator`, `SpatialPhysicalActionSource` y `OfflineSpeechToText`: el nombre dice
  **cómo** mide (por la clave de triaje), no en qué carpeta vive.
- `Runtime/Scenarios/Emergency/TriageKey.cs` + `TriageKeyLoader.cs` — POCO y lector mínimo del
  bloque `clave` (`TriajeEsperado`, `TiempoAtencion`, `BanderasRojas`, `CierreEsperado`), con
  `TryParse(string json, out TriageKey)`. Espejo exacto de `ClinicalCase` + `ClinicalCaseLoader`,
  incluidos los nombres de propiedad en español. **M9 escribe su propio lector**: los tipos
  `RawCase`/`RawHecho` de M15 son `private sealed class` anidados y son inaccesibles incluso si M9
  referenciara `NpcAi.ClinicalResponse` (que no lo hace ni lo hará, regla dura 3).
- **Constructor con `Func<ClinicalCaseId,string> cargarJson` inyectado**, nunca
  `File.ReadAllText`. Copia literal del patrón de `ClinicalResponder` (línea 25 y su comentario
  AD3): de dónde salen los bytes en cada plataforma es trabajo del compositor, no de M9. Un
  `cargarJson` nulo o que lanza se trata como "ningún caso carga" y no propaga excepción.
- **Superficie aditiva sobre la clase concreta** (no sobre el puerto), llamada por quien componga
  M9 en escena (M11, cuando exista) o por las pruebas:
  `AssignCase(ClinicalCaseId)`, `RegisterRedFlag(int index)`, `DeclareTriage(string category)`,
  `Reset()`, y las lecturas `HasKey`, `RedFlagsFound`, `ExpectedClosure`.
- **Mitad de receptividad**: receptividad *sostenida*, no paso ±1. La racha crece mientras el
  cambio mejora y **se reinicia a 0** en cualquier `Worsened` — regla propia del escenario: perder
  la cooperación del paciente en triaje obliga a reconstruir la relación desde cero, no a
  descontar un punto. Un `ReceptivityChange` que no representa cambio (incluido `default`) no
  mueve nada.
- **Mitad clínica**: fracción de `clave.banderasRojas` registradas (conjunto idempotente, índices
  fuera de rango rechazados) más un componente booleano de triaje declarado correcto contra
  `clave.triajeEsperado` (normalizado, `"I"`..`"V"`).
- `Runtime/Scenarios/Emergency/Config/TriageObjectiveSettings.cs` — POCO de pesos y umbrales
  (peso de cada mitad, longitud de la racha receptiva, reparto interno banderas/triaje) con
  valores por defecto documentados, inyectable por constructor. **No se crea ningún asset ni
  carpeta `Data/`** (ver Scope Out).
- `Tests/EditMode/Scenarios/Emergency/TriageScenarioObjectiveTests.cs : ScenarioObjectiveContract`
  (hereda la misma base que ya pasa el doble, **sin modificarla**) más pruebas propias de la
  mezcla, del lector de `clave` y de los casos borde.
- `openspec/specs/escenario-emergencia-m9/spec.md` — primera spec formal de M9.
- `Docs/MODULES.md` (sección M9), cerrando "solo doble" y documentando por fin el consumo de
  `clave` que los otros tres documentos ya daban por hecho.

### Out of Scope

- **`IScenarioObjective` y `ReceptivityChange` no cambian.** Contrato M0 congelado. Este cambio es
  puro motor/adaptador, exactamente como M2 fue a `IIntentClassifier` y M7 a
  `IPhysicalActionSource`. **No es cambio de M0** y no requiere la gobernanza de la regla dura 2.
- **`Tests/EditMode/Core/ScenarioObjectiveContract.cs` no se modifica.** La clase base solo
  ejercita `Notify` con `ReceptivityChange` sintéticos y no conoce la superficie aditiva, así que
  un método extra no la puede romper en ninguna dirección.
- **`Fakes/ScriptedScenarioObjective.cs` (el doble) no cambia.** Sigue existiendo y sigue pasando
  `ScenarioObjectiveContract`; M11 lo sigue usando donde prefiera no depender de un caso clínico
  cargado — igual que `NluIntentClassifier` quedó documentado como respaldo en M2 y
  `ScriptedPhysicalActionSource` en M7.
- **M15 (`Runtime/ClinicalResponse/`) no se toca, y este cambio no depende de una modificación de
  M15.** Hoy `IClinicalResponder.Respond` devuelve solo `Handled` + `Reply`: no dice **cuál**
  `Hecho`/`Campo` hizo *match*. Por eso `RegisterRedFlag` recibe el índice de la bandera roja ya
  superficiada y **no** intenta deducirlo: M9 evalúa el hecho reportado contra su `clave` cargada,
  con independencia de *cómo* el compositor lo determinó. Una integración más rica —que M15
  exponga el índice/`campo` que hizo *match*, y que M11 lo correlacione con las banderas rojas—
  sería un **cambio SDD aparte sobre M15** (regla dura 1) y queda como seguimiento **no
  bloqueante**: nada de lo que entrega esta propuesta se reescribe cuando llegue.
- **Emparejamiento por similitud de texto entre `hechos[].campo` y `clave.banderasRojas`.** Las
  banderas rojas son justificaciones clínicas en texto libre ("Deficit neurologico focal agudo:
  dificultad para hablar (disartria/afasia)") sin ninguna llave compartida con `hechos[]`.
  Cualquier emparejamiento automático sería heurística difusa, no una búsqueda: queda fuera.
- **Evaluación automática de `clave.cierreEsperado`.** Se carga y se expone en lectura (para que
  M11/M13 lo muestren a quien evalúa), pero **no mueve `Progress01`**: es un párrafo libre y
  calificarlo exige comparación semántica, que es otra decisión completa.
- **`Data/Scenarios/` ni un `ScriptableObject` de configuración.** El repositorio no tiene hoy
  ninguna convención `Data/Scenarios/` (existen `Data/Cases`, `Corpus`, `Dialogue`,
  `Personalities`, `Presentation`, `Speech`, `VrInput`), y los pesos de M9 son tres números
  estables, no los umbrales calibrables por hardware de M7. Se dejan como valores por defecto del
  POCO inyectable; promoverlos a asset es un cambio posterior trivial cuando M11 necesite ajustar
  por escenario, y no obliga a tocar la lógica.
- **El cableado real en escena.** Componer M9 dentro de una sesión (quién llama `AssignCase`,
  quién decide que una bandera roja se superficia) es trabajo de **M11**, igual que M1, M2, M7 y
  M8 difirieron ahí su composición. M9 solo debe quedar *componible*.
- **M10 (`Runtime/Scenarios/Boardroom/`).** Comparte el puerto y hoy el doble byte-idéntico, pero
  es otro módulo y otro dueño: no se toca ni una línea (regla dura 1).
- **`Data/Cases/*.json` no se modifica.** Es dato de M14, de solo lectura para M9.

## Capabilities

### New Capabilities

- `escenario-emergencia-m9` (nuevo `openspec/specs/escenario-emergencia-m9/spec.md`): primera spec
  formal de M9. Formaliza lo que `ScenarioObjectiveContract.cs` ya fija (arranque en 0, rango
  `[0,1]`, `IsComplete` si y solo si `Progress01 == 1` con tolerancia `1e-4`, reversibilidad,
  `Notify(default)` no-op) más las garantías nuevas: carga de `clave`, receptividad sostenida con
  reinicio por empeoramiento, registro idempotente de banderas rojas, declaración de triaje
  reescribible, renormalización de pesos sin caso asignado y casos borde del lector. Mismo patrón
  que `entrada-fisica-vr-m7`.

### Modified Capabilities

- Ninguna. `IScenarioObjective` y `ReceptivityChange` (`contrato-nucleo-m0`) no cambian, y
  `canales-evento-nucleo-m0` no participa.

## Approach

**La costura ya existe y ya está probada**: `IScenarioObjective` es la única superficie que ve el
resto del sistema, y `ScenarioObjectiveContract` ya la fija en 7 pruebas. `TriageScenarioObjective`
es una implementación nueva detrás de esa interfaz; ningún otro módulo cambia una línea.

**Por qué una superficie aditiva y no un cambio de M0**: precedente propio del repositorio.
`BertIntentClassifier` expone `EmitirParaPrueba` más allá de `IIntentClassifier`;
`VrInputBehaviour` expone `CablearParaPrueba`/`DescablearParaPrueba`/`BombearParaPrueba` más allá
de lo que exige cualquier puerto. Se declara sin adornos que aquí el uso es distinto: esos
ejemplos son ayudas de cableado para pruebas, mientras `AssignCase`/`RegisterRedFlag`/
`DeclareTriage` transportan **señal real de tiempo de ejecución**. La forma es la misma (el puerto
intacto, la clase concreta más rica), el uso es más ambicioso, y por eso se documenta en la spec en
lugar de esconderse detrás del precedente.

**La mezcla**, con los pesos del POCO:

| Componente | Peso por defecto | Cómo se alimenta | ¿Reversible? |
|---|---|---|---|
| Receptividad sostenida | 0.3 | `Notify(ReceptivityChange)` (el puerto) | Sí: cualquier `Worsened` la reinicia a 0 |
| Banderas rojas descubiertas | 0.7 × 0.7 | `RegisterRedFlag(index)` | No: lo descubierto queda descubierto |
| Triaje declarado correcto | 0.7 × 0.3 | `DeclareTriage(category)` | Sí: una nueva declaración sobrescribe la anterior |

`IsComplete` sigue siendo exactamente `Progress01 == 1` (tolerancia `1e-4`) y sigue siendo
reversible por dos vías independientes: perder la receptividad, o declarar un triaje equivocado
después de haber acertado.

**Sin caso asignado, los pesos se renormalizan**: si `HasKey` es falso, la receptividad sostenida
es el 100% del progreso y M9 se comporta como el objetivo genérico que hoy es el doble. Esto no es
cosmético: es lo que permite que las 7 pruebas de `ScenarioObjectiveContract` se ejerciten de
verdad, porque `CreateSubject()` no tiene forma de llamar a la superficie aditiva. La alternativa
(pesos fijos) dejaría el progreso con techo en 0.3, `IsComplete` nunca verdadero, y las dos
pruebas que dependen de completar (`Completar_implica_progreso_total` y
`La_completitud_es_reversible`, que ya usa `Assume.That` en la línea 87 como escape) quedarían
permanentemente vacías o inconclusas. El costo de la renormalización es que olvidar `AssignCase`
produce un objetivo completable que no midió nada clínico; se paga exponiendo `HasKey` como
lectura pública y afirmándolo en prueba, en lugar de esconder el error detrás de un techo de 0.3.

**Por qué índices y no texto** en `RegisterRedFlag`: un índice contra `clave.banderasRojas` es una
búsqueda determinista, probable en milisegundos y sin ambigüedad; el texto libre de las banderas
rojas obligaría a similitud semántica. Los índices fuera de rango se rechazan sin lanzar, y
registrar dos veces el mismo índice no suma dos veces.

**Casos borde que la spec debe fijar**: bloque `clave` ausente o JSON malformado (→ `HasKey`
falso, sin excepción); `banderasRojas` vacío (→ ese componente cuenta como satisfecho, para que el
caso siga siendo completable); `triajeEsperado` fuera de `{"I".."V"}` (→ clave inválida);
`RegisterRedFlag`/`DeclareTriage` antes de `AssignCase` (→ sin efecto); `AssignCase` con
`ClinicalCaseId.IsNone`; reasignar caso (→ descubrimientos previos descartados).

**Dependencia de `UnityEngine`**: solo en `TriageKeyLoader`, vía `JsonUtility`, exactamente el
mismo uso seguro en EditMode que `ClinicalCaseLoader` ya tiene. La lógica de puntaje no toca
`UnityEngine`, no hay `MonoBehaviour`, no hay escena, no hay VR ni audio.

## Affected Areas

| Área | Impacto | Descripción |
|---|---|---|
| `Runtime/Scenarios/Emergency/TriageScenarioObjective.cs` | Nuevo | Implementación real del puerto + superficie aditiva |
| `Runtime/Scenarios/Emergency/TriageKey.cs`, `TriageKeyLoader.cs` | Nuevo | POCO y lector mínimo del bloque `clave` |
| `Runtime/Scenarios/Emergency/Config/TriageObjectiveSettings.cs` | Nuevo | Pesos y longitud de racha, inyectables |
| `Tests/EditMode/Scenarios/Emergency/` | Modificado | Contrato heredado + pruebas de mezcla, lector y bordes |
| `openspec/specs/escenario-emergencia-m9/spec.md` | Nuevo | Primera spec formal de M9 |
| `Docs/MODULES.md` (sección M9) | Modificado | Cerrar "solo doble" y documentar el consumo de `clave` |
| `Runtime/Scenarios/Emergency/Fakes/ScriptedScenarioObjective.cs` | Sin cambio | Sigue siendo el doble del puerto |
| `Runtime/Scenarios/Emergency/NpcAi.Scenarios.Emergency.asmdef` | Sin cambio | `NpcAi.Core` + `NpcAi.Core.Channels`, ninguna referencia nueva |
| `Runtime/Core/`, `Runtime/CoreChannels/` | Sin cambio | Contrato congelado |
| `Runtime/ClinicalResponse/`, `Runtime/Scenarios/Boardroom/` | Sin cambio | Otros módulos (regla dura 1) |
| `Data/Cases/*.json` | Sin cambio | Dato de M14, solo lectura |

## Risks

| Riesgo | Prob. | Mitigación |
|---|---|---|
| **M11 no existe** (`Docs/MODULES.md` M11: solo `Samples~/Harness/README.md`, sin escena ni script), así que nadie alimenta hoy la señal clínica real | Alta | La superficie aditiva está diseñada para que la señal sea suministrable **sin cambiar M15**: el compositor reporta "esta bandera roja se superficiada" / "declaró triaje X". Todo es probable hoy en EditMode con llamadas sintéticas; el "cómo se entera el compositor" es diseño de M11, no deuda de M9 |
| Olvidar `AssignCase` produce un objetivo completable que no midió nada clínico (consecuencia directa de la renormalización) | Media | `HasKey` es lectura pública, la spec lo exige y una prueba afirma que sin clave el progreso es puramente receptivo; el fallo queda visible en lugar de disfrazado de techo 0.3 |
| Los pesos por defecto (0.3 / 0.7, y 0.7 / 0.3 dentro de la mitad clínica) son un juicio pedagógico — discutido con el usuario el 2026-09-17, inclinado deliberadamente hacia lo clínico porque es el motivo por el que `clave` existe — pero sin validar todavía con el asesor | Media | Viven en `TriageObjectiveSettings` inyectable, no incrustados en la lógica; cambiarlos no toca una línea de puntaje, y promoverlos a asset queda como cambio posterior trivial |
| Los índices de bandera roja acoplan a M9 al **orden** del arreglo en `Data/Cases/*.json`: reordenar un caso cambia el significado de un índice | Media | Se documenta en la spec y en `Docs/MODULES.md` que `clave.banderasRojas` es una lista ordenada y estable; el índice se valida contra la longitud cargada y M9 nunca lo persiste entre sesiones (M13 registra el texto, no el índice) |
| `JsonUtility` no deserializa cómodamente un bloque anidado, y el lector podría quedar frágil | Media | Es el mismo problema que `ClinicalCaseLoader` ya resolvió con tipos `Raw*` privados; se reutiliza ese patrón, con pruebas de JSON malformado, `clave` ausente y `banderasRojas` vacío |
| La superficie aditiva se lee como "M9 terminado" | Media | Propuesta, spec y sección de `Docs/MODULES.md` nombran explícitamente lo diferido: evaluación del `cierreEsperado`, integración rica con M15 y el cableado de M11 |
| Sin CI ni runner headless, el verde depende de una persona | Alta | Misma **compuerta humana** de EditMode ya establecida por M1, M2 y M7: el usuario corre Test Runner y registra el total en `apply-progress.md`. **No aplica compuerta física** (ver Success Criteria) |

## Rollback Plan

Todo lo de M9 es aditivo sobre un puerto que ya existe, y el doble no se toca. Revertir los commits
de este cambio deja M9 en el estado "solo doble" que ya está en `main`: el pipeline sigue
funcionando porque ningún otro módulo depende de `TriageScenarioObjective` por nombre, solo de
`IScenarioObjective`, y M10 nunca lo vio. No hay migración de datos, no se crea ningún asset ni
carpeta en `Data/`, no hay dependencia nueva en `package.json`, no hay cambio de
`Contract.Version` y `NpcAi.Scenarios.Emergency.asmdef` queda igual que antes. Basta borrar los
archivos nuevos y revertir la sección M9 de `Docs/MODULES.md`.

## Dependencies

- `contrato-nucleo-m0`, archivado y estable: define `IScenarioObjective` y `ReceptivityChange`. **No
  se modifica.**
- `ClinicalCaseId` (`Runtime/Core/ClinicalCaseId.cs`) ya vive en `NpcAi.Core`: M9 lo usa con **cero
  referencias nuevas** entre módulos.
- `Data/Cases/*.json` con bloque `clave` poblado (ya existe en los tres casos). Solo lectura, y a
  través del `Func<ClinicalCaseId,string>` inyectado, nunca por acceso directo a disco.
- `Tests/EditMode/Core/ScenarioObjectiveContract.cs`, existente y **no modificado**.
- **Ninguna dependencia bloqueante en M15 ni en M11.** El seguimiento sobre M15 (exponer el
  `Hecho`/`campo` que hizo *match*) es un cambio SDD aparte, posterior y **no bloqueante** para
  esta entrega.
- Ningún paquete nuevo.

## Success Criteria

- [ ] `TriageScenarioObjective` pasa exactamente la misma batería `ScenarioObjectiveContract` que
      `ScriptedScenarioObjective`, **sin modificar la clase base**, y sin dejar ninguna de las 7
      pruebas inconclusa (la renormalización sin caso hace que el sujeto del contrato sí pueda
      completar).
- [ ] La mezcla es demostrable en EditMode: con caso asignado, ni la receptividad sostenida sola ni
      la corrección clínica sola llegan a `Progress01 == 1`; hacen falta las dos.
- [ ] `IsComplete` se revierte por **ambas** vías, cada una con su prueba: un `Worsened` tras haber
      completado, y una declaración de triaje equivocada tras haber acertado.
- [ ] `RegisterRedFlag` es idempotente por índice, rechaza índices fuera de rango sin lanzar, y no
      tiene efecto antes de `AssignCase`.
- [ ] El lector de `clave` se prueba con JSON **en memoria** a través del
      `Func<ClinicalCaseId,string>` inyectado: caso feliz, `clave` ausente, JSON malformado,
      `banderasRojas` vacío y `triajeEsperado` fuera de `{"I".."V"}`. M9 nunca llama a
      `File.ReadAllText`.
- [ ] `HasKey` es falso tras un `cargarJson` nulo, que lanza, o que devuelve un caso sin `clave`, y
      ninguno de esos tres escenarios propaga excepción.
- [ ] Ningún `Debug.Log` en el código de runtime del módulo.
- [ ] **Compuerta humana (la ejecuta y la registra el usuario, no el agente)**: Unity Test Runner >
      EditMode > Run All en verde, con el total de pruebas registrado en `apply-progress.md`.
- [ ] **No aplica compuerta física ni de hardware.** A diferencia de M1 (micrófono), M2 (modelo
      entrenado) y M7 (Quest 3), M9 es lógica de escenario del lado del servidor: cero dependencia
      de VR, XR, audio, `MonoBehaviour` o escena. La única confirmación humana necesaria es el
      EditMode de arriba, y eso queda escrito así en la spec para que nadie la busque después.
- [ ] `openspec/specs/escenario-emergencia-m9/spec.md` existe y traza cada requisito a una prueba
      EditMode en verde.
- [ ] `Docs/MODULES.md` (sección M9) deja de decir "solo doble", documenta el consumo de `clave` que
      hoy solo aparece en los documentos de datos, y **nombra lo diferido**: `cierreEsperado`,
      integración rica con M15 y cableado de M11.
- [ ] El diff no toca `Runtime/Core/`, `Runtime/CoreChannels/`, `Runtime/ClinicalResponse/`,
      `Runtime/Scenarios/Boardroom/`, `Data/`, ni ninguna carpeta fuera de
      `Runtime/Scenarios/Emergency/`, `Tests/EditMode/Scenarios/Emergency/`, `Docs/` y `openspec/`.

## Decisiones del usuario (confirmadas 2026-09-17)

1. **Opción 2 de la exploración: superficie de API aditiva propiedad de M9, más allá del puerto
   congelado `IScenarioObjective`, sin cambio de contrato de M0.** Se replica el precedente del
   propio repositorio: `BertIntentClassifier` (M2) expone `EmitirParaPrueba` más allá de
   `IIntentClassifier`; `VrInputBehaviour` (M7) expone
   `CablearParaPrueba`/`DescablearParaPrueba`/`BombearParaPrueba` más allá de lo que exige
   cualquier puerto. La clase concreta de M9 sigue satisfaciendo `IScenarioObjective`
   (`Progress01`/`IsComplete`/`Notify(ReceptivityChange)`) sin modificar ni tocar nada en el nivel
   de la interfaz — los métodos extra viven solo en la clase concreta, y los llama quien componga
   M9 en una escena real (M11, cuando exista) o las pruebas directamente.
2. **`Progress01`/`IsComplete` miden una mezcla de receptividad sostenida Y corrección clínica**
   (identificar las `banderasRojas` del caso, alcanzar el `triajeEsperado` correcto) — no una de
   las dos sola. `Notify(ReceptivityChange)` sigue alimentando la mitad de receptividad; la API
   aditiva (decisión 1) es la vía por la que entra la mitad de corrección clínica.
3. **Pesos por defecto: 0.3 receptividad / 0.7 corrección clínica; dentro de la mitad clínica, 0.7
   banderas rojas descubiertas / 0.3 triaje declarado.** Inclinado a propósito hacia lo clínico
   (frente al 0.4/0.6 y 0.6/0.4 que proponía el primer borrador): el motivo por el que `clave`
   existe y está marcada como exclusiva de M9 en tres documentos es medir competencia clínica, no
   trato interpersonal — y la mitad de receptividad ya tiene un castigo duro (`Worsened` resetea
   la racha completa a 0), así que un peso más alto ahí sobre-pesaría un mal momento de trato por
   encima de un error clínico real. Dentro de lo clínico, encontrar las banderas rojas (el proceso
   diagnóstico) pesa más que declarar bien el triaje (la respuesta final), porque acertar el
   triaje sin haber indagado nada no demuestra la competencia que el escenario busca entrenar. La
   granularidad del crédito por bandera roja queda proporcional (fracción encontrada, no
   todo-o-nada ni ponderada por severidad — eso último exigiría un campo nuevo en el esquema de
   M14, fuera de alcance de este cambio). Viven en `TriageObjectiveSettings`, inyectables, sin
   validar todavía con el asesor.
