# Especificacion: contrato-nucleo-m0

## Purpose

Congela los invariantes del contrato v1 de `NpcAi.Core`: `Contract.Version`, la pureza del
ensamblado, 4 enums, 5 DTO y 7 puertos. Toda implementacion real y todo doble de modulo
(M1-M10) DEBE cumplir estos invariantes; el mecanismo de verificacion son las clases base de
prueba de contrato de `NpcAi.Core.Tests` y las pruebas de `Tests/EditMode/Core/`. Esta
capacidad documenta y fija el contrato ya existente (commit `a7a0590`); no lo reescribe.

## ADDED Requirements

### Requirement: Version del contrato

`Contract.Version` DEBE ser `1` y DEBE permanecer congelado en este cambio. Una prueba DEBE
atar ese valor al mayor encabezado `## v<N>` de `Docs/CONTRACT-CHANGELOG.md`. Si ese archivo
no existe, la prueba DEBE llamar `Assert.Ignore`, no fallar.

#### Scenario: La version publicada es 1

- Dado el ensamblado `NpcAi.Core`
- Cuando se lee `Contract.Version`
- Entonces vale `1`

#### Scenario: La version coincide con el changelog

- Dado que `Docs/CONTRACT-CHANGELOG.md` existe
- Cuando se toma el mayor `## v<N>` del archivo
- Entonces `N` es igual a `Contract.Version`

#### Scenario: Sin changelog la prueba se ignora

- Dado que `Docs/CONTRACT-CHANGELOG.md` no existe
- Cuando corre la prueba que compara version y changelog
- Entonces llama `Assert.Ignore` y no falla

### Requirement: Pureza del ensamblado NpcAi.Core

El ensamblado `NpcAi.Core` NO DEBE referenciar `UnityEngine`, `UnityEditor` ni ningun
ensamblado `NpcAi.*` ajeno. DEBE existir una prueba de reflexion que recorra las referencias
del ensamblado y lo verifique.

#### Scenario: Sin dependencias de Unity ni de otros modulos

- Dado el ensamblado compilado `NpcAi.Core`
- Cuando la prueba de reflexion lista sus ensamblados referenciados
- Entonces ninguno es `UnityEngine`, `UnityEditor` ni un modulo `NpcAi.*`

### Requirement: Enums congelados en v1

Los 4 enums DEBEN tener exactamente estos miembros, nombres y valores numericos, y el conteo
de miembros DEBE coincidir. El miembro con valor `0` DEBE ser el valor neutro o seguro de cada
enum. Cualquier rename, reorden o cambio de valor futuro es, por diseno, una ruptura de
contrato.

| Enum | Miembros (nombre = valor) |
|---|---|
| `Intent` | `Desconocida=0`, `SolicitudRespetuosa=1`, `SolicitudAgresiva=2`, `Empatia=3`, `AportaInformacion=4`, `PreguntaFueraDeTema=5`, `Interrupcion=6` |
| `Tone` | `Neutral=0`, `Respetuoso=1`, `Agresivo=2`, `Empatico=3`, `Ansioso=4` |
| `PhysicalAction` | `Ninguna=0`, `ContactoVisual=1`, `Acercarse=2`, `Alejarse=3`, `EntregarObjeto=4`, `SenalarPantalla=5`, `GestoCalma=6`, `TocarPaciente=7` |
| `Receptivity` | `NoReceptivo=-1`, `Neutral=0`, `Receptivo=1` |

#### Scenario: Cada enum expone exactamente los miembros fijados

- Dado un enum del contrato
- Cuando se comparan `Enum.GetNames` y `Enum.GetValues` con su fila de la tabla
- Entonces coinciden nombre, valor y conteo, sin miembros de mas ni de menos

#### Scenario: El valor cero es el seguro

- Dado cualquiera de los 4 enums
- Cuando se evalua el miembro con valor `0`
- Entonces es `Intent.Desconocida`, `Tone.Neutral`, `PhysicalAction.Ninguna` o `Receptivity.Neutral` segun el enum

#### Scenario: Receptivity esta ordenada -1/0/1

- Dado el enum `Receptivity`
- Cuando se comparan sus miembros como enteros
- Entonces `(int)NoReceptivo < (int)Neutral < (int)Receptivo`, con `NoReceptivo == -1` y `Receptivo == 1`

### Requirement: DTO Utterance

