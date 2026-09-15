# Tasks: M7 — Entrada física VR

## Review Workload Forecast

| Field | Value |
|---|---|
| Estimated changed lines | PR1 ~180 (núcleo + settings + pruebas), PR2 ~150 (sub-ensamblado Unity + `Data/VrInput/` + `package.json`) |
| 400-line budget risk | Bajo |
| Chained PRs recommended | Sí |
| Suggested split | PR1 → PR2 |
| Chain strategy | stacked-to-main (PR1 → main, PR2 → PR1) |

### Suggested Work Units

| Unit | Meta | PR | Prueba enfocada | Frontera de rollback |
|---|---|---|---|---|
| 1 | Núcleo puro + settings + doble sin cambios | PR1 | EditMode: `PhysicalActionSourceTests : PhysicalActionSourceContract` + anti-rebote | Borrar `PhysicalActionSource.cs`/`VrInputSettings.cs`; el doble sigue cubriendo el puerto |
| 2 | Sub-ensamblado Unity + dependencias XR + dato | PR2 | Compila contra XRI/OpenXR; validación manual en Quest | Borrar `Runtime/VrInput/Unity/`, `Data/VrInput/`, revertir `package.json` |

## Phase 0: Guardrails (leer antes de escribir)

- [ ] 0.1 Frontera de escritura PR1: `Runtime/VrInput/{PhysicalActionSource.cs,VrInputSettings.cs,Config/}`
  y `Tests/EditMode/VrInput/`. Frontera PR2: `Runtime/VrInput/Unity/`, `Data/VrInput/`,
  `package.json`. Más `Docs/MODULES.md` y `openspec/` en el último PR.
- [ ] 0.2 **Cero cambio en `NpcAi.Core`.** `IPhysicalActionSource`, `PhysicalAction`,
  `PhysicalActionChannel` no cambian firma. Si hace falta otra firma, parar y avisar — cambio
  de contrato v2, otro alcance.
- [ ] 0.3 `Tests/EditMode/Core/PhysicalActionSourceContract.cs` no se modifica: las pruebas
  nuevas **heredan**, no editan.
- [ ] 0.4 `NpcAi.VrInput.asmdef` no cambia (ya referencia `NpcAi.Core` + `NpcAi.Core.Channels`).
  Solo `NpcAi.VrInput.Unity.asmdef` (nuevo) referencia XR Interaction Toolkit.
- [ ] 0.5 Antes de PR2: instalar `com.unity.xr.interaction.toolkit` y `com.unity.xr.openxr`
  vía Package Manager en el proyecto anfitrión (paso manual de la usuaria), y habilitar OpenXR
  en Project Settings → XR Plug-in Management. Si no están instalados, `NpcAi.VrInput.Unity`
  no compila — no seguir con PR2 sin esto confirmado.
- [ ] 0.6 Módulo con integración de hardware real (como M1): flujo SDD completo, sin
  `/sdd-ff`.

## Phase 1: Núcleo puro (PR1)

- [ ] 1.1 Crear `Runtime/VrInput/VrInputSettings.cs`: `readonly struct` con
  `MinSegundosDeMirada` (default 1.0) y `CooldownSegundos` (default 0.5), sin `UnityEngine`.
- [ ] 1.2 RED: `Tests/EditMode/VrInput/PhysicalActionSourceTests.cs` —
  `: PhysicalActionSourceContract`, `CreateSubject() => new PhysicalActionSource(VrInputSettings.Default)`,
  `EmitTestAction` mapea cada `PhysicalAction` de prueba al `Reportar*` correspondiente. Añadir
  pruebas propias de anti-rebote (ver `design.md` → Testing Strategy): mirada acumulada,
  no-repetición de `ContactoVisual`, transición única de `Acercarse`/`Alejarse`, histéresis de
  `TocarPaciente`, cooldown de `EntregarObjeto`/`SenalarPantalla`/`GestoCalma`.
- [ ] 1.3 GREEN: `Runtime/VrInput/PhysicalActionSource.cs` — implementa `IPhysicalActionSource`
  y los 6 métodos `Reportar*` de `design.md`. Determinista, sin `UnityEngine`.
- [ ] 1.4 Confirmar que `Fakes/ScriptedPhysicalActionSource.cs` sigue sin tocarse y sigue
  pasando `PhysicalActionSourceContract` (ya lo hacía).
- [ ] 1.5 Crear `Runtime/VrInput/Config/VrInputSettingsAsset.cs`: ScriptableObject con
  `OnValidate` acotando rangos (ver tabla de `design.md`), `ToSettings()` devuelve el snapshot.
- [ ] 1.6 RED/GREEN: prueba de `VrInputSettingsAsset.OnValidate` en EditMode
  (`ScriptableObject.CreateInstance`).
- [ ] 1.7 MANUAL (Editor de Unity): Test Runner → EditMode, verde en
  `PhysicalActionSourceTests` y la prueba del asset. **Sin necesidad de XRI instalado todavía**
  — este PR compila sin los paquetes XR.

