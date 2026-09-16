# Especificacion: entrada-fisica-vr-m7

## Purpose

Define los requisitos y escenarios de comportamiento de la primera implementacion real de
`IPhysicalActionSource` (M7, `Runtime/VrInput/`): `SpatialPhysicalActionSource`, un nucleo en C#
puro que detecta 4 de las 7 `PhysicalAction` — `ContactoVisual` (mirada de cabeza sostenida),
`Acercarse`/`Alejarse` (distancia HMD-NPC con histeresis) y `TocarPaciente` (contacto por
collider con enfriamiento) — a partir de `SpatialSample` entregadas por la costura interna
`ISpatialSampler`, sin sesion XR, sin escena y sin headset. `VrInputBehaviour` y `TouchZoneRelay`
son la envoltura `MonoBehaviour` que conecta camara/transform/collider reales de la escena
anfitriona y publica por `PhysicalActionChannel`. `EntregarObjeto`, `SenalarPantalla` y
`GestoCalma` quedan fuera de esta entrega y son su propio cambio SDD de seguimiento;
`IPhysicalActionSource` y `PhysicalAction` (`contrato-nucleo-m0`) no cambian.

## ADDED Requirements

### Requirement: Conformidad con PhysicalActionSourceContract

Toda implementacion real en `Runtime/VrInput/` DEBE heredar de
`NpcAi.Core.Tests.PhysicalActionSourceContract` en `Tests/EditMode/VrInput/` y pasar sin modificar
la clase base. Entregar la accion a quien esta suscrito, no entregar nada tras desuscribirse,
que `Ninguna` nunca sea un evento observable y que emitir sin suscriptores no lance, son garantias
ya fijadas por la base y DEBEN sostenerse tambien en `SpatialPhysicalActionSource`.

#### Scenario: Paso de la suite contractual heredada
- Dado `SpatialPhysicalActionSourceTests : PhysicalActionSourceContract`
- Cuando se ejecuta la bateria heredada en el Test Runner EditMode
- Entonces el 100% de las pruebas contractuales resultan en verde, sin modificar la clase base

### Requirement: Deteccion de `ContactoVisual` por permanencia de mirada de cabeza

`ContactoVisual` DEBE levantarse unicamente cuando el angulo entre el *forward* de la cabeza y la
direccion al NPC este dentro del cono configurado y se sostenga durante al menos la permanencia
minima configurada. NO DEBE levantarse ante un vistazo mas corto que esa permanencia. Mientras la
mirada permanezca continuamente dentro del cono, NO DEBE repetirse: se levanta una sola vez hasta
que el angulo sale del cono (liberacion).

#### Scenario: Mirada sostenida dentro del cono levanta el evento una vez
- Dado una trayectoria sintetica que entra al cono y permanece mas alla de la permanencia minima
- Cuando el nucleo procesa esas muestras en orden
- Entonces `OnAction` recibe `ContactoVisual` exactamente una vez

#### Scenario: Un vistazo fugaz no levanta el evento
- Dado una trayectoria que entra al cono y sale antes de cumplir la permanencia minima
- Cuando el nucleo procesa esas muestras
- Entonces `OnAction` no recibe `ContactoVisual`

#### Scenario: Sostener la mirada no produce repeticiones hasta liberar el cono
- Dado que ya se levanto `ContactoVisual` y la mirada sigue dentro del cono
- Cuando llegan mas muestras sin salir del cono
- Entonces no hay una segunda emision; solo al salir y volver a entrar se levanta otra vez

### Requirement: Histeresis de distancia para `Acercarse`/`Alejarse`

Cruzar el umbral de entrada acercandose DEBE levantar `Acercarse`, y cruzar el umbral de salida
alejandose DEBE levantar `Alejarse`. Ambos umbrales DEBEN ser distintos (banda de histeresis). Una
distancia que oscila alrededor de un unico punto NO DEBE producir una rafaga de eventos alternados.

#### Scenario: Cruzar el umbral hacia el NPC levanta `Acercarse`
- Dado una trayectoria que cruza el umbral de entrada acercandose
- Cuando el nucleo procesa esas muestras
- Entonces `OnAction` recibe `Acercarse` exactamente una vez en ese cruce

#### Scenario: Oscilar sobre un umbral no produce rafaga
- Dado una trayectoria que oscila repetidamente alrededor de un solo punto de distancia
- Cuando ese punto queda dentro de la banda de histeresis (entre ambos umbrales)
- Entonces `OnAction` no recibe ninguna emision adicional por esa oscilacion

