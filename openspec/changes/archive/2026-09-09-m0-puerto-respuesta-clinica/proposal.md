# Propuesta: M0 — Puerto de respuesta clínica (`IClinicalResponder`)

## Intent

El bucle de la simulación de triaje necesita que el NPC responda **como el paciente del
caso clínico asignado**: cuando la enfermera pregunta "¿desde cuándo le duele la cabeza?",
el NPC debe decir "hace 2 meses"; ante "¿es alérgica a algo?", "al Tramadol". Hoy ningún
puerto de `NpcAi.Core` transporta un solo hecho del caso. `IDialogueGenerator.Generate`
(M6) recibe `(PersonalityId, Receptivity, IntentResult)` y nada más — es incapaz, por
contrato, de anclar una respuesta a datos de un caso.

El equipo decidió (2026-09-09, ver `Docs/SIMULACION-TRIAJE-CAPA-NUEVA.md`):

1. El contenido del caso llega al NPC por un **módulo nuevo dedicado** (M15, respondedor
   clínico), no creciendo el contrato de M6 ni por un canal de estado ambiente.
2. **Patrón enrutador**: un paso separa el turno *clínico* del turno *social*. Clínico →
   M15 produce el `NpcReply` completo con el hecho, modulado por personalidad/receptividad.
   Social → M6 sin cambios.
3. La generación estocástica de M6 (Markov) **nunca toca un hecho clínico**.

Para que M15 exista como módulo de primera clase, el resto del sistema tiene que verlo a
través de un puerto de `NpcAi.Core`. Este cambio agrega ese puerto. Es el **primer cambio
de contrato** del proyecto: sube `Contract.Version` de `1` a `2` y estrena la sección
`## v2` de `Docs/CONTRACT-CHANGELOG.md`.

## Scope

### In Scope

- Nuevo puerto `IClinicalResponder` en `Runtime/Core/Ports.cs`.
- Nuevo DTO `ClinicalResponse` (struct inmutable) en `Runtime/Core/Dtos.cs`: `Handled`
  (bool — `false` significa "turno no clínico, enrutar a M6") + `Reply` (`NpcReply`, válido
  solo si `Handled`), con un estático `ClinicalResponse.NoAplica`.
- Nuevo `readonly struct ClinicalCaseId` en `Runtime/Core/ClinicalCaseId.cs`, **espejo
  exacto de `PersonalityId`**: identificador estable sobre `string`, con igualdad completa
  (`IEquatable`, `==`, `!=`, `GetHashCode` `Ordinal`), `None = default`, `IsNone`. El
  número de casos clínicos es dato de M14, **nunca** un cambio de contrato.
- Subir `Contract.Version` a `2` en `Runtime/Core/Contract.cs`.
- Agregar `## v2 — 2026-09-09 — Puerto de respuesta clínica (M15)` a
  `Docs/CONTRACT-CHANGELOG.md`, con: los tipos nuevos, los invariantes del puerto en
  formato DEBE/NO DEBE, y la nueva asimetría de determinismo (M15 SÍ es determinista).
- Nueva clase base de prueba de contrato
  `Tests/EditMode/Core/ClinicalResponderContract.cs` (métodos `[Test]` no abstractos, para
  que el doble de M15 y su implementación real hereden y pasen las mismas pruebas).
- Cobertura aditiva en `Tests/EditMode/Core/ContractTypeTests.cs` para `ClinicalCaseId`
  (igualdad, `None`, `IsNone`) y `ClinicalResponse` (`NoAplica` es `Handled == false`).
