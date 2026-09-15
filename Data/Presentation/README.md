# M8 — Configuración de presentación

Un asset `PresentationSettingsAsset` por escenario, nunca un singleton global (regla dura 7:
lo variable es dato, no código). `Emergency.asset` es la configuración del escenario de
triaje/emergencia; un escenario nuevo se agrega creando otro asset, sin tocar
`Runtime/Presentation/`.

## Cómo crear un asset nuevo

`Assets > Create > NpcAi > Presentation > Presentation Settings` en el proyecto de Unity, o
duplicar `Emergency.asset` y ajustar los campos. La escena del escenario referencia el asset
en el campo `_configuracion` del componente `NpcPresenterBehaviour` (M11 lo cablea).

## Campos

| Campo | Tipo | Default | Nota |
|---|---|---|---|
| `TasaDeMuestreo` | `int` | 22050 | Hz del PCM de la síntesis. Piper suele emitir a 22050. `OnValidate` acota a 8000–48000 |
| `Velocidad` | `float` | 1 | Multiplicador de velocidad de habla. `OnValidate` acota a 0.5–2 |
| `VozPorDefecto` | `string` | `""` | `vozId` a usar cuando el NPC no trae uno o no está en `Voces` |
| `Voces` | `EntradaDeVoz[]` | vacío | `vozId` (de `Data/Npcs/*.json`, M11) → `idDeVoz` de la voz Piper |
| `Cues` | `EntradaDeCue[]` | vacío | `EmotionTag` o `AnimationCue` de un `NpcReply` → parámetro del `Animator` del rig anfitrión (`Trigger` o `Bool`) |
| `VozEmpaquetada` | `TextAsset` | — | Voz Piper comprimida (`.bytes`). Se asigna en el **PR3** |
| `IdDeVoz` | `string` | `""` | Carpeta de aprovisionamiento de la voz. Se usa en el **PR3** |

## Frontera con `Data/Npcs/` (M11)

`Data/Npcs/*.json` (que **crea M11**) trae la identidad mínima de cada NPC, incluido su
`vozId`. M8 no crea esos archivos: solo mapea el `vozId` a una voz Piper vía `Voces`. Si un
NPC llega sin `vozId` o con uno que no está en el mapa, M8 usa `VozPorDefecto`.

## Frontera con el proyecto anfitrión

El rig, el avatar, la escena y el `AnimatorController` son del **proyecto VR anfitrión**. El
mapa `Cues` traduce los cues del `NpcReply` a los nombres de parámetros que ese controller
expone; cambiar de rig es editar este mapa, no `Runtime/Presentation/`.
