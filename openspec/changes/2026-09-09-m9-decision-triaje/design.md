# Design: M9 — Decisión de triaje y evaluación

## Technical Approach

Implementación real de `IScenarioObjective` para el escenario de emergencia,
`EmergencyScenarioObjective`. Combina dos dimensiones de progreso (receptividad +
información clínica obtenida) y agrega la operación de decisión de triaje que compara con
`clave.triajeEsperado` de M14 y cierra la sesión.

### Unidad de módulo y grafo de referencias

    NpcAi.Scenarios.Emergency -> [NpcAi.Core, NpcAi.Core.Channels]   (sin cambio)

El `.asmdef` de M9 ya referencia `NpcAi.Core` y `NpcAi.Core.Channels`. Este cambio **no le
agrega referencias**. M9 no referencia `NpcAi.ClinicalResponse` (M15) ni `NpcAi.Nlu` (M2):
recibe lo que necesita como datos primitivos (`string campo`, `Triage categoria`) desde M11.

## Architecture Decisions

| # | Decisión | Opciones | Recomendación | Razón |
|---|---|---|---|---|
| **AD1** | ¿Cómo entran a M9 (a) los hechos clínicos obtenidos y (b) la decisión de triaje? | **(a-interna)** API propia de `EmergencyScenarioObjective` (`RegistrarHechoObtenido(string)`, `EnviarTriaje(Triage)`), M11 la llama; sin cambio de contrato. **(b-puerto)** puerto `ITriageBoard` de M0 para la decisión + canal/`Notify` para los hechos; cambio de contrato, crece el alcance de `2026-09-09-m0-puerto-respuesta-clinica` | **Híbrido**: `ITriageBoard` como puerto de M0 (la decisión de triaje es salida de primera clase: la dispara la UI, M13 la registra, las pruebas de integración la necesitan abstracta); los hechos obtenidos como API interna (a) por ahora | Minimiza la superficie de contrato nueva a lo que de verdad cruza módulos, sin bloquear M9 en una segunda formalización |
| AD2 | Fórmula de `Progress01` | `w_r*receptividad + w_c*clinico`; solo clínico; solo receptividad (como el doble) | Combinada, con `w_r = 0.3`, `w_c = 0.7` (tuning de record, editable) | El objetivo del triaje es *obtener información*; la receptividad es medio, no fin. Pesos documentados y ajustables sin tocar el contrato (patrón M5) |
| AD3 | Origen de la lista de ítems a cubrir | `clave.banderasRojas` de M14, mapeadas a `campo` de `hechos` por una tabla en la config de M9 | Derivarlas del texto de las banderas; una lista propia de M9 sin relación con M14 | La fuente de verdad del caso es M14; M9 solo mapea. Si `banderasRojas` se estructura en M14 (su Open Question), el mapeo se simplifica |
| AD4 | `Triage` enum | `Triage { NoAsignado = 0, I = 1, II = 2, III = 3, IV = 4, V = 5 }` | Cadena; reutilizar algo existente | `NoAsignado = 0` sigue la regla centinela del contrato v1 ("el 0 es el valor seguro"). Mapea 1:1 con la cadena romana de `clave.triajeEsperado` de M14 |
| AD5 | Estado tras `EnviarTriaje` | La sesión queda **cerrada**: `IsComplete` refleja el estado final y ya no es reversible por `Notify` | Permitir reenviar; seguir mutando el progreso | Una consulta se cierra una vez. La spec de M9 define este estado terminal; la prueba `ScenarioObjectiveContract.La_completitud_es_reversible` no lo contradice porque nunca llama `EnviarTriaje` |
| AD6 | `Notify` y el doble | `EmergencyScenarioObjective` sigue implementando `Notify(ReceptivityChange)` con la semántica del contrato; `ScriptedScenarioObjective` no cambia | Reemplazar el doble | El doble debe seguir pasando `ScenarioObjectiveContract` tal cual; solo la implementación real gana comportamiento |

## Data Flow

    M14: Data/Cases/caso-0X.json → clave { triajeEsperado, banderasRojas }
              │  (M11 carga y pasa a M9 al armar la sesión)
              ▼
    new EmergencyScenarioObjective(banderasRojas, triajeEsperado, mapeoBanderaACampo)
              │
    ── durante la consulta ──────────────────────────────────────────────
    M4 ReceptivityChange ── Notify(change) ──► actualiza progresoReceptividad
    M15 ClinicalResponse (Handled, campo) ──► M11 ──► RegistrarHechoObtenido(campo)
                                                     ──► actualiza progresoClinico
              │
    Progress01 = 0.3*progresoReceptividad + 0.7*progresoClinico   (∈ [0,1])
    IsComplete = (Progress01 == 1)   ... hasta que:
    ── cierre ───────────────────────────────────────────────────────────
    UI botón → EnviarTriaje(Triage.II)
              │  compara con triajeEsperado
              ▼
    TriajeVeredicto { Elegida, Esperada, Acerto, CierreEsperado }
    sesión cerrada (IsComplete fijo, Notify deja de mutar)

