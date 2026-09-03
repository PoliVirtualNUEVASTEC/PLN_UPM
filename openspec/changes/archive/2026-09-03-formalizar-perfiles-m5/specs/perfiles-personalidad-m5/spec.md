# Especificacion: perfiles-personalidad-m5

## Purpose

Congela el alcance y las garantias de los datos de personalidad de M5 (`Data/Personalities/`):
que son 4 archivos `.asset` y cero clases, que el esquema lo posee `receptividad-m4`, que cada
perfil respeta los signos del contrato de receptividad y tiene umbrales coherentes, que los 4
`.asset` arman un `ReceptivityProfileCatalog` via `BuildCatalog` sin tocar el motor, y que el
tuning fino por personalidad es dato — su fuente de verdad esta en el design de este cambio, no
en `receptividad-m4`. El esquema (`ReceptivityProfileAsset`, `ReceptivityProfile`,
`ReceptivityProfileCatalog`) y los invariantes del motor los posee `receptividad-m4` y NO se
redefinen aqui. El mecanismo de verificacion son las 8 pruebas de
`Tests/EditMode/Receptivity/PersonalityProfilesDataTests.cs` y las pruebas de M4 que tocan el
adaptador y el catalogo. Documenta y fija el M5 ya existente y en verde; no lo reescribe.

## ADDED Requirements

### Requirement: Alcance — cuatro personalidades, cero clases

M5 DEBE entregar exactamente 4 perfiles de personalidad como archivos `.asset` en
`Data/Personalities/`: `grosero`, `histerico`, `introvertido` y `empatico`. Cada `.asset` DEBE
ser una instancia de `ReceptivityProfileAsset`. Agregar o quitar una personalidad DEBE ser crear
o borrar un `.asset` y NO DEBE tocar ninguna clase de `Runtime/`. El numero de personalidades es
dato de M5 y NUNCA un cambio de contrato de M4 ni de M0. El nombre de archivo de cada `.asset`
DEBE coincidir con su campo `personalityId` (la clave del catalogo).

#### Scenario: Data/Personalities trae exactamente los cuatro perfiles del alcance

- Dado el contenido de `Data/Personalities/`
- Cuando se listan los `ReceptivityProfileAsset` bajo esa carpeta
- Entonces hay exactamente 4 y sus `personalityId` son `grosero`, `histerico`, `introvertido` y `empatico`

#### Scenario: El nombre de archivo coincide con el personalityId

- Dado cualquiera de los 4 `.asset`
- Cuando se compara el nombre del archivo (sin extension) con su campo `personalityId`
- Entonces son iguales

### Requirement: El esquema es propiedad de M4

Cada `.asset` de M5 DEBE ser una instancia del `ReceptivityProfileAsset` que define
`receptividad-m4` (sub-ensamblado `NpcAi.Receptivity.Unity`). M5 NO DEFINE tipos: solo aporta
datos. Los campos poblados por cada `.asset` DEBEN ser `personalityId`, los umbrales
(`umbralReceptivo`, `umbralNoReceptivo`, `limitePuntaje`, `puntajeInicial`) y las 3 tablas de
delta (`porIntencion`, `porTono`, `porAccion`). La proyeccion al tipo puro la hace `ToProfile()`
de M4, sin logica de M5.

#### Scenario: Cada asset se resuelve como ReceptivityProfileAsset y proyecta a un perfil

- Dado cualquiera de los 4 `.asset`
- Cuando se carga como `ReceptivityProfileAsset` y se llama `ToProfile()`
- Entonces devuelve un `ReceptivityProfile` con esos mismos umbrales y deltas

### Requirement: Los datos respetan los signos del contrato de receptividad

Para cada perfil de M5, tras `ToProfile()`: `DeltaPorIntencion(Intent.SolicitudAgresiva)`,
`DeltaPorIntencion(Intent.Interrupcion)` y `DeltaPorTono(Tone.Agresivo)` DEBEN ser `<= 0`;
`DeltaPorIntencion(Intent.SolicitudRespetuosa)`, `DeltaPorIntencion(Intent.Empatia)`,
`DeltaPorTono(Tone.Respetuoso)`, `DeltaPorTono(Tone.Empatico)` y
`DeltaPorAccion(PhysicalAction.GestoCalma)` DEBEN ser `>= 0`. El motor de M4 ya blinda la
monotonia estructuralmente (`receptividad-m4`, `Blindar`); este requisito es una guarda de
**datos** que atrapa un `.asset` mal tuneado antes de que llegue al motor o a quien lea la tabla.

#### Scenario: Cada perfil respeta los signos

- Dado el catalogo armado con los 4 `.asset`
- Cuando se consultan los deltas de agresion / interrupcion / tono agresivo y los de respeto / empatia / gesto de calma en cada perfil
- Entonces los primeros son `<= 0` y los segundos `>= 0` en las 4 personalidades

### Requirement: Umbrales y puntaje inicial coherentes

Para cada perfil de M5: `umbralNoReceptivo` DEBE ser estrictamente menor que `umbralReceptivo`;
`limitePuntaje` DEBE ser `> 0`; `puntajeInicial` DEBE estar dentro de
`[-limitePuntaje, +limitePuntaje]`.

#### Scenario: Cada perfil tiene umbrales y puntaje inicial coherentes

- Dado el catalogo armado con los 4 `.asset`
- Cuando se leen `umbralReceptivo`, `umbralNoReceptivo`, `limitePuntaje` y `puntajeInicial` de cada perfil
- Entonces `umbralNoReceptivo < umbralReceptivo`, `limitePuntaje > 0` y `puntajeInicial` cae en `[-limitePuntaje, +limitePuntaje]`

