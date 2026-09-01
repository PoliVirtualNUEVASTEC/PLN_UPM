# Propuesta: Formalizar el contrato v1 del Nucleo M0 (Runtime/Core + Runtime/CoreChannels)

## Intent

El contrato M0 ya existe y es coherente (commit `a7a0590`): 7 puertos, 5 DTO, 4 enums, `Contract.Version = 1`, 10 asmdefs y 8 archivos de prueba de contrato. Lo que falta es su especificacion versionada y la evidencia ejecutable de 15 comportamientos que hoy solo estan documentados, implicitos o sin probar. Mientras eso siga asi, cada modulo M1-M10 interpreta el contrato leyendo codigo y sus dobles pueden divergir sin que ninguna prueba falle. Se formaliza ahora, antes de que los modulos implementen sobre supuestos distintos. Esta propuesta **no reescribe** el contrato: lo documenta y lo fija.

## Scope

### In Scope

- Especificacion OpenSpec versionada del contrato v1 (spec, design, tasks).
- Cierre de las 15 brechas G1-G15 entre lo afirmado y lo que las pruebas exigen.
- **Frontera dura de escritura (4 rutas, ninguna mas):** `Runtime/Core/`, `Runtime/CoreChannels/`, `Tests/EditMode/Core/`, `Docs/CONTRACT-CHANGELOG.md`.
- `Contract.Version` permanece en **1** (congelado).

### Out of Scope

- Cualquier otro modulo; ningun doble en `Runtime/*/Fakes/`.
- Nuevos miembros `abstract` en las clases base de prueba de contrato (obligarian a cambiar todos los dobles y violan "un cambio = un modulo").
- `IEquatable`/`GetHashCode` en los DTO (G12): candidato a v2.
- Clamp de rangos float (G14): son contrato del productor, no invariantes del struct.
- Cambiar `Contract.Version` de `const` a `static readonly`.
- `noEngineReferences: false` en `NpcAi.Receptivity.asmdef`: cambio aparte.

## Capabilities

### New Capabilities

- `contrato-nucleo-m0`: invariantes de los 7 puertos, 5 DTO, 4 enums y la version del contrato de `NpcAi.Core`.
- `canales-evento-nucleo-m0`: semantica de `EventChannel` en `NpcAi.Core.Channels` (suscripcion, guardas nulas, `OnDisable`).

### Modified Capabilities

- None (`openspec/specs/` esta vacio).

## Approach

Enfoque A, "documentar y fijar lo que existe":

- **Runtime (minimo):** solo 2 guardas nulas (`NpcReply.Text ?? ""`, `ReceptivityChange.ReasonCode ?? ""`) mas notas XML-doc que no introducen comportamiento nuevo.
- **Pruebas:** toda la cobertura nueva entra como metodos `[Test]` **no abstractos** en las clases base existentes, o como archivos nuevos bajo `Tests/EditMode/Core/`.
- **Docs:** reescribir la seccion v1 del changelog con invariantes por puerto.

| Grupo | Brechas | Contenido |
|---|---|---|
| A. Meta del contrato | G1, G2, G15 | Prueba `Contract.Version` == mayor `## v<N>` del changelog (`Assert.Ignore` si falta el archivo); prueba de reflexion que fija la pureza de `NpcAi.Core.asmdef` (cero `UnityEngine`/`UnityEditor`, cero `NpcAi.*` ajeno); reescritura de la seccion v1 con invariantes por puerto, `Score`/`ReasonCode` como diagnostico no tipado, asimetria de determinismo, reglas centinela, politica de igualdad de DTO, nota sobre `const` y puntero de enforcement a `Tests/EditMode/Core/`. |
| B. Canales | G3 | Agregar la referencia `NpcAi.Core.Channels` en `NpcAi.Core.Tests.asmdef` y crear `EventChannelTests.cs` (camino feliz, guardas nulas, desuscripcion deja de entregar, `OnDisable` limpia listeners). |
| C. Tipos y DTO | G4, G5, G6, G12, G13, G14 | Una prueba por enum comparando `GetValues`/`GetNames` contra la lista esperada de (nombre, valor) y su conteo, con `Receptivity.NoReceptivo == -1` y `Receptivo == 1`; guardas nulas en `NpcReply.Text` y `ReceptivityChange.ReasonCode` con sus aserciones; 2 aserciones mas para que `IntentResult.Unknown(latencyMs)` preserve la latencia; G12 y G14 solo se documentan. |
| D. Semantica de puertos | G7, G8, G9, G10, G11 | Notas XML-doc y `[Test]` no abstractos: `Current` es `Neutral` hasta `Reset`; `Generate` (Markov) no es determinista frente a `Classify` que si lo es; `Classify` NO DEBE lanzar en ningun estado y DEBERIA devolver `IntentResult.Unknown` cuando `IsReady == false` (con un stub no-listo definido dentro de `Tests/EditMode/Core/`, nunca un doble de modulo); eventos finos (nada se emite antes del primer `StartListening`, `StartListening` doble, Start/Stop/Start reanuda, fan-out multi-suscriptor, STT sin suscriptores no lanza); `IsComplete` si y solo si `Progress01 == 1`, y la completitud es **reversible** (el progreso PUEDE bajar y `IsComplete` volver a `false`); `Notify(default)` como no-op. |

