# Design: M0 — Puerto de respuesta clínica (`IClinicalResponder`)

## Technical Approach

Extensión **puramente aditiva** de `NpcAi.Core`. Se agregan un puerto, dos DTO y un
identificador; se sube `Contract.Version` a `2`; se registra la entrada `## v2` en el
changelog. Ningún tipo de v1 cambia un byte. El puerto se diseña para el patrón enrutador:
el llamador pregunta primero a `IClinicalResponder`, y solo si la respuesta viene con
`Handled == false` enruta el turno a `IDialogueGenerator` (M6).

### Unidad de módulo y grafo de referencias

    NpcAi.Core        (sin cambio de referencias; noEngineReferences: true SE MANTIENE)

Los tres tipos nuevos son C# puro: `string`, `bool`, `struct`, más los tipos v1 `Utterance`,
`IntentResult`, `NpcReply`, `PersonalityId`. Cero `UnityEngine`, cero `NpcAi.*` ajeno —
`CoreAssemblyPurityTests` (G2) lo verifica por reflexión sobre el binario. `NpcAi.CoreChannels`
**no** gana un canal en este cambio (ver AD5).

## Architecture Decisions

| # | Decisión | Elección | Alternativas rechazadas | Razón |
|---|---|---|---|---|
| AD1 | ¿Puerto nuevo, o crecer `IDialogueGenerator`? | Puerto nuevo `IClinicalResponder` | Agregar un parámetro de caso a `Generate` de M6 | Decisión del equipo (2026-09-09). Separa contenido determinista (hechos del caso) de estilo estocástico (Markov de M6). Crecer M6 obligaría además a que su cadena de Markov reciba y preserve un dato textual, cosa que no garantiza |
| AD2 | ¿El caso viaja como objeto o como id? | `ClinicalCaseId` (string), vinculado con `AssignCase` | `ClinicalCase` DTO con síntomas/antecedentes/signos vitales/tabla de hechos dentro de `NpcAi.Core` | Un DTO rico en el contrato lo ataría al esquema de M14; cualquier campo nuevo del caso sería un cambio de contrato. Con un id, M15 resuelve el contenido contra `Data/Cases/` y el esquema de M14 evoluciona libre. Espeja `PersonalityId` (decisión 1 del contrato v1) |
| AD3 | ¿Cómo se vincula el caso al respondedor? | `AssignCase(ClinicalCaseId, PersonalityId)`, análogo a `IReceptivityEngine.Reset(PersonalityId)` | Pasar `ClinicalCaseId` y `PersonalityId` en cada llamada a `Respond` | El caso y la personalidad son estado de sesión, no de turno: se fijan una vez al armar la sesión (M11) y no cambian mientras dure la consulta. Igual patrón que el `Reset` de M4. `Respond` queda con la firma mínima: lo que dijo la enfermera + su intención/tono ya clasificados por M2 |
| AD4 | Señal de "turno no clínico" | `bool Handled` en `ClinicalResponse` (`false` ⇒ enrutar a M6) | `NpcReply?` nulable; excepción; un `Intent`/enum nuevo de "tipo de pregunta" | Un `bool` en el DTO de salida hace el enrutador trivial y no agrega tipos nulables al contrato congelado. Una excepción violaría "NO DEBE lanzar". Un enum nuevo de tipos de pregunta clínica sería superficie de contrato que M15 puede resolver internamente |
| AD5 | ¿`EventChannel<ClinicalResponse>` en `NpcAi.CoreChannels`? | No en este cambio | Agregar el canal ahora | El enrutado M15→M8 y M15/M6 lo decide M11 (`2026-09-09-m11-armado-sesion`). Si esa integración resulta cablearse por Inspector, el canal se agrega ahí como cambio de M0 acotado. Agregarlo ahora sin consumidor es superficie muerta |
| AD6 | Determinismo de `Respond` | **Determinista** en `Handled` y `Reply.Text` para la misma `(caso, personalidad, utterance, intent)` | Permitir variación como en M6 | Un hecho clínico ("hace 2 meses", "alérgica al Tramadol") no puede cambiar de redacción entre turnos: confundiría la evaluación de triaje. M15 usa plantillas, no Markov. Asimetría nueva, registrada en `CONTRACT-CHANGELOG.md` v2 junto a la de v1 (`Classify` determinista / `Generate` no) |
| AD7 | Estado cuando el `ClinicalCaseId` no existe en M14 | `IsReady == false`; `Respond` devuelve `ClinicalResponse.NoAplica` sin lanzar | Lanzar; devolver `Handled == true` con texto de relleno | Simetría con `IIntentClassifier`: `IsReady == false` ⇒ salida segura, nunca excepción. Con `IsReady == false` el enrutador manda todo a M6, que ya funciona con cualquier entrada |
| AD8 | Corregir la regla de integración obsoleta del changelog | Sí, en este mismo cambio | Dejarla y abrir otro cambio | El cambio ya edita `CONTRACT-CHANGELOG.md` para agregar `## v2`; dejar la línea 4 diciendo "solo los lunes / todos los duenos" contradiría al `CLAUDE.md` del repo y a `openspec/config.yaml`, que ya fijaron "sin ventana fija, co-revisión del otro dueño de M0 o el asesor" |