- Corregir la línea de encabezado obsoleta de `Docs/CONTRACT-CHANGELOG.md` ("los cambios
  de contrato se integran **solo los lunes**... con aprobacion de todos los duenos") para
  que coincida con la regla vigente (`openspec/config.yaml` y el `CLAUDE.md` del repo: sin
  ventana fija, co-revisión del otro dueño de M0 o el asesor).

### Out of Scope

- **Implementación de M15.** Este cambio agrega el puerto y su base de prueba; el
  respondedor real y su doble son el cambio `2026-09-09-m15-respondedor-clinico`.
- **El esquema del caso clínico.** `IClinicalResponder` NO recibe un DTO con el contenido
  del caso: recibe un `ClinicalCaseId` (string). El contenido (síntomas, antecedentes,
  signos vitales, tabla de hechos) es dato de M14 y lo carga M15 internamente. `NpcAi.Core`
  nunca conoce la forma del JSON del caso — así el esquema de M14 puede evolucionar sin
  tocar el contrato.
- **El enum `Triage` (I–V) y la decisión de triaje.** Son del cambio
  `2026-09-09-m9-decision-triaje`; ese cambio decidirá si el enum vive en M0 (si hay puerto
  nuevo de decisión) o en M9. Este cambio NO lo agrega.
- **`IntentResult` / `Intent` / `Tone` no cambian.** El tipo de pregunta clínica ("¿desde
  cuándo?", "¿alergias?") NO se agrega como valor de `Intent`: M15 lo resuelve por su
  cuenta contra la tabla de hechos del caso.
- **El enrutado en sí** (quién llama a M15 antes que a M6). Se decide en
  `2026-09-09-m11-armado-sesion`. Este cambio solo garantiza que la señal de enrutado
  existe (`ClinicalResponse.Handled`).
- **M6 no cambia una línea.** `IDialogueGenerator` queda idéntico.

## Capabilities

### New Capabilities

- `respuesta-clinica-m0` (nuevo, `openspec/specs/respuesta-clinica-m0/spec.md`, se crea al
  archivar este cambio): formaliza el puerto `IClinicalResponder`, los DTO
  `ClinicalCaseId` y `ClinicalResponse`, y sus invariantes DEBE/NO DEBE, con trazabilidad a
  `ClinicalResponderContract` y `ContractTypeTests`.

### Modified Capabilities

- `contrato-nucleo-m0`: pasa de 7 puertos a 8 y de 5 DTO a 7 (`+ClinicalCaseId`,
  `+ClinicalResponse`). `Contract.Version` `1 → 2`. Los 7 puertos y 5 DTO previos y los 4
  enums quedan **byte-idénticos**: este cambio es puramente aditivo.

## Approach

**Aditivo, nunca destructivo.** El contrato v1 congeló nombre, valor, orden y cardinalidad
de los 4 enums y la forma de los 5 DTO y 7 puertos. Este cambio no toca ninguno: agrega un
puerto, dos DTO y un identificador. `ContractTypeTests` sigue en verde sin edición (solo se
le suman casos nuevos para los tipos nuevos).

**`ClinicalCaseId` como `readonly struct` sobre `string`, no enum** — decisión idéntica a
la de `PersonalityId` en v1 (ver `CONTRACT-CHANGELOG.md` → decisión 1). Pasar de 3 a 20
casos clínicos agrega archivos JSON en `Data/Cases/` (M14) y no toca `NpcAi.Core`.

**El caso viaja como id, no como objeto.** `IClinicalResponder.AssignCase(ClinicalCaseId,
PersonalityId)` vincula caso y personalidad al respondedor — mismo patrón que
`IReceptivityEngine.Reset(PersonalityId)`. La respuesta se pide con
`Respond(Utterance, IntentResult)`. M15 resuelve el `ClinicalCaseId` contra `Data/Cases/`
por su cuenta. Así `NpcAi.Core` no gana un DTO grande ni conoce el esquema de M14.

**La señal de enrutado es un `bool` en el DTO de salida.** `ClinicalResponse.Handled`:
`true` → M15 respondió con un hecho, el `NpcReply` va tal cual a M8; `false` → el turno no
es clínico, el llamador enruta a M6. El enrutador (M11) queda trivial: "pregunta a M15;
si `!Handled`, pregunta a M6". No hace falta un clasificador de "clínico vs. social"
aparte.

**M15 SÍ es determinista** — asimetría nueva y deliberada frente a M6. `IDialogueGenerator.
Generate` PUEDE variar entre llamadas (Markov); `IClinicalResponder.Respond` DEBE ser
determinista en `Handled` y en `Reply.Text` para la misma `(caso, personalidad, utterance,
intent)`. Un hecho clínico no puede cambiar de redacción entre turnos: el respondedor usa
plantillas, no generación estocástica. `CONTRACT-CHANGELOG.md` v2 registra esta asimetría
junto a la de v1.

## Affected Areas

| Área | Impacto | Descripción |
|---|---|---|
| `Runtime/Core/Ports.cs` | Modificado (aditivo) | `+ interface IClinicalResponder` |
| `Runtime/Core/Dtos.cs` | Modificado (aditivo) | `+ struct ClinicalResponse` |
| `Runtime/Core/ClinicalCaseId.cs` | Nuevo | `readonly struct ClinicalCaseId` (espejo de `PersonalityId`) |
| `Runtime/Core/Contract.cs` | Modificado | `Version` `1 → 2` |
| `Docs/CONTRACT-CHANGELOG.md` | Modificado | `+ ## v2`; corrección de la regla de integración obsoleta |
| `Tests/EditMode/Core/ClinicalResponderContract.cs` | Nuevo | Base de prueba de contrato del puerto |
| `Tests/EditMode/Core/ContractTypeTests.cs` | Modificado (aditivo) | Casos nuevos para `ClinicalCaseId` y `ClinicalResponse` |
| `Runtime/Core/` (enums, 7 puertos, 5 DTO previos) | Sin cambio | Byte-idénticos |
| `Runtime/CoreChannels/` | Sin cambio | Este cambio no agrega un `EventChannel` (ver Open Questions) |
| M1–M13 existentes | Sin cambio | Ningún módulo referencia `IClinicalResponder` todavía |

## Risks

| Riesgo | Prob. | Mitigación |
|---|---|---|
| `ContractVersionChangelogTests` falla si se sube `Contract.Version` sin agregar `## v2` (o viceversa) | Alta si se hace en pasos sueltos | El bump de versión y la entrada del changelog van en **el mismo commit** (tarea 1.4). La prueba existe justamente para atrapar esto |
| Este es el primer cambio de contrato: el equipo no tiene rodada la co-revisión de M0 | Media | Regla 3 del repo y `openspec/config.yaml`: cambio SDD propio + revisión antes del merge por el otro dueño compartido de M0 (Jefferson) o el asesor. Sin ventana fija. La checklist "Antes de mergear" del `README.md` ya tiene el punto 7 para esto |
| El puerto se diseña sin la implementación de M15 delante y luego no calza | Media | `ClinicalResponderContract` (base de prueba) se escribe en este cambio y se ejerce con un stub mínimo local (igual que `ClasificadorNoListo` en `IntentClassifierContract`), así el puerto se valida como usable antes de mergear. Si M15 descubre que la firma no sirve, corregirla es otro cambio de contrato (v3), no una edición silenciosa |
| Ampliar `NpcAi.Core` invita a meter también `Triage`, la decisión de triaje, el enrutador… | Media | Out of Scope explícito y `Success Criteria` que acota el diff a 3 archivos de `Runtime/Core/` + changelog + 2 de test |
| `Contract.Version` es `const`: un consumidor no recompilado reporta `1` | Baja (conocida, aceptada en v1) | Sin cambio de política aquí; se hereda el riesgo aceptado de v1. Todos los módulos se recompilan al actualizar el paquete por tag |

## Rollback Plan

`IClinicalResponder`, `ClinicalResponse` y `ClinicalCaseId` son adiciones puras. Revertir
los commits de este cambio:

- Baja `Contract.Version` a `1` y quita la sección `## v2` del changelog —
  `ContractVersionChangelogTests` vuelve a verde con `Version == 1`.
- Quita el puerto, los DTO y la base de prueba.
- Ningún módulo depende de `IClinicalResponder` por nombre todavía (M15 y M11 no están
  mergeados), así que nada más se rompe. No hay migración de datos.

## Dependencies

- `contrato-nucleo-m0`, archivado y estable: este cambio lo extiende, es el único
  autorizado a tocar `Runtime/Core/`.
- Ninguna dependencia de código nuevo. Los cambios de M14 y M15 **dependen de este**, no al
  revés.

## Success Criteria

- [ ] `Contract.Version == 2` y `Docs/CONTRACT-CHANGELOG.md` tiene `## v2`;
      `ContractVersionChangelogTests` en verde.
- [ ] `CoreAssemblyPurityTests` sigue en verde: `IClinicalResponder`, `ClinicalResponse` y
      `ClinicalCaseId` son C# puro, sin `UnityEngine` ni `NpcAi.*` ajeno.
- [ ] `ContractTypeTests` en verde, con los casos nuevos para los dos tipos nuevos, y sin
      haber modificado ningún caso existente de enums/DTO v1.
- [ ] `ClinicalResponderContract` compila y pasa contra un stub mínimo local; queda lista
      para que M15 (real y doble) la herede.
- [ ] El diff no toca nada fuera de `Runtime/Core/`, `Docs/CONTRACT-CHANGELOG.md`,
      `Tests/EditMode/Core/` y `openspec/`.
- [ ] Revisado antes del merge por el otro dueño compartido de M0 o el asesor (regla 3).

## Decisiones del usuario (confirmadas 2026-09-09)

1. El contenido del caso llega por un **módulo nuevo dedicado** (M15), no creciendo M6 ni
   por canal de estado ambiente.
2. **Patrón enrutador**: M15 arma el `NpcReply` clínico completo; M6 queda intacto para el
   turno social; Markov nunca toca hechos.
3. **M15 asignado a Luis**; es un módulo nuevo de primera clase.
