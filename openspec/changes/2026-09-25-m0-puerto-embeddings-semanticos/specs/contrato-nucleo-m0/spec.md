# Delta for contrato-nucleo-m0

## MODIFIED Requirements

### Requirement: Version del contrato

`Contract.Version` DEBE ser `4` y DEBE permanecer congelado hasta el proximo cambio de
contrato. Una prueba DEBE atar ese valor al mayor encabezado `## v<N>` de
`Docs/CONTRACT-CHANGELOG.md`. Si ese archivo no existe, la prueba DEBE llamar `Assert.Ignore`,
no fallar. El pin literal de prueba (`ContractTypeTests.Version_del_contrato_es_tres`) DEBE
renombrarse a `Version_del_contrato_es_cuatro` con valor `4`, en el mismo commit: el pin cambia
en cada bump por definicion, no es una excepcion a la regla de aditivo puro (que protege tipos
y puertos, no el pin de version).
(Previously: DEBE ser `1`. Esta capacidad nunca formalizo los bumps a `2` y a `3` que hicieron
`respuesta-clinica-m0` y `requerimientos-juntas-m0` respectivamente; ese historial intermedio
quedo solo en `Docs/CONTRACT-CHANGELOG.md` `## v2`/`## v3`, no en este spec.
`Runtime/Core/Contract.cs` ya esta en `3` hoy; este delta lo sube a `4` y, en este archivo,
salta directo de `1` a `4`.)

#### Scenario: La version publicada es 4

- Dado el ensamblado `NpcAi.Core`
- Cuando se lee `Contract.Version`
- Entonces vale `4`

#### Scenario: La version coincide con el changelog

- Dado que `Docs/CONTRACT-CHANGELOG.md` existe
- Cuando se toma el mayor `## v<N>` del archivo
- Entonces `N` es igual a `Contract.Version`

#### Scenario: Sin changelog la prueba se ignora

- Dado que `Docs/CONTRACT-CHANGELOG.md` no existe
- Cuando corre la prueba que compara version y changelog
- Entonces llama `Assert.Ignore` y no falla

#### Scenario: El pin de version se renombra en el mismo commit

- Dado `ContractTypeTests`
- Cuando se busca el pin literal de version
- Entonces se llama `Version_del_contrato_es_cuatro` y devuelve `4`
