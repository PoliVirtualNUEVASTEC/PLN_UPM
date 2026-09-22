# M10 — Configuración del objetivo de sala de juntas

Un asset `BoardroomObjectiveSettingsAsset` por escenario, nunca un singleton global (regla
dura 7: lo variable es dato, no código). `Boardroom.asset` es la configuración del objetivo de
levantamiento de requerimientos (`RequirementsScenarioObjective`); un escenario nuevo se agrega
creando otro asset, sin tocar `Runtime/Scenarios/Boardroom/`.

Carpeta nueva: hoy `Data/` tiene `Cases`, `Corpus`, `Dialogue`, `Personalities`, `Presentation`,
`Requirements`, `Speech`, `VrInput`. M9 (`Runtime/Scenarios/Emergency/`) sigue con sus pesos
como constantes C# — es deuda registrada de M9, no de este cambio — así que cuando M9 sume su
propio asset podrá vivir en esta misma carpeta sin tocar M10.

## Cómo crear un asset nuevo

`Assets > Create > NpcAi/Escenarios/Objetivo de sala de juntas` en el proyecto de Unity, o
duplicar `Boardroom.asset` y ajustar los campos. El compositor de sala de juntas (M11, fuera de
alcance de este cambio) construye `RequirementsScenarioObjective` con `asset.ToSettings()`.

## Campos

`RequirementsScenarioObjective.Progress01` mezcla tres vías con estos pesos
(`BoardroomObjectiveSettings.Mezclar`, AD7 de `design.md`). Ninguno vive como constante de
tuning en código — las `const` de `BoardroomObjectiveSettings` son solo el default del
constructor sin parámetros (AD13), para que `ScenarioObjectiveContract.CreateSubject()` siga
produciendo un sujeto válido sin un asset cargado.

| Campo | Tipo | Default | Rango válido | Qué controla |
|---|---|---|---|---|
| `pesoDeTrato` | `float` | `0.2` | `[0,1]` | Cuánto del progreso total depende de la vía de trato (`Notify`). El complemento (`1 - pesoDeTrato`) es el peso del levantamiento (cobertura + cierre juntos). |
| `pesoDeCobertura` | `float` | `0.75` | `[0,1]` | Reparto **dentro** del levantamiento: qué fracción de esa mitad va a cobertura del catálogo (`RegisterDisclosure`) frente a cierre fiel (`PresentSummary`). El complemento (`1 - pesoDeCobertura`) es el peso del cierre. |
| `pasosDeTrato` | `int` | `5` | `>= 1` (`[Min(1)]` en el Inspector; el constructor de `BoardroomObjectiveSettings` también lo aplica como red de seguridad) | Granularidad del libro mayor de trato: cada `Worsened` no recuperado cuesta `pesoDeTrato / pasosDeTrato` del progreso total. |

Con los valores reales de `Boardroom.asset` (`pesoDeTrato 0.2`, `pesoDeCobertura 0.75`) la
fórmula de `Mezclar` se aplana a:

    Progress01 = 0.20·trato + 0.60·cobertura + 0.20·cierre

Es decir: cobertura pesa 3 veces más que cierre dentro del levantamiento (0.60 vs. 0.20), y el
trato queda deliberadamente bajo (0.20) porque la habilidad evaluada es levantar requerimientos,
no simplemente mantener contento al cliente.

## Tabla de sanidad

Fila por fila, con `t` = vía de trato, `c` = cobertura, `k` = cierre, todos en `[0,1]`
(`RequirementsProgresoTests` la fija en PR4 con tolerancia `1e-4`):

| Escenario | `t` | `c` | `k` | `Progress01` | `IsComplete` |
|---|---|---|---|---|---|
| Recién construido, sin caso | `0` | — | — | `0.00` | no |
| `AssignCase` y nada más | `1` | `0` | `0` | `0.20` | no |
| Sesión perfecta | `1` | `1` | `1` | `1.00` | sí |
| Cubre todo, no cierra | `1` | `1` | `0` | `0.80` | no |
| Cierre temprano honesto (mitad del catálogo) | `1` | `0.5` | `1` | `0.70` | no |
| Cubre y cierra, maltrató toda la sesión | `0` | `1` | `1` | `0.80` | no |
| Cubre, cierra, un solo `Worsened` sin recuperar | `0.8` | `1` | `1` | `0.96` | no |

La última fila es la penalización mínima observable: **0.04 por cada empeoramiento no
recuperado** (`pesoDeTrato / pasosDeTrato` = `0.2 / 5`).

## Advertencia — no subir `pesoDeTrato` por encima de `0.5`

Subir `pesoDeTrato` por encima de `0.5` **invierte la intención del escenario**: el ejercicio
pasaría a premiar apaciguar al cliente (vía de trato) por encima de levantar sus requerimientos
reales (cobertura + cierre), que es exactamente lo que la decisión 1 de Jefferson descartó al
diseñar M10. Si se necesita que el trato sea más determinante, es una decisión de diseño nueva
— no un ajuste de calibración — y debe pasar por su propio cambio SDD, no por editar este
`.asset` en aislamiento.