## File Inventory

| Archivo | Rol |
|---|---|
| `Runtime/Scenarios/Emergency/EmergencyScenarioObjective.cs` | `IScenarioObjective` real + API `RegistrarHechoObtenido(string)` y (según AD1) `EnviarTriaje` o implementación de `ITriageBoard` |
| `Runtime/Scenarios/Emergency/EmergencyObjectiveConfig.cs` | Estructura con `banderasRojas`, `triajeEsperado`, `mapeoBanderaACampo`; la arma M11 desde el caso de M14 |
| `Runtime/Scenarios/Emergency/Fakes/ScriptedScenarioObjective.cs` | Sin cambio (doble del puerto `IScenarioObjective`) |
| `Runtime/Scenarios/Emergency/Fakes/ScriptedTriageBoard.cs` | Nuevo **solo si AD1-b**: doble de `ITriageBoard` |
| `Tests/EditMode/Scenarios/Emergency/EmergencyScenarioObjectiveTests.cs` | `: ScenarioObjectiveContract` + pruebas propias |
| `Tests/EditMode/Scenarios/Emergency/TriageVerdictTests.cs` | Acierto / desacierto / cierre |
| `openspec/specs/escenario-emergencia-m9/spec.md` | Primera spec formal (al archivar) |

## Interfaces / Contracts

```csharp
// Si AD1-b: en NpcAi.Core (dentro del cambio 2026-09-09-m0-puerto-respuesta-clinica)
public enum Triage { NoAsignado = 0, I = 1, II = 2, III = 3, IV = 4, V = 5 }

public readonly struct TriajeVeredicto
{
    public readonly Triage Elegida;
    public readonly Triage Esperada;
    public readonly bool   Acerto;          // Elegida == Esperada
    public readonly string CierreEsperado;  // texto del caso; nunca null
}

public interface ITriageBoard
{
    /// <summary>Envía la categoría de triaje y cierra la sesión. Idempotente tras el primer envío.</summary>
    TriajeVeredicto EnviarTriaje(Triage categoria);
    bool Enviado { get; }
}

// M9 (siempre)
public sealed class EmergencyScenarioObjective : IScenarioObjective   // (+ ITriageBoard si AD1-b)
{
    public EmergencyScenarioObjective(EmergencyObjectiveConfig config);
    public float Progress01 { get; }
    public bool  IsComplete { get; }
    public void  Notify(ReceptivityChange change);
    public void  RegistrarHechoObtenido(string campo);   // API interna, la llama M11
}
```

## Testing Strategy

| Archivo | Qué fija |
|---|---|
| `ScenarioObjectiveContract` (heredada, sin cambios) | Progreso en `[0,1]`, `IsComplete` ⟺ `Progress01 == 1`, reversible por `Notify`, `Notify(default)` no-op, no baja de 0 |
| `EmergencyScenarioObjectiveTests` | Hereda la base; `CreateSubject()` con una config de prueba. Pruebas propias: `RegistrarHechoObtenido` de un campo ligado a bandera roja sube `Progress01`; cubrir todas + receptividad ok ⇒ `Progress01 == 1`; un campo no ligado no sube el progreso clínico |
| `TriageVerdictTests` | `EnviarTriaje(Esperada)` ⇒ `Acerto`; `EnviarTriaje(otra)` ⇒ `!Acerto` con `Esperada` correcta; segundo envío es no-op / idempotente; `CierreEsperado` no vacío |
| Prueba de no-regresión | Tras `EnviarTriaje`, correr de nuevo la batería de `ScenarioObjectiveContract` sobre otra instancia (sin enviar) sigue verde |

Ejecución EditMode: Test Runner de Unity 6. Ningún agente ejecuta Unity: el verde es
compuerta humana.

## Migration / Rollout

Sin migración. `EmergencyScenarioObjective` se instancia en M11 con la config del caso.
Rollback: revertir deja `Runtime/Scenarios/Emergency/` en "solo doble".

## Open Questions

- [ ] **AD1**: confirmar híbrido (puerto `ITriageBoard` + API interna para hechos) vs.
      todo-interno vs. todo-contrato. Si se elige el puerto, coordinar con
      `2026-09-09-m0-puerto-respuesta-clinica` para incluirlo en el mismo `v2`.
- [ ] Pesos `w_r`/`w_c` de `Progress01`: ¿0.3/0.7 o el objetivo clínico pesa 100% y la
      receptividad solo actúa como tope (si `NoReceptivo`, `Progress01` no puede llegar a 1)?
- [ ] ¿`banderasRojas` en M14 se estructura (lista de `{descripcion, camposLigados[]}`)
      para que el mapeo de AD3 viva en el dato y no en la config de M9?
- [ ] ¿M13 (bitácora) se suscribe al `TriajeVeredicto`? Fuera de alcance de este cambio;
      anotar como seguimiento.
