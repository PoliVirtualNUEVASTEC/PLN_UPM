# Delta for contrato-nucleo-m0

## MODIFIED Requirements

### Requirement: Version del contrato

`Contract.Version` DEBE ser `3` y DEBE permanecer congelado hasta el proximo cambio de
contrato. Una prueba DEBE atar ese valor al mayor encabezado `## v<N>` de
`Docs/CONTRACT-CHANGELOG.md`. Si ese archivo no existe, la prueba DEBE llamar
`Assert.Ignore`, no fallar.
(Previously: DEBE ser `1`. Esta capacidad nunca formalizo el bump a `2` que hizo
`respuesta-clinica-m0`; ese historial intermedio quedo solo en `Docs/CONTRACT-CHANGELOG.md`
`## v2`, no en este spec. Este delta salta directo de `1` a `3`.)

#### Scenario: La version publicada es 3

- Dado el ensamblado `NpcAi.Core`
- Cuando se lee `Contract.Version`
- Entonces vale `3`

#### Scenario: La version coincide con el changelog

- Dado que `Docs/CONTRACT-CHANGELOG.md` existe
- Cuando se toma el mayor `## v<N>` del archivo
- Entonces `N` es igual a `Contract.Version`

#### Scenario: Sin changelog la prueba se ignora

- Dado que `Docs/CONTRACT-CHANGELOG.md` no existe
- Cuando corre la prueba que compara version y changelog
- Entonces llama `Assert.Ignore` y no falla