`Utterance` DEBE exponer los campos de solo lectura `Text` (string), `Confidence` (float) y
`DurationSeconds` (float), fijados por el constructor. `IsEmpty` DEBE ser
`string.IsNullOrWhiteSpace(Text)`. Los rangos `Confidence` en `[0,1]` y `DurationSeconds` en
`>= 0` son contrato del productor (M1); el struct NO DEBE validarlos ni recortarlos.

#### Scenario: IsEmpty refleja el texto

- Dado `new Utterance(null, c, d)`, `new Utterance("", c, d)` o `new Utterance("   ", c, d)`
- Cuando se lee `IsEmpty`
- Entonces es `true`; con texto no vacio es `false`

#### Scenario: El struct no recorta rangos

- Dado `new Utterance("hola", 5f, -2f)`
- Cuando se leen `Confidence` y `DurationSeconds`
- Entonces devuelven `5f` y `-2f` sin cambios

### Requirement: DTO IntentResult

`IntentResult` DEBE exponer `Intent`, `Tone`, `Confidence` (float) y `LatencyMs` (float).
`Confidence` en `[0,1]` y `LatencyMs` en `>= 0` son contrato del productor (M2), no invariantes
del struct. `IntentResult.Unknown(latencyMs = 0)` DEBE devolver
`(Intent.Desconocida, Tone.Neutral, 0f, latencyMs)` y DEBE preservar el `latencyMs` recibido.

#### Scenario: Unknown por defecto es seguro

- Dado `IntentResult.Unknown()`
- Cuando se leen sus campos
- Entonces son `Desconocida`, `Neutral`, `Confidence == 0f` y `LatencyMs == 0f`

#### Scenario: Unknown preserva la latencia

- Dado `IntentResult.Unknown(12.5f)`
- Cuando se lee `LatencyMs`
- Entonces vale `12.5f` y `Intent`, `Tone` y `Confidence` siguen siendo los seguros

### Requirement: DTO ReceptivityChange

`ReceptivityChange` DEBE exponer `From`, `To`, `Score` (int) y `ReasonCode` (string), con
`Changed == (From != To)`, `Improved == ((int)To > (int)From)` y
`Worsened == ((int)To < (int)From)`. El constructor DEBE normalizar `ReasonCode` nulo a la
cadena vacia. Un `ReasonCode` construido a mano PUEDE ser vacio y los consumidores NO DEBEN
ramificar su logica segun su contenido. `Score` es diagnostico no tipado, sin rango
garantizado.

#### Scenario: La direccion se calcula a partir de From y To

- Dado `new ReceptivityChange(Neutral, Receptivo, 2, "X")` y `new ReceptivityChange(Neutral, NoReceptivo, -2, "Y")`
- Cuando se leen `Improved`, `Worsened` y `Changed`
- Entonces el primero es `Improved`, el segundo es `Worsened` y `new ReceptivityChange(Neutral, Neutral, 0, "Z")` no esta `Changed`

#### Scenario: El constructor normaliza ReasonCode nulo

- Dado `new ReceptivityChange(Neutral, Neutral, 0, null)`
- Cuando se lee `ReasonCode`
- Entonces es `""` y no `null`

### Requirement: DTO NpcReply

`NpcReply` DEBE exponer `Text`, `EmotionTag` y `AnimationCue` (string). El constructor DEBE
normalizar los tres a la cadena vacia cuando el argumento es nulo. `IsEmpty` DEBE ser
`string.IsNullOrWhiteSpace(Text)`.

#### Scenario: El constructor normaliza los tres string nulos

- Dado `new NpcReply(null, null, null)`
- Cuando se leen los tres campos
- Entonces los tres son `""` y `IsEmpty` es `true`

#### Scenario: IsEmpty refleja el texto util

- Dado `new NpcReply("Lo escucho.", "", "")`
- Cuando se lee `IsEmpty`
- Entonces es `false`

### Requirement: Identificador PersonalityId

`PersonalityId` DEBE ser un `readonly struct` sobre `string`, NO un enum. El constructor DEBE
normalizar con `Trim` y `ToLowerInvariant`, y DEBE dejar `Value` en `null` cuando el argumento
es nulo, vacio o solo espacios. `None` DEBE ser `default` y `IsNone` DEBE ser verdadero cuando
`Value` es nulo o vacio. La igualdad DEBE ser completa y `Ordinal` (`Equals`, `GetHashCode`,
`operator ==`, `operator !=`). El numero de personalidades es dato de M5 y NUNCA un cambio de
contrato.

#### Scenario: Normaliza y compara por valor

