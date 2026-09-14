# Tasks: Historial multi-sesión para M13

## Review Workload Forecast

| Field | Value |
|---|---|
| Estimated changed lines | ~350 (un solo PR; sin dependencia externa nueva, SQLite ya vendorizado) |
| 400-line budget risk | Bajo |
| Chained PRs recommended | No — cambio autocontenido dentro de `Runtime/SessionLog/` |

## Phase 0: Guardrails (leer antes de escribir)

- [x] 0.1 Frontera de escritura: SOLO `Runtime/SessionLog/` (núcleo, `Fakes/`, `Sqlite/`,
  `Unity/`), `Tests/EditMode/SessionLog/`, `openspec/changes/2026-09-10-historial-multi-sesion-m13/`
  y `openspec/specs/bitacora-sesion-m13/spec.md`. Nada en `Runtime/Core/` ni otro módulo.
- [x] 0.2 No reabrir `openspec/changes/archive/2026-09-07-bitacora-sesion-m13/`: ese cambio
  queda tal cual, este es un cambio nuevo que declara `bitacora-sesion-m13` como Modified
  Capability.
- [x] 0.3 `ISessionStore` sigue sin ser puerto de `NpcAi.Core`: cero cambio en
  `Runtime/Core/Ports.cs`.

## Phase 1: Núcleo puro (DTO + interfaz + recorder)

- [x] 1.1 Crear `Runtime/SessionLog/SesionInfo.cs` (`Etiqueta`, `IniciadaEn`).
- [x] 1.2 Crear `Runtime/SessionLog/EtiquetaSesion.cs`: helper interno `Normalizar(string)` +
  constante `PorDefecto` (AD2), compartido por ambos adaptadores.
- [x] 1.3 Modificar `Runtime/SessionLog/ISessionStore.cs`: `IniciarSesion(string etiqueta)`,
  `ListarSesiones()`, `ObtenerTurnosDeSesion(string etiqueta)`.
- [x] 1.4 Modificar `Runtime/SessionLog/SessionRecorder.cs`: refleja la nueva firma de
  `IniciarSesion` y agrega `ListarSesiones()`/`ObtenerTurnosDeSesion(string)` como pass-through.
- [x] 1.5 Modificar `Runtime/SessionLog/SessionExport.cs`: overload
  `ExportarTextoPlano(ISessionStore store, string etiqueta)` sobre
  `store.ObtenerTurnosDeSesion(etiqueta)`.

## Phase 2: Adaptadores (paridad)

- [x] 2.1 Reescribir `Runtime/SessionLog/Fakes/InMemorySessionStore.cs` sobre
  `Dictionary<string, List<SessionTurn>>` + `Dictionary<string, DateTime>` de inicio, con
  etiqueta activa (AD1, AD3).
- [x] 2.2 Reescribir `Runtime/SessionLog/Sqlite/SqliteSessionStore.cs`: tabla `SesionRow`
  (`Etiqueta` PK, `IniciadaEn`) nueva, columna `Etiqueta` en `TurnoRow`, quitar `DeleteAll` de
  `IniciarSesion` (AD4).
- [x] 2.3 RED/GREEN: reescribir `Tests/EditMode/SessionLog/SessionStoreContract.cs` — quitar
  `Una_segunda_IniciarSesion_reinicia_limpio`; agregar `Sesiones_distintas_no_mezclan_turnos`,
  `ListarSesiones_incluye_todas_las_iniciadas_mas_reciente_primero`,
  `ObtenerTurnosDeSesion_no_se_ve_afectado_por_cambiar_de_sesion_activa`,
  `Reusar_la_misma_etiqueta_acumula_turnos_en_vez_de_borrar`,
  `Etiqueta_null_o_vacia_usa_el_valor_por_defecto`.
- [ ] 2.4 Confirmar `InMemorySessionStoreTests` y `SqliteSessionStoreTests` (heredan la base
  reescrita) pasan igual en los dos adaptadores — paridad. **Pendiente del usuario en Unity.**
- [x] 2.5 Ajustar `Tests/EditMode/SessionLog/SqliteSessionStoreTests.Un_turno_sobrevive_a_reabrir...`
  a la nueva firma de `IniciarSesion` (pasar una etiqueta), sin cambiar lo que prueba.

## Phase 3: Recorder + Unity + exportación