## Phase 2: Envoltura Unity + dependencias XR (PR2)

- [ ] 2.0 Instalar `com.unity.xr.interaction.toolkit` y `com.unity.xr.openxr` en el proyecto
  anfitrión (Package Manager); habilitar OpenXR (Project Settings → XR Plug-in Management).
  Confirmar versiones exactas resueltas por el Editor de la usuaria.
- [ ] 2.1 Actualizar `package.json`: agregar bloque `dependencies` con las versiones
  confirmadas en 2.0.
- [ ] 2.2 Crear `Runtime/VrInput/Unity/NpcAi.VrInput.Unity.asmdef`:
  `references: ["NpcAi.Core", "NpcAi.Core.Channels", "NpcAi.VrInput", "Unity.XR.Interaction.Toolkit", "Unity.XR.CoreUtils"]`.
- [ ] 2.3 Crear `Runtime/VrInput/Unity/VrInputBehaviour.cs`: construye `PhysicalActionSource`
  con el snapshot del `VrInputSettingsAsset` asignado por Inspector; en `Update()` calcula el
  ángulo de mirada hacia el ancla del NPC y llama `ReportarMirada`; suscribe `OnTriggerEnter`/
  `OnTriggerExit` de las zonas cercana/contacto (colliders hijos o referenciados por Inspector)
  a `ReportarZonaCercana`/`ReportarContactoPaciente`; suscribe el evento `selectEntered` del
  `XRSocketInteractor` de entrega a `ReportarEntrega`; suscribe el rayo (`XRRayInteractor`) y
  el botón de gesto de calma (Input System) a `ReportarSenalado`/`ReportarGestoCalma` con la
  comprobación de ángulo correspondiente. Publica cada `PhysicalAction` recibido de
  `PhysicalActionSource.OnAction` en `PhysicalActionChannel.Raise`.
- [ ] 2.4 Crear `Data/VrInput/README.md` (propósito del asset, cómo ajustar ángulos/cooldowns
  tras probar en el Quest) y `Data/VrInput/Default.asset`.
- [ ] 2.5 Confirmar que el proyecto compila sin errores con los paquetes XR instalados
  (Console limpia tras importar).
- [ ] 2.6 MANUAL (dispositivo, compuerta humana de la usuaria — no ejecutable por el agente):
  armar una escena de prueba desechable (fuera del paquete, como `_ArnesM13`) con un XR Origin,
  un objeto placeholder de NPC con las zonas de trigger, y validar en el Quest que cada una de
  las 7 señales físicas dispara el `PhysicalAction` esperado.

## Phase 3: Documentación y cierre (último PR)

- [ ] 3.1 Agregar la sección **M7 — Entrada física VR** a `Docs/MODULES.md`: carpeta, dueño
  (Luis Miguel Cañaveral Restrepo — atribución sin cambio, decisión del usuario), qué hace,
  mapeo de las 7 acciones, estado real, dependencias XR nuevas, pendiente de validación en
  dispositivo.
- [ ] 3.2 Revisar que el diff acumulado de PR1+PR2 respeta los `Success Criteria` de
  `proposal.md` (ninguna carpeta fuera de `Runtime/VrInput/`, `Tests/EditMode/VrInput/`,
  `Data/VrInput/`, `package.json`, `Docs/`, `openspec/`).

## Phase 4: Cierre (acciones del autor, cada PR)

- [ ] 4.1 `git add` solo de las carpetas del PR; `git diff --cached` antes de commitear.
- [ ] 4.2 Checklist "Antes de mergear" del `README.md` (pruebas propias en verde incluida la
  de contrato, diff acotado, spec/design/tasks archivados, rama al día con `main`).
- [ ] 4.3 Al cerrar el último PR: mover el cambio a `openspec/changes/archive/`, crear
  `openspec/specs/entrada-fisica-vr-m7/spec.md` (requisitos DEBE/NO DEBE: anti-rebote,
  `Ninguna` nunca se emite, conformidad con `PhysicalActionSourceContract`; trazabilidad hacia
  la validación manual en dispositivo, que no es una prueba EditMode), `archive-report.md`.
- [ ] 4.4 PR mergeado a `main` por la autora (regla 8).
- [ ] 4.5 Registrar en el documento de contexto (regla 10). No aplica Engram en este entorno.

## Notas

- Ningún agente instala paquetes de Unity ni configura Project Settings: el paso 0.5/2.0 es
  manual, de la usuaria.
- Ningún agente ejecuta Unity ni tiene acceso a un Quest: el paso 2.6 es una compuerta humana
  explícita, igual que la validación de hardware de M1.
- Ampliar M7 con eye-tracking (Quest Pro) o hand-tracking real es una entrega futura, no una
  reapertura de este cambio: el núcleo (`PhysicalActionSource`) no cambia — solo se agregaría
  un proveedor de señal nuevo en `VrInputBehaviour` o un `VrInputBehaviour` alternativo.
