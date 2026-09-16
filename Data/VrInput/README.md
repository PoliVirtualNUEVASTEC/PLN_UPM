# M7 — Configuración de entrada física VR

Un asset `VrInputSettingsAsset` por escenario, nunca un singleton global (regla dura 7: lo
variable es dato, no código). `Emergency.asset` es la configuración del escenario de
triaje/emergencia (mismo nombre que ya usan `Data/Speech/` y `Data/Presentation/` para este
escenario); un escenario nuevo se agrega creando otro asset, sin tocar `Runtime/VrInput/`.

## Cómo crear un asset nuevo

`Assets > Create > NpcAi/VrInput/Vr Input Settings` en el proyecto de Unity, o duplicar
`Emergency.asset` y ajustar los campos. La escena del escenario referencia el asset en el campo
`_configuracion` del componente `VrInputBehaviour` (M11 lo cablea).

## Campos

Los tres detectores del núcleo (`SpatialPhysicalActionSource`) solo leen estos seis números —
ninguno vive como constante en código (`design.md`, tabla "Configuración").

| Campo | Tipo | Default | Rango en `OnValidate` | Qué controla |
|---|---|---|---|---|
| `GradosDelConoDeMirada` | `float` | `20` | `Clamp(1, 90)` | Qué tan centrado debe estar el NPC en el campo de visión del usuario para contar como "mirando". Muy bajo (p. ej. 5°) exige apuntar casi exacto con la cabeza y `ContactoVisual` casi nunca se levanta; muy alto (p. ej. 80°) lo levanta con el NPC casi al costado, lo que se siente como un falso positivo. |
| `GradosDeLiberacionDeMirada` | `float` | `30` | `Clamp(1, 120)`, luego `Max(GradosDelConoDeMirada)` | El cono "ancho" de salida (histéresis): hay que salir de este ángulo, no solo del cono de entrada, para que una mirada futura vuelva a contar. Si queda igual al cono de entrada, un temblor de cabeza justo en el borde produce una ráfaga de encendidos/apagados de `ContactoVisual`. |
| `SegundosDePermanenciaDeMirada` | `float` | `0.6` | `Clamp(0, 5)` | Cuánto tiempo sostenido dentro del cono hace falta antes de levantar `ContactoVisual`. En `0` se levanta en el primer tick dentro del cono (cualquier vistazo cuenta); muy alto (p. ej. `3`) hace que encarar al NPC se sienta como que "no responde". |
| `MetrosParaAcercarse` | `float` | `1.2` | `Clamp(0.1, 10)` | Distancia HMD-paciente por debajo de la cual se considera que el usuario se acercó. Muy bajo obliga a estar casi encima del paciente; muy alto levanta `Acercarse` cuando el usuario todavía está lejos, lo que rompe la sensación de "acercarse de verdad". |
| `MetrosParaAlejarse` | `float` | `2.0` | `Clamp(0.2, 20)`, luego `Max(MetrosParaAcercarse + 0.1)` | Distancia por encima de la cual se considera que el usuario se alejó. Junto con `MetrosParaAcercarse` forma la banda de histéresis: si quedara muy cerca de `MetrosParaAcercarse`, pararse justo en el borde produce una ráfaga alternada de `Acercarse`/`Alejarse` en vez de una transición limpia. |
| `SegundosDeEnfriamientoDeContacto` | `float` | `1.0` | `Clamp(0, 10)` | Ventana mínima entre dos emisiones de `TocarPaciente` mientras el contacto sigue activo. En `0` un solo toque sostenido puede inundar el canal con eventos repetidos cada frame; muy alto hace que toques reales y separados se traten como uno solo. |

## Calibración en Quest 3 (compuerta humana 3.6)

Los seis valores de arriba son puntos de partida razonables, **no medidos** (`design.md`, Open
Questions): se calibran contra un paciente a escala real durante la confirmación física en
Quest 3 (tarea `3.6` de `tasks.md`), no adivinando en el editor. Esa compuerta puede terminar
ajustando estos números en vez de solo confirmar pase/falla — el resultado final, y cualquier
ajuste, se registra en `apply-progress.md` del cambio `2026-09-16-m7-entrada-fisica-vr`,
atribuido a quien corrió la prueba. Hasta que esa compuerta se corra, tratar estos defaults como
provisionales.

`VrInputSettingsAsset.OnValidate()` acota estos campos automáticamente al guardarse en el
Inspector; los valores fuera de rango nunca llegan al snapshot `VrInputSettings` que consume el
núcleo.