## Data Flow

    (M11 arma la sesión: elige ClinicalCaseId + PersonalityId al azar)
              │
              ▼
    IClinicalResponder.AssignCase(caseId, personality)   ← una vez por sesión
              │
              ▼   por cada turno de la enfermera:
    M1 ISpeechToText → Utterance ──┐
    M2 IIntentClassifier → IntentResult ──┐
                                          ▼
              IClinicalResponder.Respond(utterance, intent) → ClinicalResponse
                                          │
                     ┌────────────────────┴─────────────────────┐
              Handled == true                            Handled == false
                     │                                          │
                     ▼                                          ▼
             Reply → M8 INpcPresenter          M6 IDialogueGenerator.Generate(...) → M8

`NpcAi.Core` solo define las formas y el puerto. Quién llama en qué orden es M11.

## File Inventory

| Archivo | Rol |
|---|---|
| `Runtime/Core/ClinicalCaseId.cs` | `readonly struct ClinicalCaseId : IEquatable<ClinicalCaseId>` — copia estructural de `PersonalityId.cs` |
| `Runtime/Core/Dtos.cs` | `+ readonly struct ClinicalResponse` (`Handled`, `Reply`, `static NoAplica`) |
| `Runtime/Core/Ports.cs` | `+ interface IClinicalResponder` en la sección "Comprensión" o una nueva "Respuesta clínica" |
| `Runtime/Core/Contract.cs` | `Version` `1 → 2` |
| `Docs/CONTRACT-CHANGELOG.md` | `+ ## v2` (tipos nuevos, invariantes DEBE/NO DEBE, asimetría de determinismo de M15); corrección de la regla de integración |
| `Tests/EditMode/Core/ClinicalResponderContract.cs` | Base abstracta con `[Test]` no abstractos; `protected abstract IClinicalResponder CreateSubject();` + stub local no-listo |
| `Tests/EditMode/Core/ContractTypeTests.cs` | `+` casos para `ClinicalCaseId` (igualdad `Ordinal`, `None`, `IsNone`) y `ClinicalResponse` (`NoAplica.Handled == false`) |

## Interfaces / Contracts

