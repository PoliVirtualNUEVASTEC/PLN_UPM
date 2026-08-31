# M5 — Perfiles de personalidad

**Alcance actual: 4 personalidades.** Un archivo de datos por personalidad, cero clases nuevas.

| Id (`PersonalityId.Value`) | Personalidad | Por que esta en el alcance |
|---|---|---|
| `grosero`      | Grosero      | El extremo hostil: castiga fuerte la agresion y la torpeza. Es el caso dificil que justifica el motor. |
| `histerico`    | Histerico    | Alta reactividad emocional: exige gestos de calma. Es la personalidad que hace relevante la entrada fisica (M7) en el escenario de emergencia. |
| `introvertido` | Introvertido | Baja iniciativa: obliga al usuario a sostener la conversacion. Es el caso que hace relevante el escenario de sala de juntas. |
| `empatico`     | Empatico     | El extremo cooperativo. Sirve de linea base contra la cual se miden las otras tres. |

Las cuatro cubren los extremos del motor de receptividad en dos ejes —hostilidad y expresividad—,
asi que la demostracion del Objetivo 2 no pierde fuerza por reducir de 8 a 4.

## Ampliacion futura

`parlanchin`, `extrovertido`, `tranquilo`, `pesimista` quedan fuera del alcance actual.
Agregarlas es **crear cuatro archivos de datos**: no toca `NpcAi.Core`, ni el motor de M4, ni ningun otro modulo.
Esa es exactamente la propiedad que `PersonalityId` como `readonly struct` (en vez de enum) esta protegiendo.

> Cambio de alcance registrado el 30 de agosto de 2026. Pendiente de validar con el asesor y de reflejar en el FTG.