## Affected Areas

| Area | Impacto | Descripcion |
|---|---|---|
| `Runtime/Core/Dtos.cs` | Modificado | 2 guardas nulas (G5, G6). |
| `Runtime/Core/Ports.cs` | Modificado | Solo XML-doc (G7, G8, G9, G11). |
| `Runtime/Core/Contract.cs` | Sin cambio | `Version` sigue en 1. |
| `Runtime/CoreChannels/` | Sin cambio | Solo queda cubierto por pruebas. |
| `Tests/EditMode/Core/` | Nuevo + Modificado | Referencia asmdef y pruebas de G1-G4, G7, G9-G11, G13. |
| `Docs/CONTRACT-CHANGELOG.md` | Modificado | Reescritura de la seccion v1 (G15). |

## Gobernanza

Este es un **cambio de contrato** (CLAUDE.md regla 2): se integra **solo en la ventana del lunes** y requiere co-aprobacion de todos los duenos de modulo. `Contract.Version` **se queda en 1**: las 2 guardas nulas no son ruptura porque ningun consumidor correcto podia depender de recibir `null`, e `IsEmpty` ya trataba `null` como vacio. El resto del cambio es documentacion y pruebas.

## Risks

| Riesgo | Prob. | Mitigacion |
|---|---|---|
| Tocar `Runtime/Core` activa la ventana del lunes y la co-aprobacion | Alta | Declarado arriba; `sdd-tasks` agenda la integracion. |
| Unity no corre en fases de agente; el verde de pruebas es compuerta humana | Alta | Aceptacion por Test Runner o CLI `-runTests`; no hay CI en el repo. |
| Colision `enum NpcAi.Core.Receptivity` vs namespace `NpcAi.Receptivity` | Media | Calificar `Core.Receptivity` en toda prueba nueva. |
| G1 depende de `Path.GetFullPath("Packages/com.poli.npc-ai/Docs/CONTRACT-CHANGELOG.md")` | Media | `Assert.Ignore` si el archivo no existe. |
| Volumen supera el presupuesto de 400 lineas de revision | Alta | Aceptado `size:exception`: un unico PR, revision pesada asumida por el usuario (2026-08-31). |
| Presion por agregar miembros `abstract` a las bases | Media | Prohibido por Out of Scope; solo `[Test]` no abstractos. |

## Presupuesto de revision

Estimado 600-700 lineas autorales (pruebas + docs + ~6 lineas de runtime). Supera el presupuesto de 400 lineas. **Decision del usuario (2026-08-31): un unico PR marcado `size:exception`**, no PRs encadenados. Los grupos A/B/C/D quedan como guia de commits dentro de ese PR, no como PRs separados.

## Rollback Plan

Cada grupo (A, B, C, D) es un commit revertible por separado dentro del PR unico. Revertir el commit del grupo C devuelve `Dtos.cs` a su estado en `a7a0590`; los grupos A, B y D son solo pruebas y documentacion y se revierten sin tocar runtime. Como `Contract.Version` no sube, ningun modulo consumidor necesita accion de migracion tras un revert.

## Dependencies

- Ventana de integracion del lunes y co-aprobacion de todos los duenos de modulo.
- Unity 6 disponible para correr EditMode (validacion humana o CLI).

## Success Criteria

- [ ] Todas las pruebas EditMode en verde en Unity 6 (Test Runner o `Unity -runTests -batchmode -testPlatform EditMode`).
- [ ] `Docs/CONTRACT-CHANGELOG.md` documenta la v1 completa (invariantes por puerto, determinismo, centinelas, igualdad de DTO, enforcement).
- [ ] `Contract.Version` sigue en 1 y una prueba lo ata al changelog.
- [ ] G1-G15 cerradas o registradas explicitamente como diferidas (G12, G14).
- [ ] El diff no toca ninguna ruta fuera de las 4 autorizadas.

## Decisiones del usuario (2026-08-31)

1. **G11 — completitud reversible.** Una vez `IsComplete == true`, un objetivo PUEDE volver a incompleto: `Progress01` puede bajar y `IsComplete` volver a `false`. Se mantiene `IsComplete` si y solo si `Progress01 == 1`. La spec NO debe exigir pegajosidad.
2. **G9 — DEBERIA.** `Classify` con `IsReady == false` DEBERIA devolver `IntentResult.Unknown`. Regla dura (DEBE): `Classify` NO DEBE lanzar en ningun estado.
3. **Entrega — un unico PR con `size:exception`.** No PRs encadenados. Revision pesada asumida.
4. **G12 — diferido a v2.** Sin objecion: no se agrega `IEquatable`/`GetHashCode` a los DTO en este cambio; se documenta la igualdad estructural por defecto y se registra como candidato v2.
5. **G4 — enums congelados en v1.** Sin objecion: fijar nombre y valor de cada miembro de enum es la rigidez buscada; cualquier rename futuro es, por diseno, una ruptura de contrato.
