# Historial del contrato (`NpcAi.Core`)

Toda modificacion a `NpcAi.Core` sube `Contract.Version` y deja una entrada aqui.
Regla: un cambio de contrato a `Runtime/Core` o `Runtime/CoreChannels` es su propio cambio SDD, revisado antes del merge por el otro dueno compartido de M0 (o, en su defecto, el asesor). Sin ventana fija ni quorum de todos los duenos.

## v2 — 2026-09-09 — Puerto de respuesta clinica (M15)

Primer cambio de contrato despues del congelamiento de v1. Extension **puramente aditiva**:
ningun enum, DTO ni puerto de v1 cambia (nombre, valor, orden, cardinalidad, firma). Agrega
la superficie que permite que M15 (respondedor clinico) exista como modulo de primera clase.

### Tipos nuevos

- **DTO inmutables:** `ClinicalCaseId`, `ClinicalResponse` (los DTO pasan de 5 a 7).
- **Puertos:** `IClinicalResponder` (los puertos pasan de 7 a 8).

Los tres son C# puro (`string`, `bool`, `struct` y tipos de v1): `NpcAi.Core` mantiene
`noEngineReferences: true` y cero referencias `NpcAi.*` ajenas (`CoreAssemblyPurityTests` lo verifica).

### `ClinicalCaseId` — identificador estable sobre string

`readonly struct` sobre `string`, **espejo exacto de `PersonalityId`** (decision 1 de v1):
`Value` normalizado a minusculas y sin espacios de borde (`Trim().ToLowerInvariant()`), `null`
/ vacio / solo espacios ⇒ `Value == null`; igualdad completa y `Ordinal` (`Equals`,
`GetHashCode`, `operator ==`, `operator !=`); `None = default`; `IsNone == true` solo para
`None`; `GetHashCode()` de `None` es `0`. El numero de casos clinicos es dato de M14
(`Data/Cases/`), **nunca** un cambio de contrato.

### `ClinicalResponse` — senal de enrutado

`readonly struct` con `Handled` (bool) y `Reply` (`NpcReply`). `ClinicalResponse.NoAplica`
DEBE tener `Handled == false`. `Handled == false` DEBE significar "turno no clinico": el
llamador enruta a `IDialogueGenerator` (M6) y `Reply` NO tiene garantias. `Handled == true`
DEBE significar que `Reply` va tal cual a M8. NO implementa `IEquatable<T>` (igualdad
estructural por defecto, igual que los DTO de v1 distintos de `PersonalityId`).

### Invariantes de `IClinicalResponder`

Toda implementacion real (`Runtime/<Modulo>/`) y todo doble (`Runtime/<Modulo>/Fakes/`) DEBE
heredar `NpcAi.Core.Tests.ClinicalResponderContract` y pasar el 100% de sus `[Test]`, sin
escena de Unity ni entorno de VR.

- **`IsReady`**: leer NO DEBE lanzar en ningun estado. DEBE ser `false` hasta que `AssignCase` vincule un `ClinicalCaseId` existente en M14.
- **`AssignCase(ClinicalCaseId, PersonalityId)`**: con el mismo par DEBE ser determinista e idempotente. Con un `ClinicalCaseId` desconocido NO DEBE lanzar y DEBE dejar `IsReady == false`. Caso y personalidad son estado de sesion, no de turno (mismo patron que `IReceptivityEngine.Reset`).
- **`Respond(Utterance, IntentResult)`**: NO DEBE lanzar en ningun estado (sin `AssignCase`, con `Utterance` vacio o `default`, con `IntentResult` `default`, con solo simbolos, con cadenas de 5000 caracteres). Con `IsReady == false` DEBE devolver `ClinicalResponse.NoAplica`. Cuando `Handled == true`, `Reply.Text` NO DEBE ser vacio ni solo espacios y `Reply.EmotionTag` / `Reply.AnimationCue` NO DEBEN ser `null`.

### Asimetria de determinismo

