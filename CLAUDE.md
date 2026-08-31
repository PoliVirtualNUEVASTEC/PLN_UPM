# Instrucciones para el agente en este repositorio

Este es el paquete UPM `com.poli.npc-ai`. Antes de escribir una sola linea, lee `Runtime/Core/`.

## Reglas duras — se aplican a cada cambio, sin excepcion

1. **Un cambio toca exactamente un modulo.** Si el trabajo pide tocar dos carpetas de `Runtime/`, esta mal cortado: para y avisa.
2. **`Runtime/Core/` y `Runtime/CoreChannels/` son de solo lectura** salvo que el cambio sea explicitamente un cambio de contrato (M0). Un cambio de contrato es su propio cambio SDD, se integra solo los lunes y requiere aprobacion de todos los duenos de modulo.
3. **Cada modulo referencia `NpcAi.Core`.** Los que se conectan por Inspector referencian ademas `NpcAi.Core.Channels`. Ninguna otra referencia entre modulos: si hace falta, es un cambio de contrato, no una tarea de programacion.
4. **Todo modulo publica su doble** en `Runtime/<Modulo>/Fakes/`, determinista, y ese doble hereda de la misma clase de prueba de contrato que la implementacion real.
5. **`NpcAi.Core` no puede referenciar `UnityEngine`** (`noEngineReferences: true`). Si necesitas un tipo de Unity en el contrato, la respuesta es no.
6. **`NpcAi.Receptivity` (M4) no usa `UnityEngine`** en su logica. Es C# puro para poder probarlo en milisegundos y demostrar su tabla de transiciones.
7. **Lo variable es dato, no codigo.** Personalidades, corpus, respuestas base y umbrales son ScriptableObjects o JSON en `Data/`. Agregar una personalidad no debe cambiar ni una clase.

## Que NO hacer

- No abrir un cambio SDD para trabajo que se entiende en 1-3 archivos (p. ej. agregar frases al corpus).
- No meter dos modulos en un mismo cambio SDD.
- No usar `/sdd-ff` en los modulos de IA (M1, M2, M4, M6).
- No tratar la narracion del agente como evidencia: valen las pruebas que corren, los receipts y los archivos OpenSpec.

## Convenciones

- Namespaces: `NpcAi.<Modulo>`; dobles en `NpcAi.<Modulo>.Fakes`.
- Pruebas en `Tests/EditMode/<Modulo>/`, namespace `NpcAi.<Modulo>.Tests`.
- Nada de `Debug.Log` en codigo de runtime de un modulo: los puertos no imprimen, devuelven.