- Dado `new PersonalityId("Grosero")` y `new PersonalityId("  grosero ")`
- Cuando se comparan
- Entonces son iguales y `Value` es `"grosero"`

#### Scenario: None e IsNone

- Dado `PersonalityId.None` y `new PersonalityId("   ")`
- Cuando se lee `IsNone`
- Entonces es `true` en ambos, y `false` para un id con valor

#### Scenario: Igualdad completa

- Dado dos `PersonalityId` con el mismo `Value`
- Cuando se comparan con `Equals`, `==`, `!=` y `GetHashCode`
- Entonces `Equals` y `==` son `true`, `!=` es `false` y los `GetHashCode` coinciden

### Requirement: Politica de igualdad de los DTO

`Utterance`, `IntentResult`, `ReceptivityChange` y `NpcReply` NO implementan `IEquatable<T>`
ni sobrecargan `Equals` o `GetHashCode` en v1; su igualdad es la estructural por defecto de
los value types de C#. Solo `PersonalityId` tiene igualdad completa y explicita. Anadir
`IEquatable`/`GetHashCode` a los otros cuatro queda diferido a v2 y sera un cambio de
contrato. Los consumidores NO DEBERIAN depender del rendimiento de la igualdad por defecto en
rutas calientes.

#### Scenario: Los cuatro DTO no declaran igualdad propia

- Dado los tipos `Utterance`, `IntentResult`, `ReceptivityChange` y `NpcReply`
- Cuando se inspeccionan por reflexion
- Entonces ninguno implementa `IEquatable<T>`, mientras que `PersonalityId` si lo hace

#### Scenario: Igualdad estructural por defecto

- Dado dos `NpcReply` con los mismos tres campos
- Cuando se comparan con `Equals`
- Entonces son iguales; con un campo distinto no lo son

### Requirement: Puerto ISpeechToText

Una implementacion de `ISpeechToText` DEBE arrancar con `IsListening == false`.
`StartListening` DEBE dejar `IsListening == true` y `StopListening` DEBE dejarlo en `false`.
NO DEBE emitir `OnUtterance` antes del primer `StartListening` ni despues de un
`StopListening`. Mientras escucha, DEBE reemitir el `Utterance` recibido sin alterarlo.
`StopListening` DEBE ser idempotente. La secuencia `Start`/`Stop`/`Start` DEBE reanudar la
emision. DEBE entregar cada `Utterance` a todos los suscriptores (fan-out) y NO DEBE lanzar
si no hay suscriptores.

#### Scenario: Estado inicial y transiciones

- Dado un sujeto recien creado
- Cuando se llama `StartListening` y luego `StopListening`
- Entonces `IsListening` pasa de `false` a `true` y vuelve a `false`

#### Scenario: No emite fuera de la ventana de escucha

- Dado un suscriptor a `OnUtterance`
- Cuando llega un `Utterance` antes del primer `StartListening` o despues de `StopListening`
- Entonces el suscriptor no recibe nada

#### Scenario: Reemite lo que escucha

- Dado el sujeto escuchando y un suscriptor
- Cuando llega `new Utterance("necesito ayuda", 0.8f, 1.2f)`
- Entonces el suscriptor recibe un `Utterance` con `Text == "necesito ayuda"`

#### Scenario: Stop idempotente y reanudacion

- Dado el sujeto tras `StartListening` y `StopListening`
- Cuando se llama `StopListening` otra vez y luego `StartListening`
- Entonces no se lanza excepcion y la emision se reanuda

#### Scenario: Fan-out y ausencia de suscriptores

- Dado dos suscriptores, o ninguno
- Cuando el sujeto escucha y llega un `Utterance`
- Entonces ambos suscriptores lo reciben y sin suscriptores no se lanza excepcion

### Requirement: Puerto IPhysicalActionSource

Una implementacion de `IPhysicalActionSource` DEBE entregar cada `PhysicalAction` a los
suscriptores vigentes de `OnAction` (fan-out). Un handler quitado con `-=` NO DEBE seguir
recibiendo. `PhysicalAction.Ninguna` NUNCA DEBE emitirse: es centinela, no evento. Emitir sin
suscriptores NO DEBE lanzar.

#### Scenario: Entrega a los suscriptores vigentes

- Dado uno o varios suscriptores a `OnAction`
- Cuando se emite `PhysicalAction.GestoCalma`
- Entonces todos reciben `GestoCalma`

#### Scenario: Un handler desuscrito deja de recibir