A la de v1 (`IIntentClassifier.Classify` determinista en `Intent` / `Tone`;
`IDialogueGenerator.Generate` NO obligado a serlo) se suma la de v2, **opuesta** a la de M6:
`IClinicalResponder.Respond` **DEBE** ser determinista en `Handled` y en `Reply.Text` para la
misma tupla `(ClinicalCaseId, PersonalityId, Utterance, IntentResult)` (`Reply.EmotionTag`,
`Reply.AnimationCue` y cualquier latencia NO estan obligados). Un hecho clinico ("hace 2
meses", "alergica al Tramadol") no puede cambiar de redaccion entre turnos: M15 usa
plantillas, no cadenas de Markov. Una prueba de contrato de `Respond` SI debe exigir igualdad
de `Handled` y `Reply.Text` entre dos llamadas con la misma entrada.

## v1 — 2026-08-30 — Contrato inicial congelado (Sprint 0)

Primera version. Define:

- **DTO inmutables:** `Utterance`, `IntentResult`, `ReceptivityChange`, `NpcReply`, `PersonalityId`.
- **Enums:** `Intent`, `Tone`, `PhysicalAction`, `Receptivity`.
- **Siete puertos:** `ISpeechToText`, `IIntentClassifier`, `IPhysicalActionSource`, `IReceptivityEngine`, `IDialogueGenerator`, `INpcPresenter`, `IScenarioObjective`.

Decisiones de diseno que quedan registradas con la v1:

1. `PersonalityId` es un `readonly struct` sobre `string`, no un enum. **El numero de personalidades es un dato de M5, no un cambio de contrato.** Pasar de 4 a 8 personalidades no toca este archivo.
2. Todos los enums tienen el valor seguro en `0` (`Intent.Desconocida`, `Tone.Neutral`, `PhysicalAction.Ninguna`). Un valor sin inicializar nunca significa algo fuerte.
3. `Receptivity` se numera `-1 / 0 / 1` para que el orden sea comparable y las transiciones se puedan probar aritmeticamente.
4. `NpcAi.Core` se compila con `noEngineReferences: true`. El contrato es C# puro **por compilacion**, no por convencion.
5. Los canales de evento (ScriptableObject) viven en `NpcAi.Core.Channels`, un ensamblado aparte que si referencia Unity.

### Enums congelados en v1 (nombre = valor)

El nombre, el valor numerico, el orden y la cardinalidad de cada enum quedan fijados. Cualquier rename, reorden o cambio de valor futuro es, **por diseno**, una ruptura de contrato (cambio SDD de contrato propio, co-revisado por el otro dueno de M0 o el asesor antes del merge; ver la regla del encabezado). El miembro con valor `0` es el neutro o seguro de cada enum.

| Enum | Miembros |
|---|---|
| `Intent` | `Desconocida=0`, `SolicitudRespetuosa=1`, `SolicitudAgresiva=2`, `Empatia=3`, `AportaInformacion=4`, `PreguntaFueraDeTema=5`, `Interrupcion=6` |
| `Tone` | `Neutral=0`, `Respetuoso=1`, `Agresivo=2`, `Empatico=3`, `Ansioso=4` |
| `PhysicalAction` | `Ninguna=0`, `ContactoVisual=1`, `Acercarse=2`, `Alejarse=3`, `EntregarObjeto=4`, `SenalarPantalla=5`, `GestoCalma=6`, `TocarPaciente=7` |
| `Receptivity` | `NoReceptivo=-1`, `Neutral=0`, `Receptivo=1` |

### Invariantes por puerto

Toda implementacion real y todo doble de modulo DEBE cumplir estos invariantes. El mecanismo de verificacion son las clases base `*Contract.cs` de `NpcAi.Core.Tests` (heredadas por los 7 dobles) y las pruebas de `Tests/EditMode/Core/`.

- **`ISpeechToText`**: arranca con `IsListening == false`. `StartListening` deja `true`, `StopListening` deja `false` y es idempotente. NO emite `OnUtterance` antes del primer `StartListening` ni despues de un `StopListening`. Mientras escucha, reemite el `Utterance` recibido sin alterarlo. La secuencia `Start` / `Stop` / `Start` reanuda la emision. Fan-out a todos los suscriptores; emitir sin suscriptores NO lanza.
- **`IPhysicalActionSource`**: entrega cada `PhysicalAction` a los suscriptores vigentes de `OnAction` (fan-out). Un handler quitado con `-=` deja de recibir. `PhysicalAction.Ninguna` NUNCA se emite: es centinela, no evento. Emitir sin suscriptores NO lanza.
- **`IIntentClassifier`**: `Classify` NO DEBE lanzar en ningun estado (ni con `IsReady == false`, ni con `null`, vacio, solo espacios, simbolos, cadenas muy largas o numeros). Ante texto `null` / `""` / solo espacios devuelve `Intent.Desconocida` con `Confidence == 0`. Para una misma entrada, `Intent` y `Tone` son deterministas; `Confidence` y `LatencyMs` NO estan obligados a serlo. Leer `IsReady` NO lanza. Cuando `IsReady == false`, `Classify` **DEBERIA** devolver `IntentResult.Unknown` (recomendacion, no regla dura).
- **`IReceptivityEngine`**: `Current` es `Receptivity.Neutral` hasta que se llame `Reset`. `Reset` con la misma `PersonalityId` es determinista e idempotente y restaura el estado inicial. `Evaluate` devuelve `result.From` igual al `Current` previo y `result.To` igual al `Current` posterior, con `ReasonCode` no nulo ni vacio. Bajo agresion sostenida `(int)Current` NO sube; bajo empatia sostenida NO baja. `Evaluate(IntentResult.Unknown(), PhysicalAction.Ninguna)` NO mueve el estado y su `result.Changed` es `false`.
- **`IDialogueGenerator`**: `Generate` NUNCA devuelve `Text` vacio para ningun valor de `Receptivity`. `EmotionTag` y `AnimationCue` NUNCA son `null`. Funciona con `PersonalityId.None` sin lanzar. `Receptivo` y `NoReceptivo` producen `Text` distinto. `Generate` PUEDE variar su salida entre llamadas con la misma entrada: **NO se exige determinismo** (ver "Asimetria de determinismo").
- **`INpcPresenter`**: `Play` es un sumidero sin retorno observable. `Play` con un `NpcReply` normal NO lanza. `Play(default)` NO lanza. Diez reproducciones encadenadas NO lanzan.
- **`IScenarioObjective`**: arranca con `Progress01 == 0` e `IsComplete == false`. `Progress01` permanece en `[0,1]` tras cualquier secuencia de `Notify` y NO baja de `0`. `IsComplete` es `true` **si y solo si** `Progress01 == 1` (tolerancia `1e-4`). La completitud es **REVERSIBLE** y NO pegajosa: tras `IsComplete == true`, un `Notify` que reduzca el progreso PUEDE devolver `IsComplete` a `false`. `Notify(default)` es no-op.

### `Score` y `ReasonCode` son diagnostico no tipado

`ReceptivityChange.Score` (int) es un puntaje interno del motor tras la evaluacion: **sin rango garantizado** y sin significado de contrato. `ReceptivityChange.ReasonCode` (string) es una etiqueta de diagnostico legible: el constructor normaliza `null` a `""`, un `ReasonCode` construido a mano PUEDE ser vacio y **los consumidores NO DEBEN ramificar su logica segun su contenido**. La direccion de una transicion se lee de `From` / `To` (`Changed`, `Improved`, `Worsened`), nunca de `Score` ni de `ReasonCode`.

### Asimetria de determinismo

Decision deliberada del contrato: `IIntentClassifier.Classify` **DEBE** ser determinista en `Intent` y `Tone` para una misma entrada; `IDialogueGenerator.Generate` **NO** esta obligado a serlo (el generador real usa cadenas de Markov y variacion por personalidad). Una prueba de contrato no debe exigir igualdad entre dos llamadas a `Generate`.

### Reglas centinela

- El miembro de enum con valor `0` es siempre el neutro/seguro: un campo sin inicializar nunca significa algo fuerte.
- `PhysicalAction.Ninguna` es centinela, no evento: `IPhysicalActionSource` NUNCA lo emite.
- `IntentResult.Unknown(latencyMs = 0)` es el resultado seguro para entradas vacias o no entendidas: devuelve `(Intent.Desconocida, Tone.Neutral, Confidence == 0f, latencyMs)` y **preserva el `latencyMs` recibido**.
- `Receptivity.Neutral == 0` es el estado inicial de `IReceptivityEngine` antes de cualquier `Reset`.

### Politica de igualdad de los DTO

`Utterance`, `IntentResult`, `ReceptivityChange` y `NpcReply` **NO** implementan `IEquatable<T>` ni sobrecargan `Equals` / `GetHashCode` en v1: su igualdad es la estructural por defecto de los value types de C#. Solo `PersonalityId` tiene igualdad completa y explicita (`Equals`, `GetHashCode`, `operator ==`, `operator !=`, comparacion `Ordinal`). Los consumidores NO DEBERIAN depender del rendimiento de la igualdad por defecto en rutas calientes.

- **G12 (diferido a v2):** anadir `IEquatable<T>` / `GetHashCode` a los otros cuatro DTO queda para v2 y sera un cambio de contrato.
- **G14 (diferido a v2):** el recorte (clamp) de rangos float (`Confidence` en `[0,1]`, `DurationSeconds` y `LatencyMs` en `>= 0`) es **contrato del productor** (M1, M2), no invariante del struct. Los structs NO validan ni recortan esos rangos en v1; formalizar un clamp queda para v2.

### `Contract.Version` es `const`

`Contract.Version` es `public const int Version = 1`. Riesgo conocido y aceptado: al ser `const`, el valor se **hornea** en cada ensamblado consumidor en tiempo de compilacion, asi que un consumidor no recompilado puede reportar una version vieja. Cambiar `const` por `static readonly` es una preocupacion de empaquetado que NO se mezcla con esta formalizacion; se evaluara por separado. En v1 el valor permanece congelado en `1`.

### Enforcement

Las 15 afirmaciones del contrato (G1-G15) dejan de ser solo comentarios: se verifican desde `Tests/EditMode/Core/`.

- `ContractVersionChangelogTests`: ata `Contract.Version` al mayor `## v<N>` de este archivo (`Assert.Ignore` si el archivo no esta).
- `CoreAssemblyPurityTests`: reflexion sobre `NpcAi.Core`, cero referencias `UnityEngine*` / `UnityEditor*` y cero `NpcAi.*` ajeno.
- `ContractTypeTests`: enums congelados (nombre, valor, orden, conteo), guardas nulas de `NpcReply` / `ReceptivityChange`, `IntentResult.Unknown` preserva latencia, direccion de `ReceptivityChange`.
- `EventChannelTests`: semantica de `EventChannel<T>` y de los 5 canales concretos de `NpcAi.Core.Channels`.
- Clases base `*Contract.cs`: metodos `[Test]` **no abstractos** heredados por los 7 dobles de modulo; verifican la semantica de los 7 puertos sin que ningun doble cambie una linea.

`G12` y `G14` no producen codigo en v1: quedan registrados arriba como diferidos a v2.
