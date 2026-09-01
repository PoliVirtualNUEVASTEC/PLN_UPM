# Especificacion: canales-evento-nucleo-m0

## Purpose

Congela la semantica de `EventChannel<T>` en `NpcAi.Core.Channels` y de los 5 canales
concretos del nucleo. Los `EventChannel` son la unica via de conexion permitida entre modulos
conectados por Inspector (ScriptableObject). Esta capacidad documenta y fija el comportamiento
ya existente (commit `a7a0590`); no lo reescribe.

## ADDED Requirements

### Requirement: Semantica de EventChannel

`EventChannel<T>` DEBE ofrecer `Subscribe(Action<T>)`, `Unsubscribe(Action<T>)` y `Raise(T)`.
`Raise` sin suscriptores NO DEBE lanzar. `Raise` DEBE entregar el mismo payload a cada
suscriptor vigente. `Unsubscribe` DEBE cortar la entrega a ese handler. `Subscribe(null)` y
`Unsubscribe(null)` NO DEBEN lanzar ni alterar la lista de suscriptores. Al deshabilitarse el
asset (`OnDisable`), el canal DEBE limpiar su lista interna de suscriptores, de modo que un
`Raise` posterior NO invoque a ningun handler previo.

#### Scenario: Raise sin suscriptores no lanza

- Dado un canal sin suscriptores
- Cuando se llama `Raise(payload)`
- Entonces no se lanza excepcion

#### Scenario: Raise entrega el payload a cada suscriptor

- Dado dos handlers suscritos al canal
- Cuando se llama `Raise(payload)`
- Entonces ambos handlers reciben exactamente ese `payload`

#### Scenario: Unsubscribe corta la entrega

- Dado un handler suscrito y luego pasado a `Unsubscribe`
- Cuando se llama `Raise(payload)`
- Entonces ese handler no es invocado

#### Scenario: Subscribe y Unsubscribe con null no lanzan

- Dado un canal
- Cuando se llama `Subscribe(null)` y `Unsubscribe(null)`
- Entonces no se lanza excepcion y los suscriptores existentes siguen recibiendo

#### Scenario: OnDisable limpia los suscriptores

- Dado un handler suscrito al canal
- Cuando el asset se deshabilita (`OnDisable`) y luego se llama `Raise(payload)`
- Entonces el handler previo no es invocado

### Requirement: Canales concretos del nucleo

`UtteranceChannel`, `IntentResultChannel`, `PhysicalActionChannel`,
`ReceptivityChangeChannel` y `NpcReplyChannel` DEBEN existir como tipos `sealed` que heredan
de `EventChannel<T>` con `T` igual a `Utterance`, `IntentResult`, `PhysicalAction`,
`ReceptivityChange` y `NpcReply` respectivamente, y DEBEN heredar sin cambios la semantica de
`EventChannel`. Agregar o quitar un canal concreto es un cambio de contrato.

#### Scenario: Los 5 canales existen con su tipo de payload

- Dado el ensamblado `NpcAi.Core.Channels`
- Cuando se inspeccionan los 5 tipos por reflexion
- Entonces cada uno es `sealed`, hereda de `EventChannel<T>` y su `T` es el DTO o enum esperado

#### Scenario: Un canal concreto se comporta como la base

- Dado cualquiera de los 5 canales con un handler suscrito
- Cuando se llama `Raise(payload)` y luego se hace `Unsubscribe` y otro `Raise`
- Entonces el handler recibe el primer `payload` y no el segundo