- Dado un handler agregado con `+=` y quitado con `-=`
- Cuando se emite una accion
- Entonces ese handler no la recibe

#### Scenario: Ninguna no es un evento

- Dado un suscriptor a `OnAction`
- Cuando se intenta emitir `PhysicalAction.Ninguna`
- Entonces el suscriptor no recibe nada

#### Scenario: Emitir sin suscriptores no lanza

- Dado un sujeto sin suscriptores
- Cuando se emite `PhysicalAction.Acercarse`
- Entonces no se lanza excepcion

### Requirement: Puerto IIntentClassifier

`Classify` NO DEBE lanzar en ningun estado, ni con `IsReady == false`, ni con entradas
atipicas (`null`, vacio, solo espacios, simbolos, cadenas muy largas, numeros). Ante texto
`null`, `""` o solo espacios DEBE devolver `Intent.Desconocida` con `Confidence == 0`.
`Confidence` DEBE quedar en `[0,1]` y `LatencyMs` DEBE ser `>= 0`. Para una misma entrada,
`Intent` y `Tone` DEBEN ser deterministas; `Confidence` y `LatencyMs` NO estan obligados a
serlo. Leer `IsReady` NO DEBE lanzar. Cuando `IsReady == false`, `Classify` DEBERIA devolver
`IntentResult.Unknown`.

#### Scenario: Texto vacio o nulo devuelve Desconocida

- Dado `Classify(null)`, `Classify("")` o `Classify("   ")`
- Cuando se lee el resultado
- Entonces `Intent == Desconocida`, `Confidence == 0f` y no se lanza excepcion

#### Scenario: Determinismo parcial

- Dado dos llamadas a `Classify` con el mismo texto
- Cuando se comparan los resultados
- Entonces `Intent` y `Tone` coinciden; `Confidence` y `LatencyMs` pueden diferir

#### Scenario: Clasificador no listo

- Dado un sujeto con `IsReady == false`
- Cuando se lee `IsReady` y se llama `Classify("por favor")`
- Entonces ninguna llamada lanza y `Classify` DEBERIA devolver `IntentResult.Unknown`

#### Scenario: Entradas raras no rompen los rangos

- Dado `Classify("!!!???")`, `Classify(new string('a', 5000))` y `Classify("123 456")`
- Cuando se leen los resultados
- Entonces ninguna lanza, `Confidence` esta en `[0,1]` y `LatencyMs` es `>= 0`

### Requirement: Puerto IReceptivityEngine

`Current` DEBE ser `Receptivity.Neutral` hasta que se llame `Reset`. `Reset` con la misma
`PersonalityId` DEBE ser determinista e idempotente y DEBE restaurar el estado inicial.
`Evaluate` DEBE devolver `result.From` igual al `Current` previo y `result.To` igual al
`Current` posterior, con `ReasonCode` no nulo ni vacio. Bajo agresion sostenida `(int)Current`
NO DEBE subir; bajo empatia sostenida NO DEBE bajar. `Evaluate(IntentResult.Unknown(),
PhysicalAction.Ninguna)` NO DEBE mover el estado y su `result.Changed` DEBE ser `false`.

#### Scenario: Neutral hasta Reset, y Reset determinista

- Dado dos motores recien creados
- Cuando se lee `Current` sin `Reset`, y luego ambos hacen `Reset(pid)` con la misma personalidad, incluso repetido
- Entonces `Current` es `Neutral` antes del `Reset` y coincide entre ambos motores despues, sin cambiar entre `Reset` repetidos

#### Scenario: Evaluate reporta el estado y una razon

- Dado un motor con `Current` previo `P`
- Cuando se llama `Evaluate(intent, action)`
- Entonces `result.From == P`, `result.To == Current` posterior y `result.ReasonCode` no es nulo ni vacio

#### Scenario: Monotonia bajo presion sostenida

- Dado un motor reiniciado con estado inicial `I`
- Cuando se llama 5 veces `Evaluate` con intencion y tono agresivos, y por separado 5 veces con empatia
- Entonces tras la agresion `(int)Current <= (int)I` y tras la empatia `(int)Current >= (int)I`

#### Scenario: Entrada neutra no mueve el estado

- Dado un motor reiniciado
- Cuando se llama `Evaluate(IntentResult.Unknown(), PhysicalAction.Ninguna)`
- Entonces `Current` no cambia y `result.Changed` es `false`

#### Scenario: Reset restaura el estado inicial