- [x] 3.1 Ajustar `Tests/EditMode/SessionLog/SessionRecorderTests.cs` a la nueva firma de
  `IniciarSesion(etiqueta)`; agregar pruebas de `ListarSesiones`/`ObtenerTurnosDeSesion` a nivel
  `SessionRecorder`.
- [x] 3.2 Ajustar `Tests/EditMode/SessionLog/SessionExportTests.cs`: prueba del nuevo overload
  `ExportarTextoPlano(store, etiqueta)` sobre una sesión que no es la activa.
- [x] 3.3 Modificar `Runtime/SessionLog/Unity/SessionLogBehaviour.cs`: `IniciarSesion(string
  etiqueta)`, agregar `ListarSesiones()`/`ObtenerTurnosDeSesion(string)`/
  `ExportarTextoPlano(string etiqueta)` como pass-through público.
- [x] 3.4 Actualizar el arnés de prueba (`Assets/_ArnesM13/`, fuera del paquete) para pasar una
  etiqueta de texto al botón "Iniciar Sesion", y agregar botones para listar sesiones / exportar
  una sesión pasada. (Hecho por el agente como cortesía; no es parte del diff del paquete.)

## Phase 3b: Reanudación automática tras un cierre no limpio (ampliación 2026-09-14)

Motivado por una prueba real del usuario: si la app se cierra por un error a mitad de una
sesión, al reabrir no debía hacer falta reintroducir la etiqueta para seguir grabando donde
se quedó (AD7, AD8 de `design.md`).

- [x] 3b.1 Agregar `SessionRecorder.ReanudarUltimaSesion()`: retoma la etiqueta más reciente de
  `ListarSesiones()` si existe alguna; no-op si nunca hubo sesión.
- [x] 3b.2 Agregar `SessionRecorder.EtiquetaActiva` (propiedad de solo lectura).
- [x] 3b.3 Corregir `SessionRecorder.IniciarSesion`: `_siguienteSecuencia` ahora continúa desde
  `ObtenerTurnosDeSesion(etiqueta).Count` en vez de reiniciar siempre en 0 (bug latente que esta
  ampliación expuso con más frecuencia).
- [x] 3b.4 `SessionLogBehaviour.OnEnable()` llama `ReanudarUltimaSesion()` automáticamente;
  expone `EtiquetaActiva`.
- [x] 3b.5 Pruebas nuevas en `SessionRecorderTests`:
  `IniciarSesion_con_etiqueta_ya_usada_continua_la_numeracion_en_vez_de_reiniciar`,
  `ReanudarUltimaSesion_retoma_la_etiqueta_mas_reciente_y_permite_seguir_registrando`,
  `ReanudarUltimaSesion_sin_sesiones_previas_no_activa_nada`.
- [x] 3b.6 Actualizar el arnés de prueba para mostrar `EtiquetaActiva` (verificación visual de
  que la reanudación automática funcionó).
- [x] 3b.7 Actualizar `proposal.md`, `design.md` (AD7, AD8) y `spec.md` (nuevo requisito +
  trazabilidad) con esta ampliación.

## Phase 4: Spec + cierre

- [x] 4.1 Modificar `openspec/specs/bitacora-sesion-m13/spec.md`: reemplazar el requisito "una
  segunda IniciarSesion reinicia limpio" por el nuevo comportamiento; agregar los requisitos
  nuevos (listar, consultar por etiqueta, reanudación automática) con sus escenarios y
  trazabilidad.
- [ ] 4.2 `git add` solo de las rutas de este cambio; `git diff --cached` confirmando que no
  cruza a otro módulo.
- [ ] 4.3 Checklist "Antes de mergear" del `README.md`.
- [ ] 4.4 Al cerrar: mover este cambio a `openspec/changes/archive/`, con su propio
  `archive-report.md` (sin `verify-report.md` propio si se hace por inspección, igual patrón
  que M13 original).
- [ ] 4.5 PR mergeado a `main` por el autor (regla 8).

> Fases 1-3 (código) razonadas e implementadas por el agente; RED/GREEN no se ejecutó en el
> agente (sin CI/runner headless en este repo) — **verificación manual en Unity Editor
> pendiente del usuario** (Test Runner > EditMode > Run All).

## Notas

- Ningún agente ejecuta Unity: cada verde de Test Runner es compuerta humana.
- Este cambio no toca `Runtime/Core/`: no es cambio de contrato, sin ventana ni co-aprobación.