### Requirement: Deteccion de contacto para `TocarPaciente` con enfriamiento

Entrar en el pulso de contacto (collider del paciente) DEBE levantar `TocarPaciente`. Un solape
continuo DEBE aplicar un enfriamiento configurable: NO DEBE producir emisiones repetidas mientras
el contacto siga activo dentro de esa ventana.

#### Scenario: Entrar en contacto levanta el evento
- Dado un pulso de contacto que pasa de inactivo a activo
- Cuando el nucleo procesa esa muestra
- Entonces `OnAction` recibe `TocarPaciente`

#### Scenario: El contacto continuo no inunda de eventos
- Dado un contacto activo sostenido por varias muestras dentro de la ventana de enfriamiento
- Cuando el nucleo procesa esas muestras
- Entonces solo hay una emision de `TocarPaciente` hasta que el enfriamiento venza

### Requirement: La configuracion es dato, acotada en OnValidate

Ningun umbral (angulo del cono, permanencia, distancias de acercarse/alejarse, banda de
histeresis, enfriamiento) DEBE existir como constante en codigo. DEBEN leerse de
`VrInputSettingsAsset : ScriptableObject` bajo `Data/VrInput/`. `OnValidate` DEBE acotar cada
rango a valores validos (no negativos donde aplique, umbral de entrada distinto del de salida).

#### Scenario: OnValidate acota los rangos fuera de limite
- Dado un `VrInputSettingsAsset` con angulo, permanencia, distancias o enfriamiento fuera de rango
- Cuando se llama `OnValidate`
- Entonces cada valor queda dentro de su rango valido definido por el asset

### Requirement: Ciclo de vida de `VrInputBehaviour` sin fugas y no-op seguro

`VrInputBehaviour` DEBE suscribirse al evento `OnAction` de su `SpatialPhysicalActionSource`
interno en `OnEnable` y desuscribirse en `OnDisable`, sin fugas entre escenas. `VrInputBehaviour`
NO DEBE suscribirse a `PhysicalActionChannel`: es solo destino de `Raise` (publicar), nunca de
`Subscribe` — suscribirse a su propio canal de salida seria un lazo. Sin `Camera` o `Transform`
objetivo asignados, DEBE ser no-op silencioso: NO DEBE lanzar y NO DEBE usar `Debug.Log`.

#### Scenario: OnEnable suscribe y OnDisable desuscribe sin fugas
- Dado un `VrInputBehaviour` cableado con un `PhysicalActionChannel` como destino de publicacion
- Cuando se levanta una accion antes y despues de `OnDisable`
- Entonces solo la anterior a `OnDisable` llega al canal

#### Scenario: Sin camara u objetivo asignados no lanza
- Dado un `VrInputBehaviour` sin `Camera` ni `Transform` objetivo asignados
- Cuando se bombea una actualizacion
- Entonces no lanza y no invoca `Debug.Log`

## Trazabilidad (requisito -> prueba)

| Requisito | Prueba EditMode |
|---|---|
| Conformidad con PhysicalActionSourceContract | `Entrega_la_accion_a_quien_esta_suscrito`, `No_entrega_nada_despues_de_desuscribirse`, `Ninguna_no_es_un_evento`, `Emitir_sin_suscriptores_no_lanza` (heredadas de la base sin cambios) |
| Deteccion de `ContactoVisual` por permanencia de mirada | `Mirada_sostenida_en_el_cono_levanta_ContactoVisual_una_vez`, `Vistazo_fugaz_no_levanta_ContactoVisual`, `Mirada_continua_no_repite_ContactoVisual_hasta_liberar_el_cono` (propuesto) |
| Histeresis de distancia para Acercarse/Alejarse | `Cruzar_el_umbral_de_entrada_levanta_Acercarse`, `Cruzar_el_umbral_de_salida_levanta_Alejarse`, `Oscilar_sobre_el_umbral_no_produce_una_rafaga_de_eventos` (propuesto) |
| Deteccion de contacto para TocarPaciente con enfriamiento | `Entrar_en_contacto_levanta_TocarPaciente`, `Contacto_continuo_dentro_del_enfriamiento_no_repite_el_evento` (propuesto) |
| La configuracion es dato, acotada en OnValidate | `OnValidate_acota_cono_permanencia_distancias_histeresis_y_enfriamiento` (propuesto) |
| Ciclo de vida de VrInputBehaviour sin fugas y no-op seguro | `OnEnable_suscribe_y_OnDisable_desuscribe_sin_fugas`, `Sin_camara_u_objetivo_asignados_no_lanza` (propuesto) |