- Dado un motor con estado inicial `I` al que se le aplicaron varias evaluaciones agresivas
- Cuando se llama `Reset(pid)` con la misma personalidad
- Entonces `Current` vuelve a `I`

### Requirement: Puerto IDialogueGenerator

`Generate` NUNCA DEBE devolver `Text` vacio para ningun valor de `Receptivity`. `EmotionTag`
y `AnimationCue` NUNCA DEBEN ser `null`. DEBE funcionar con `PersonalityId.None` sin lanzar.
`Receptivo` y `NoReceptivo` DEBEN producir `Text` distinto. `Generate` PUEDE variar su salida
entre llamadas con la misma entrada: NO se exige determinismo (asimetria deliberada frente a
`IIntentClassifier.Classify`).

#### Scenario: Nunca devuelve texto vacio

- Dado cada valor de `Receptivity`
- Cuando se llama `Generate(pid, receptivity, IntentResult.Unknown())`
- Entonces `reply.IsEmpty` es `false`

#### Scenario: Etiquetas nunca nulas

- Dado un `reply` de `Generate`
- Cuando se leen `EmotionTag` y `AnimationCue`
- Entonces ninguno es `null`

#### Scenario: Funciona sin personalidad

- Dado `PersonalityId.None`
- Cuando se llama `Generate(None, Neutral, IntentResult.Unknown())`
- Entonces no lanza y `reply.IsEmpty` es `false`

#### Scenario: Receptivo y NoReceptivo difieren

- Dado la misma personalidad e intencion
- Cuando se llama `Generate` con `Receptivo` y con `NoReceptivo`
- Entonces los dos `Text` no son iguales

#### Scenario: No se exige determinismo

- Dado dos llamadas a `Generate` con la misma entrada
- Cuando se comparan los `Text`
- Entonces PUEDEN diferir sin violar el contrato

### Requirement: Puerto INpcPresenter

`INpcPresenter.Play` es un sumidero sin retorno observable. `Play` con un `NpcReply` normal
NO DEBE lanzar. `Play(default)` NO DEBE lanzar. Diez reproducciones encadenadas NO DEBEN
lanzar.

#### Scenario: Reproduce una respuesta normal

- Dado un presentador
- Cuando se llama `Play(new NpcReply("Lo escucho.", "neutral", "idle"))`
- Entonces no se lanza excepcion

#### Scenario: Sobrevive a una respuesta vacia

- Dado un presentador
- Cuando se llama `Play(default)`
- Entonces no se lanza excepcion

#### Scenario: Sobrevive a reproducciones encadenadas

- Dado un presentador
- Cuando se llama `Play` 10 veces seguidas con respuestas distintas
- Entonces no se lanza excepcion

### Requirement: Puerto IScenarioObjective

Una implementacion de `IScenarioObjective` DEBE arrancar con `Progress01 == 0` e
`IsComplete == false`. `Progress01` DEBE permanecer en `[0,1]` tras cualquier secuencia de
`Notify` y NO DEBE bajar de `0`. `IsComplete` DEBE ser `true` si y solo si `Progress01 == 1`
(tolerancia `1e-4`). La completitud es REVERSIBLE y NO es pegajosa: tras `IsComplete == true`,
un `Notify` que reduzca el progreso PUEDE devolver `IsComplete` a `false`. `Notify(default)`
DEBE ser no-op.

#### Scenario: Estado inicial

- Dado un objetivo recien creado
- Cuando se leen `Progress01` e `IsComplete`
- Entonces son `0f` y `false`

#### Scenario: Progreso siempre acotado

- Dado cualquier secuencia de `Notify` con cambios de mejora y de empeoramiento
- Cuando se lee `Progress01` en cada paso
- Entonces siempre esta en `[0,1]` y nunca baja de `0`

#### Scenario: IsComplete si y solo si el progreso es total

- Dado un objetivo tras varios `Notify`
- Cuando `IsComplete` es `true`
- Entonces `Progress01` es `1f` con tolerancia `1e-4`; y si `Progress01 < 1` entonces `IsComplete` es `false`

#### Scenario: La completitud es reversible

- Dado un objetivo con `IsComplete == true`
- Cuando un `Notify` reduce `Progress01` por debajo de `1`
- Entonces `IsComplete` PUEDE volver a `false`

#### Scenario: Notify con el valor por defecto es no-op

- Dado un objetivo con progreso `p`
- Cuando se llama `Notify(default)`
- Entonces `Progress01` sigue en `p` e `IsComplete` no cambia