### Requirement: Los assets arman el catalogo sin tocar el motor

Los 4 `.asset` DEBEN poder ensamblarse en un `ReceptivityProfileCatalog` via
`ReceptivityProfileAsset.BuildCatalog(assets)` (costura definida en `receptividad-m4`). El
catalogo resultante DEBE resolver los 4 ids a un perfil propio — NO `ReceptivityProfile.Default`
— y DEBE caer en `ReceptivityProfile.Default` para un id ausente o para `PersonalityId.None`, sin
lanzar. Pasar ese catalogo a `new ReceptivityEngine(catalogo)` y hacer `Reset` con cada
personalidad NO DEBE lanzar. `ReceptivityEngine` NO DEBE conocer el tipo `ReceptivityProfileAsset`.

#### Scenario: BuildCatalog resuelve los cuatro ids con perfil propio

- Dado los 4 `ReceptivityProfileAsset` de `Data/Personalities/`
- Cuando se llama `ReceptivityProfileAsset.BuildCatalog(assets)`
- Entonces `Personalidades` son exactamente los 4 ids y `PerfilDe(id)` de cada uno no es `ReceptivityProfile.Default`

#### Scenario: Id fuera del catalogo cae en Default

- Dado el catalogo armado con los 4 `.asset`
- Cuando se llama `PerfilDe(new PersonalityId("no-existe"))` y `PerfilDe(PersonalityId.None)`
- Entonces las dos devuelven `ReceptivityProfile.Default` y no lanzan

#### Scenario: Cada perfil entra al motor real sin lanzar

- Dado `new ReceptivityEngine(BuildCatalog(assets))`
- Cuando se llama `Reset(new PersonalityId(id))` con cada una de las 4 personalidades
- Entonces ninguna llamada lanza y `Current` queda en un estado definido (`NoReceptivo`, `Neutral` o `Receptivo`)

### Requirement: Los extremos son distinguibles

Los perfiles `grosero` (extremo hostil) y `empatico` (extremo cooperativo) NO DEBEN producir el
mismo estado inicial en el motor real, y el hostil NO DEBE arrancar mas receptivo que el
cooperativo: `(int)estadoInicial(grosero) <= (int)estadoInicial(empatico)`. Esto ancla la
propiedad que justifica reducir el reparto de 8 a 4 personalidades — cubrir los extremos de los
dos ejes (hostilidad y expresividad).

#### Scenario: grosero y empatico no arrancan en el mismo estado

- Dado `new ReceptivityEngine(BuildCatalog(assets))`
- Cuando se hace `Reset(new PersonalityId("grosero"))` y luego `Reset(new PersonalityId("empatico"))`
- Entonces los dos estados difieren y `(int)Current` de `grosero` es `<=` que el de `empatico`

### Requirement: El tuning por personalidad es dato de M5

Los numeros vigentes de los 4 perfiles (umbrales y las 3 tablas de delta) DEBEN estar
documentados en el design de este cambio (seccion "Tuning de record"). Al momento de su creacion
reproducen 1:1 `ReceptivityProfileCatalog.Standard()`. Ajustar esos numeros DEBE hacerse editando
el `.asset` (y esa tabla), NO DEBE cambiar ninguna clase y NO ES un cambio de contrato de M4.
`receptividad-m4` NO DEBE recibir escenarios con numeros concretos por personalidad: esos viven
en `perfiles-personalidad-m5`.

#### Scenario: Standard sigue siendo el reparto en codigo y coincide con los assets

- Dado `ReceptivityProfileCatalog.Standard()`
- Cuando se listan sus personalidades
- Entonces son exactamente `grosero`, `histerico`, `introvertido` y `empatico`, los mismos 4 ids que los `.asset` de `Data/Personalities/`

## Trazabilidad (requisito -> prueba en verde)

| Requisito | Prueba(s) que ya lo demuestran |
|---|---|
| Alcance — cuatro personalidades, cero clases | `PersonalityProfilesDataTests.Data_Personalities_trae_exactamente_los_cuatro_perfiles_de_M5`, `...Cada_archivo_se_llama_igual_que_su_personalityId` |
| El esquema es propiedad de M4 | `PersonalityProfilesDataTests.BuildCatalog_resuelve_los_cuatro_ids_con_perfil_propio`; `ReceptivityProfileAssetTests.ToProfile_traslada_umbrales_y_puntaje_inicial`, `...ToProfile_traslada_las_tres_tablas_de_delta` (M4) |
| Los datos respetan los signos del contrato | `PersonalityProfilesDataTests.Cada_perfil_respeta_los_signos_del_contrato` |
| Umbrales y puntaje inicial coherentes | `PersonalityProfilesDataTests.Cada_perfil_tiene_umbrales_y_puntaje_inicial_coherentes` |
| Los assets arman el catalogo sin tocar el motor | `PersonalityProfilesDataTests.BuildCatalog_resuelve_los_cuatro_ids_con_perfil_propio`, `...Un_id_fuera_del_catalogo_cae_en_Default`, `...Cada_perfil_de_disco_entra_al_motor_real_sin_lanzar` |
| Los extremos son distinguibles | `PersonalityProfilesDataTests.Los_extremos_hostil_y_cooperativo_no_arrancan_en_el_mismo_estado`; corroborado por `ReceptivityEngineTests.Grosero_arranca_no_receptivo_y_empatico_arranca_neutral` (M4) |
| El tuning por personalidad es dato de M5 | `ReceptivityProfileCatalogTests.Standard_trae_exactamente_las_cuatro_personalidades_de_M5` (M4); tabla "Tuning de record" del design (inspeccion) |