```csharp
// Runtime/Core/ClinicalCaseId.cs — espejo exacto de PersonalityId
public readonly struct ClinicalCaseId : System.IEquatable<ClinicalCaseId>
{
    public readonly string Value;                 // minúsculas, sin tildes, p. ej. "caso-01"
    public ClinicalCaseId(string value);
    public static readonly ClinicalCaseId None;   // default
    public bool IsNone { get; }
    // Equals / GetHashCode / operator == / operator != — comparación Ordinal
}

// Runtime/Core/Dtos.cs
public readonly struct ClinicalResponse
{
    public readonly bool     Handled;   // false ⇒ turno no clínico, enrutar a M6
    public readonly NpcReply Reply;     // válido solo si Handled

    public ClinicalResponse(bool handled, NpcReply reply);
    public static ClinicalResponse NoAplica => new ClinicalResponse(false, default);
}

// Runtime/Core/Ports.cs
/// <summary>M15 — responde como el paciente del caso clínico asignado.</summary>
public interface IClinicalResponder
{
    /// <summary>False hasta que <see cref="AssignCase"/> vincule un caso existente. Leer NO DEBE lanzar.</summary>
    bool IsReady { get; }

    /// <summary>
    /// Vincula el caso y la personalidad. Con el mismo par DEBE ser determinista e
    /// idempotente. Con un <see cref="ClinicalCaseId"/> desconocido NO DEBE lanzar y
    /// deja <see cref="IsReady"/> en false.
    /// </summary>
    void AssignCase(ClinicalCaseId caseId, PersonalityId personality);

    /// <summary>
    /// NO DEBE lanzar en ningún estado. Con <see cref="IsReady"/> false devuelve
    /// <see cref="ClinicalResponse.NoAplica"/>. Cuando <c>Handled == true</c>,
    /// <c>Reply.Text</c> NO DEBE ser vacío y <c>Reply.EmotionTag</c>/<c>AnimationCue</c>
    /// NO DEBEN ser null. DEBE ser determinista en <c>Handled</c> y en <c>Reply.Text</c>
    /// para la misma (caso, personalidad, utterance, intent).
    /// </summary>
    ClinicalResponse Respond(Utterance nurseUtterance, IntentResult intent);
}
```

## Testing Strategy

| Archivo | Qué fija |
|---|---|
| `ClinicalResponderContract` (nuevo) | Invariantes observables del puerto: `IsReady` no lanza; sin `AssignCase` ⇒ `Respond` devuelve `NoAplica`; `Respond` nunca lanza (utterance vacío, `IntentResult` default, texto raro, cadena larga); cuando `Handled` ⇒ `Reply.Text` no vacío y tags no null; determinismo de `Handled` y `Reply.Text` para la misma entrada; `AssignCase` idempotente; `AssignCase` con id desconocido no lanza y deja `IsReady == false` |
| Stub local no-listo | Dentro de `ClinicalResponderContract.cs` (patrón `ClasificadorNoListo` de `IntentClassifierContract`): `IsReady => false`, `Respond => ClinicalResponse.NoAplica`. Alcanza el camino no-listo sin depender del doble de M15 |
| `ContractTypeTests` (aditivo) | `ClinicalCaseId`: `new(" Caso-01 ")` normaliza a `"caso-01"`; `None.IsNone`; igualdad `Ordinal`; `GetHashCode` de `None` es `0`. `ClinicalResponse.NoAplica.Handled == false` |
| `ContractVersionChangelogTests` (sin cambio) | Ata `Contract.Version == 2` al mayor `## v<N>` del changelog |
| `CoreAssemblyPurityTests` (sin cambio) | Los tipos nuevos no meten `UnityEngine*` ni `NpcAi.*` ajeno en el binario de `NpcAi.Core` |

Ejecución EditMode: Test Runner de Unity 6, igual que el resto del repo. Ningún agente
ejecuta Unity: el verde es compuerta humana.

## Migration / Rollout

Sin migración de datos. `Contract.Version 1 → 2` es aditivo: un consumidor v1 recompilado
contra v2 sigue compilando (no se quitó ni renombró nada). Los módulos que hoy no usan
`IClinicalResponder` (todos) no se enteran del cambio.

Rollback: revertir los commits deja `Contract.Version == 1`, sin `## v2`, sin el puerto ni
los DTO. `ContractVersionChangelogTests` vuelve a verde con `Version == 1`.

## Open Questions

- [ ] ¿`Respond` recibe `Utterance` (texto + confianza + duración) o bastaría `string`? Se
      elige `Utterance` por simetría con lo que ya produce M1 y para que M15 pueda usar la
      confianza de transcripción si le sirve. Confirmar al implementar M15.
- [ ] ¿`IClinicalResponder` necesita un `Reset()` sin argumentos (volver a "sin caso") para
      reusar la instancia entre sesiones, o M11 crea una instancia nueva por sesión? Se
      decide en `2026-09-09-m11-armado-sesion`; si hace falta, agregarlo es cambio de
      contrato v3 acotado.
- [ ] ¿El canal `EventChannel<ClinicalResponse>` se agrega cuando M11 defina el cableado?
      Depende de si el enrutado se hace por Inspector o por composición en código.
