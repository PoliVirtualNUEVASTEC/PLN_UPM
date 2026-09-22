# Especificacion: catalogo-requerimientos-m16

## Purpose

Congela el esquema y los invariantes de `Data/Requirements/*.json`, el catalogo de casos de
sala de juntas que consume `RequirementCaseLoader` (capacidad `respondedor-requerimientos-m16`).
Un caso == un archivo, transcrito a partir de los 4 dominios narrados en
`Data/Corpus/juntas.json` (torneo de futbol, tienda, colegio, aerolinea; ese corpus
permanece de solo lectura, fuente narrativa, no catalogo). Lo variable (tabla de
requerimientos, umbrales de receptividad, banco de desvios) es dato, nunca constante C#.

## ADDED Requirements

### Requirement: Esquema minimo por requerimiento

Cada entrada de la tabla de requerimientos de un caso DEBE declarar, como minimo: `id`
(unico dentro del caso), al menos un `ejemploDePregunta` no vacio, `respuesta` no vacia, y
`receptividadMinima` (uno de `NoReceptivo`, `Neutral`, `Receptivo`).

#### Scenario: Un requerimiento sin campos minimos falla la validacion de esquema

- Dado un requerimiento del catalogo sin `respuesta` o sin ningun `ejemploDePregunta`
- Cuando `RequirementCaseLoader` valida el caso
- Entonces la carga se rechaza (`TryParse` devuelve `false` o equivalente)

### Requirement: Un caso es un archivo, y su `id` declarado coincide con el nombre de archivo

Cada `Data/Requirements/caso-juntas-0N.json` DEBE declarar un campo `id` cuyo valor sea
identico al nombre de archivo sin extension. `RequirementCaseId` se resuelve por ese `id`.

#### Scenario: El id declarado coincide con el nombre de archivo

- Dado cada `Data/Requirements/caso-juntas-0N.json` real
- Cuando se compara su campo `id` contra el nombre de archivo
- Entonces son identicos

### Requirement: Cobertura obligatoria de los 3 niveles de receptividad por caso

Cada caso DEBE declarar al menos un requerimiento con `receptividadMinima == NoReceptivo`,
al menos uno con `Neutral`, y al menos uno con `Receptivo` — analogo al minimo de
`ClinicalCasesDataTests` de M14, para que la progresion sea observable y no un todo-o-nada
disfrazado.

#### Scenario: Cada caso real cubre los 3 niveles

- Dado cada uno de los `Data/Requirements/caso-juntas-0N.json` reales
- Cuando se inspecciona la `receptividadMinima` de sus requerimientos
- Entonces hay al menos uno de cada nivel (`NoReceptivo`, `Neutral`, `Receptivo`)

### Requirement: Los 4 dominios narrativos se transcriben como 4 casos completos

`Data/Requirements/` DEBE contener 4 archivos de caso versionados que cubran los 4 dominios
narrados en `Data/Corpus/juntas.json` (futbol, tienda, colegio, aerolinea), cada uno
satisfaciendo por si solo el esquema minimo y la cobertura de receptividad de este primer
corte.

#### Scenario: Los 4 casos existen y validan de forma independiente

- Dado `Data/Requirements/`
- Cuando se listan y validan sus archivos de caso
- Entonces hay exactamente 4, uno por dominio narrativo, y cada uno pasa
  `RequirementCasesDataTests` sin depender de los otros 3

### Requirement: El banco de desvios es generico por personalidad, no un campo por requerimiento

El texto de `AunNoRevelado` NO DEBE declararse como campo del requerimiento; DEBE salir de
un banco generico indexado por `PersonalityId`, con al menos una entrada por cada
personalidad de `perfiles-personalidad-m5`. Ninguna entrada del banco DEBE contener, como
subcadena, la `respuesta` de ningun requerimiento del catalogo.

#### Scenario: El desvio nunca revela el hecho protegido

- Dado el banco de desvios por personalidad y la tabla de requerimientos de un caso real
- Cuando se compara cada entrada del banco contra cada `respuesta` de ese caso
- Entonces ninguna entrada del banco contiene esa `respuesta` como subcadena

## Trazabilidad (requisito -> prueba)

| Requisito | Prueba(s) |
|---|---|
| Esquema minimo por requerimiento | `RequirementCasesDataTests.Un_requerimiento_sin_campos_minimos_no_valida` |
| Un caso es un archivo, `id` == nombre de archivo | `RequirementCasesDataTests.El_id_declarado_coincide_con_el_nombre_de_archivo` |
| Cobertura obligatoria de los 3 niveles de receptividad | `RequirementCasesDataTests.Cada_caso_cubre_los_3_niveles_de_receptividad_con_al_menos_uno_cada_uno` |
| Los 4 dominios narrativos se transcriben como 4 casos completos | `RequirementCasesDataTests.Existen_4_casos_uno_por_dominio_y_cada_uno_valida_solo` |
| El banco de desvios es generico por personalidad | `RequirementCasesDataTests.Ninguna_entrada_del_banco_de_desvios_filtra_una_respuesta` |

## Nota abierta para `sdd-design`

El minimo exacto de `ejemplosDePregunta` por requerimiento (M14 fijo "minimo 2" para
`Hecho`) y el minimo total de requerimientos por caso mas alla de "al menos 1 por nivel de
receptividad" no fueron confirmados por el usuario con un numero especifico; este spec solo
exige "al menos 1" como piso funcional. Tampoco se confirmo si el banco de desvios vive como
JSON en `Data/Requirements/` o como tabla fija en codigo del modulo (la regla 7 sugiere
dato, pero el archivo/formato exacto no esta decidido). `sdd-design` DEBE resolver ambos
puntos sin reabrir `proposal.md`.
