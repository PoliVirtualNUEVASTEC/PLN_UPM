# Propuesta: M7 — Entrada física VR (`Runtime/VrInput/`)

## Intent

`IPhysicalActionSource` está congelado en `NpcAi.Core` desde el contrato v1: un único evento,
`OnAction`, que entrega un `PhysicalAction` cuando el usuario hace algo físico con el cuerpo o
los controles VR (`ContactoVisual`, `Acercarse`, `Alejarse`, `EntregarObjeto`,
`SenalarPantalla`, `GestoCalma`, `TocarPaciente`). Hoy `Runtime/VrInput/` solo tiene el doble
(`Fakes/ScriptedPhysicalActionSource.cs`, un `Emit()` manual) — cero implementación real. Este
cambio entrega M7: la primera implementación real de `IPhysicalActionSource`, sobre un Meta
Quest con controles físicos (XR Interaction Toolkit + OpenXR), siguiendo el mismo patrón
núcleo-puro/adaptador que ya usan M1 (Vosk), M4 (Receptivity) y M6 (Dialogue).

## Scope

### In Scope

- `Runtime/VrInput/PhysicalActionSource.cs`: núcleo puro (`noEngineReferences`-friendly, sin
  `UnityEngine`) que implementa `IPhysicalActionSource`. Recibe señales ya interpretadas
  (booleanos/duraciones: "¿el jugador está mirando al NPC?", "¿está dentro de la zona
  cercana?", "¿se soltó un botón de gesto?", etc.) y decide, con umbrales/anti-rebote propios,
  cuándo disparar cada `PhysicalAction`. Es la parte que se prueba en EditMode sin headset,
  igual que las estrategias de segmentación de M1 se prueban con audio sintético.
- `Runtime/VrInput/Unity/VrInputBehaviour.cs` (sub-ensamblado `NpcAi.VrInput.Unity`, mismo
  patrón que `SpeechToTextBehaviour`/`SessionLogBehaviour`): la envoltura real que lee XR
  Interaction Toolkit (rig, controles, interactors) cada frame y alimenta el núcleo. Publica
  además en `PhysicalActionChannel` (ya existe en `NpcAi.Core.Channels`) para quien se conecte
  por Inspector.
- Mapeo controles físicos → `PhysicalAction` (ver `design.md` para el detalle y las decisiones
  del usuario 2026-09-15): zonas de trigger para `Acercarse`/`Alejarse`/`TocarPaciente`, rayo +
  gatillo para `SenalarPantalla`, `XRGrabInteractable` + `XRSocketInteractor` para
  `EntregarObjeto`, botón + orientación del control para `GestoCalma`, y un raycast de la
  cabeza (proxy de mirada, no eye-tracking real) sostenido un mínimo de tiempo para
  `ContactoVisual`.
- Declarar `com.unity.xr.interaction.toolkit` y `com.unity.xr.openxr` como dependencias reales
  en `package.json` (hoy `"vr"` solo aparece como *keyword*, igual que le pasó a Sentis en M2
  antes de corregirse).
- `openspec/specs/entrada-fisica-vr-m7/spec.md` (al archivar): requisitos DEBE/NO DEBE sobre
  el núcleo (anti-rebote, `Ninguna` nunca se emite, conformidad con
  `PhysicalActionSourceContract`) y trazabilidad hacia la validación manual en dispositivo.
- Actualizar `Docs/MODULES.md`: sección M7, estado real. **Dueño se mantiene como Luis Miguel
  Cañaveral Restrepo** (decisión del usuario 2026-09-15: la reasignación no aplica aquí, a
  diferencia de M14/M15 — el trabajo lo hace Nataly pero la atribución de dueño no cambia).

### Out of Scope

- **Eye-tracking real.** El usuario confirmó que prueba con Quest 2/3 sin hand-tracking: solo
  controles físicos. `ContactoVisual` es una aproximación por hacia-dónde-mira-la-cabeza, no
  seguimiento ocular. Si más adelante el equipo usa un Quest Pro, es una ampliación del
  proveedor de mirada, no una reapertura de este cambio.
- **Reconocimiento de gestos de mano libres (hand-tracking).** Decisión del usuario
  2026-09-15: primera entrega simple y confiable con XRI estándar (botones, colisiones,
  grab/socket, rayo), no posturas de mano libres. Es una ampliación futura documentada, no
  parte de este cambio.
- **Escena real de M11.** `VrInputBehaviour` necesita un XR Origin, un ancla en el NPC y una
  "zona cercana"/"zona de contacto" ya armadas en una escena; esa escena es responsabilidad de
  `2026-09-09-m11-armado-sesion`, que sigue sin existir. M7 entrega el componente listo para
  cablearse, no la escena.
- **Qué hace el NPC con cada `PhysicalAction`.** Eso ya lo decide M4 (`ReceptivityEngine.Evaluate`)
  y M6; M7 no cambia esa lógica.
- **Build a dispositivo / validación en Quest real.** Como en M1, es una compuerta humana del
  usuario, no algo que este agente pueda ejecutar.

## Capabilities

### New Capabilities

- `entrada-fisica-vr-m7` (nuevo): primera spec formal de M7. Formaliza el comportamiento
  observable del núcleo (anti-rebote, conformidad con `PhysicalActionSourceContract`) y deja
  trazabilidad hacia la validación manual en dispositivo, que no es una prueba EditMode.

### Modified Capabilities

- Ninguna. `IPhysicalActionSource`, `PhysicalAction`, `PhysicalActionSourceContract` y
  `PhysicalActionChannel` no cambian una firma.

## Approach

**Núcleo puro que recibe señales ya interpretadas, no geometría cruda.** `PhysicalActionSource`
no sabe qué es un `XRGrabInteractable` ni un `Transform`: recibe llamadas como
`ReportarMirada(bool mirandoAlNpc, float deltaSegundos)` o `ReportarZonaCercana(bool dentro)` y
decide, con sus propios umbrales/anti-rebote, si eso constituye un `PhysicalAction` nuevo. Esto
es exactamente lo que hizo M1 con `ISegmentationStrategy`: la lógica de "cuándo es un evento" se
separa de "cómo se detecta la señal cruda", y la primera se prueba en milisegundos sin hardware.

**`VrInputBehaviour` es la única pieza que toca XR Interaction Toolkit.** Vive en
`Runtime/VrInput/Unity/` (sub-ensamblado nuevo, mismo patrón que M1/M13), y en cada
`Update()`/callback de XRI traduce el estado del rig a las llamadas `Reportar*` del núcleo.
Nada de esto se prueba en EditMode más allá de que compile — la validación real es en el Quest,
como el reconocimiento de voz de M1.

**Cada `PhysicalAction` tiene un mecanismo de detección deliberadamente simple** (decisión del
usuario 2026-09-15, ver tabla en `design.md`): zonas de trigger con histéresis para
proximidad/contacto (entra una vez, sale una vez — sin necesidad de matemática de distancia por
frame), grab+socket de XRI para entrega de objetos, rayo+gatillo para señalar, y botón+
orientación para el gesto de calma. Ninguno depende de reconocimiento de gestos libres ni de
seguimiento ocular.

## Affected Areas

| Área | Impacto | Descripción |
|---|---|---|
| `Runtime/VrInput/PhysicalActionSource.cs` | Nuevo | Núcleo puro, implementación real del puerto |
| `Runtime/VrInput/Unity/VrInputBehaviour.cs` | Nuevo | Sub-ensamblado `NpcAi.VrInput.Unity`, envoltura XRI |
| `Runtime/VrInput/Fakes/ScriptedPhysicalActionSource.cs` | Sin cambio | Sigue pasando el mismo contrato |
| `Tests/EditMode/VrInput/` | Nuevo | Pruebas del núcleo con señales sintéticas |
| `package.json` | Modificado | Agrega `dependencies`: XR Interaction Toolkit + OpenXR |
| `Docs/MODULES.md` | Modificado | Sección M7 con estado real |
| `openspec/specs/entrada-fisica-vr-m7/spec.md` | Nuevo (al archivar) | Primera spec formal |
| `Runtime/Core/`, `Runtime/CoreChannels/` | Sin cambio | Cero cambio de contrato |

## Risks

| Riesgo | Prob. | Mitigación |
|---|---|---|
| El proyecto anfitrión no tiene instalados XR Interaction Toolkit / OpenXR todavía (verificado: no aparecen en `Packages/` hoy) | Alta | Se agregan como `dependencies` reales en `package.json`; Unity los resuelve al abrir el proyecto. Instalar y configurar OpenXR (Project Settings → XR Plug-in Management) es un paso manual de la usuaria, documentado en `tasks.md` |
| El proxy de mirada (raycast de cabeza) da falsos positivos si el NPC está en el borde del campo visual | Media | Umbral de ángulo configurable + duración mínima sostenida antes de disparar `ContactoVisual`, igual criterio que `MsMinimosDeVoz` en M1 |
| Sin hardware real durante la fase de agente, el mapeo controles→acción no se puede validar hasta que la usuaria pruebe en el Quest | Alta, aceptada | Mismo patrón que M1: el núcleo se prueba 100% en EditMode con señales sintéticas; la validación en dispositivo es una compuerta humana explícita, no bloqueante para cerrar el núcleo |
| `VrInputBehaviour` referencia tipos de XR Interaction Toolkit que no compilan si el paquete no está instalado | Media | Se instala como parte de este cambio (paso manual documentado), antes de escribir `VrInputBehaviour` |

## Rollback Plan

M7 es aditivo: nada más lo referencia todavía (M11 no existe como escena). Revertir los commits
borra `Runtime/VrInput/PhysicalActionSource.cs`, `Runtime/VrInput/Unity/`,
`Tests/EditMode/VrInput/` nuevos, y las líneas de `dependencies` en `package.json`. El doble
(`ScriptedPhysicalActionSource`) queda intacto y el puerto sigue cubierto para cualquiera que ya
lo use vía el doble. No hay migración de datos.

## Dependencies

- `contrato-nucleo-m0`: define `IPhysicalActionSource`, `PhysicalAction`,
  `PhysicalActionSourceContract`. Ya mergeado, sin cambios necesarios.
- `canales-evento-nucleo-m0`: define `PhysicalActionChannel`. Ya mergeado.
- **Paquetes UPM nuevos** (no instalados hoy en el proyecto anfitrión):
  `com.unity.xr.interaction.toolkit`, `com.unity.xr.openxr`. Se agregan a `dependencies` en
  este cambio; instalarlos y configurar OpenXR es un paso manual de la usuaria.
- Ninguna dependencia de red ni de otro módulo de `Runtime/`.

## Success Criteria

- [ ] `Runtime/VrInput/PhysicalActionSource.cs` implementa `IPhysicalActionSource`, pasa el
      100% de `PhysicalActionSourceContract` (heredado, sin modificar), y ningún método público
      referencia `UnityEngine`.
- [ ] `Runtime/VrInput/Unity/VrInputBehaviour.cs` compila contra XR Interaction Toolkit +
      OpenXR y traduce el estado del rig a las llamadas `Reportar*` del núcleo.
- [ ] `ContactoVisual`, `Acercarse`, `Alejarse`, `EntregarObjeto`, `SenalarPantalla`,
      `GestoCalma` y `TocarPaciente` tienen cada uno un mecanismo de detección documentado en
      `design.md`, sin depender de eye-tracking ni de reconocimiento de gestos libres.
- [ ] `Ninguna` nunca se emite como evento (invariante heredada del contrato).
- [ ] `package.json` declara XR Interaction Toolkit y OpenXR como `dependencies` reales.
- [ ] `Docs/MODULES.md` tiene la sección M7 actualizada, con Luis Miguel Cañaveral Restrepo
      como dueño documentado.
- [ ] El diff no toca `Runtime/Core/`, `Runtime/CoreChannels/` ni ninguna carpeta fuera de
      `Runtime/VrInput/`, `Tests/EditMode/VrInput/`, `package.json`, `Docs/` y `openspec/`.

## Decisiones del usuario (confirmadas 2026-09-15)

1. Hardware de prueba: **Meta Quest 3 (o 2), solo controles físicos** — sin hand-tracking, sin
   eye-tracking. `ContactoVisual` se aproxima con un raycast de la cabeza, no mirada ocular.
2. Estrategia de gestos: **simple y confiable con XR Interaction Toolkit estándar**
   (botones, colisiones/triggers, grab+socket, rayo) — no reconocimiento de gestos de mano
   libres. Ampliar a eye-tracking o hand-tracking real es una entrega futura, no parte de este
   cambio.
3. Atribución de dueño en `Docs/MODULES.md`: **se mantiene Luis Miguel Cañaveral Restrepo**
   (a diferencia de M14/M15, aquí no hay reasignación formal — el trabajo lo hace Nataly).
