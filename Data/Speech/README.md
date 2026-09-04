# M1 — Configuracion de reconocimiento de voz

Un asset `SpeechSettingsAsset` por escenario, nunca un singleton global (regla dura 7: lo
variable es dato, no codigo). `Boardroom.asset` es la configuracion de la sala de juntas;
un escenario nuevo (p. ej. `Emergency.asset`) se agrega creando otro asset, sin tocar
`Runtime/Speech/`.

## Como crear un asset nuevo

`Assets > Create > NpcAi > Speech > Speech Settings` en el proyecto de Unity, o duplicar
`Boardroom.asset` y ajustar los campos. La escena del escenario referencia el asset
correspondiente en el componente que envuelve `OfflineSpeechToText` (M11).

## Campos

| Campo | Tipo | Default | Rango |
|---|---|---|---|
| `Estrategia` | `TriggerStrategy` | `ActividadDeVoz` | enum: `PulsarParaHablar` \| `ActividadDeVoz` |
| `TasaDeMuestreo` | `int` | 16000 | 8000–48000 |
| `EtiquetaDeIdioma` | `string` | `es-CO` | informativa |
| `ModeloEmpaquetado` | `TextAsset` | `vosk-model-small-es-0.42.bytes` | requerido en runtime real (PR4) |
| `IdDeModelo` | `string` | `vosk-model-small-es-0.42` | carpeta destino + sello del aprovisionamiento |
| `DispositivoDeMicrofono` | `string` | `""` | vacio = dispositivo por defecto del sistema |
| `UmbralDeEnergia` | `float` (RMS) | 0.02 | 0–1 |
| `MsMinimosDeVoz` | `int` | 200 | 0–2000 |
| `MsDeSilencioParaCortar` | `int` | 700 | 100–3000 |
| `MsDePreRoll` | `int` | 300 | 0–1000 |
| `MaxSegundosPorFrase` | `float` | 15 | 1–60 |
| `MsMaximosDeCierre` | `int` | 250 | 0–2000 |

**Sin campo de confianza minima.** El requisito "Paso de confianza sin filtrar" (Decision 6
de design.md) prohibe cualquier umbral que condicione la emision de un `Utterance` a su
`Confidence`: M1 emite todo, y es M2 quien decide que hacer con una transcripcion de baja
confianza.

`SpeechSettingsAsset.OnValidate()` acota estos campos automaticamente al guardarse en el
Inspector; los valores fuera de rango nunca llegan al snapshot `SpeechSettings` que consume
el nucleo.
