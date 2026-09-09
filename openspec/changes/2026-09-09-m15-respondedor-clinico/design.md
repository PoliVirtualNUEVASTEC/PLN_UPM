# Design: M15 — Respondedor clínico

## Technical Approach

Adaptador nuevo (`ClinicalResponder`) detrás de `IClinicalResponder` (puerto que agrega
`2026-09-09-m0-puerto-respuesta-clinica`). Recuperación por **tabla de hechos**: el bloque
`hechos` de cada caso de M14 mapea `ejemplosDePregunta` → `respuesta`. `ClinicalResponder`
normaliza lo que dijo la enfermera, busca el `campo` que mejor empareja y devuelve su
`respuesta` con un matiz de personalidad. Si nada empareja, `ClinicalResponse.NoAplica`.

### Unidad de módulo y grafo de referencias

    NpcAi.ClinicalResponse -> [NpcAi.Core]

Un solo ensamblado. Referencia **solo `NpcAi.Core`** (no `NpcAi.Core.Channels`: la primera
entrega no se cablea por Inspector; no `NpcAi.Nlu` ni ningún otro módulo — regla 3 del
repo). El núcleo lógico (`ClinicalCaseLoader`, `ClinicalFactMatcher`) trabaja sobre
`string`, así que se prueba sin Unity; el `.asmdef` puede quedar con
`noEngineReferences: true` si la carga de bytes se delega a un proveedor inyectado (AD3).

## Architecture Decisions

| # | Decisión | Elección | Alternativas rechazadas | Razón |
|---|---|---|---|---|
| AD1 | Técnica de respuesta | Tabla de hechos + emparejador de texto propio | Modelo entrenado; reusar `SemanticMatcher` de M2 | El contrato exige determinismo y "no inventar hechos"; una tabla explícita lo da. `SemanticMatcher` vive en `NpcAi.Nlu` y M15 no puede referenciarlo. El normalizador de M15 es ~30 líneas |
| AD2 | Estilo de la respuesta | `respuesta` del caso + prefijo/sufijo fijo por personalidad | Redactar con plantillas ricas; delegar a M6 | El dato debe sobrevivir textualmente (AD5); delegar a M6 lo expondría a la variación de Markov. El matiz mínimo mantiene a M15 lejos de "ser un segundo M6" |
| AD3 | De dónde salen los bytes de `Data/Cases/` | `ClinicalCaseLoader(string json)` para el núcleo; la obtención del `string` la hace `ClinicalResponder` vía un `Func<ClinicalCaseId,string>` inyectado por constructor (M11 lo provee: `Resources.Load<TextAsset>` o lectura de `Packages/...`) | `ClinicalResponder` hace `Resources.Load` directo (ata el asmdef a Unity y a una convención de carpeta); vendorizar los JSON en el código | El proveedor inyectado deja el asmdef puro y el núcleo 100% probable con JSON de prueba; M11 decide la estrategia de carga real según plataforma (Editor vs. Quest) sin tocar M15 |
| AD4 | `ClinicalCase` incluye `clave` | No | Mapear todo el JSON | `clave` (triaje esperado, banderas) es dato de evaluación de M9. Que `ClinicalCase` ni lo tenga hace estructuralmente imposible que M15 lo filtre. Prueba dedicada lo confirma |
| AD5 | Garantía sobre el dato | `Reply.Text.Contains(hecho.respuesta)` siempre verdadero cuando `Handled` | Solo "el texto es no vacío" | El propósito de M15 es que el hecho llegue intacto; el contrato del puerto solo exige "no vacío", así que este requisito extra vive en la spec de M15 y en su prueba |
| AD6 | `EmotionTag`/`AnimationCue` | Tabla fija por defecto (`"neutral"` / `"idle"`), opcionalmente por `campo` del hecho (p. ej. `dolor` → `"gesto_dolor"`) | Derivar del texto; generarlos | Son señales de control para M8; fijas preservan el comportamiento ya validado en los dobles de M6/M8 |
| AD7 | `Respond` y `Receptivity` | `Respond` no recibe `Receptivity` (coincide con el puerto de M0) | Pasar `Receptivity` para respuestas evasivas si `NoReceptivo` | Un hecho no cambia con el humor del paciente. "Paciente `NoReceptivo` que no coopera" es política de enrutado (M11 puede mandar el turno a M6) o extensión v3, no esta entrega |
| AD8 | Sin coincidencia parcial ambigua | Si 2 `campo` empatan en cobertura, gana el de menor índice en la lista `hechos` (orden del archivo) | Elegir al azar; devolver ambos | Determinismo. El orden del archivo es estable y editable por quien redacta el caso |

## Data Flow

    M11 arma la sesión → new ClinicalResponder(cargarJson)  (cargarJson: ClinicalCaseId → string)
              │
              ▼
    responder.AssignCase(caseId, personalityId)
              │  cargarJson(caseId) → JSON → ClinicalCaseLoader.Parse → ClinicalCase
              │  (si el JSON no existe o no valida: IsReady = false, sin lanzar)
              ▼
    responder.Respond(utterance, intent)          ← por turno
              │
              ▼
    ClinicalFactMatcher.Match(normalizar(utterance.Text), case.Hechos)
              │
      ┌───────┴─────────────────────┐
    campo emparejado            sin coincidencia
      │                              │
      ▼                              ▼
    texto = matiz(personality,     ClinicalResponse.NoAplica   → enrutador llama a M6
            hecho.respuesta)
    Reply = new NpcReply(texto, emotionTag(campo), animationCue(campo))
    → new ClinicalResponse(true, Reply)

## File Inventory

| Archivo | Rol |
|---|---|
| `Runtime/ClinicalResponse/NpcAi.ClinicalResponse.asmdef` | Ensamblado; `references: ["NpcAi.Core"]` |
| `Runtime/ClinicalResponse/ClinicalCase.cs` | POCO: `Paciente` (edad, motivo, sintomas[], antecedentes[], alergias[], medicacionActual[], signosVitales), `Hechos` (lista de `Hecho { Campo, EjemplosDePregunta[], Respuesta }`). Sin `Clave` |
| `Runtime/ClinicalResponse/ClinicalCaseLoader.cs` | `static ClinicalCase Parse(string json)`; `static bool TryParse(string json, out ClinicalCase caso)`; valida esquema mínimo |
| `Runtime/ClinicalResponse/ClinicalFactMatcher.cs` | `Normalizar(string)`; `int Match(string textoNormalizado, IReadOnlyList<Hecho> hechos)` → índice o `-1` |
| `Runtime/ClinicalResponse/ClinicalResponder.cs` | `IClinicalResponder`. Constructor `ClinicalResponder(Func<ClinicalCaseId,string> cargarJson)`; `AssignCase`; `Respond`; matiz de personalidad |
| `Runtime/ClinicalResponse/Fakes/ScriptedClinicalResponder.cs` | Doble: tabla de hechos embebida (3–4 entradas), sin carga de archivo, `IsReady` tras `AssignCase` con cualquier id no `None` |
| `Tests/EditMode/ClinicalResponse/NpcAi.ClinicalResponse.Tests.asmdef` | Ref: `NpcAi.Core`, `NpcAi.Core.Tests`, `NpcAi.ClinicalResponse` |
| `Tests/EditMode/ClinicalResponse/ClinicalResponderTests.cs` | `: ClinicalResponderContract`, `CreateSubject()` con un caso de prueba embebido + `AssignCase` |
| `Tests/EditMode/ClinicalResponse/ScriptedClinicalResponderTests.cs` | `: ClinicalResponderContract` sobre el doble |
| `Tests/EditMode/ClinicalResponse/ClinicalFactMatcherTests.cs` | Unidad: normalización, coincidencia esperada por consulta, no-coincidencia con saludo |
| `Tests/EditMode/ClinicalResponse/ClinicalCasesDataTests.cs` | Carga los 3 `Data/Cases/caso-*.json` reales; valida esquema, `id` == archivo, `triajeEsperado` ∈ {I..V}, `signosVitales` completos, `hechos` ≥ mínimo, y `clave` no aparece en ninguna `respuesta` |

## Interfaces / Contracts

```csharp
// Runtime/ClinicalResponse/ClinicalResponder.cs
public sealed class ClinicalResponder : IClinicalResponder
{
    public ClinicalResponder(System.Func<ClinicalCaseId, string> cargarJson);
    public bool IsReady { get; }                                   // true tras AssignCase con un caso que carga y valida
    public void AssignCase(ClinicalCaseId caseId, PersonalityId personality);
    public ClinicalResponse Respond(Utterance nurseUtterance, IntentResult intent);
}

// Runtime/ClinicalResponse/ClinicalCaseLoader.cs
public static class ClinicalCaseLoader
{
    public static bool TryParse(string json, out ClinicalCase caso);  // false si el esquema mínimo no está
}
```

## Testing Strategy

| Archivo | Qué fija |
|---|---|
| `ClinicalResponderContract` (heredada, sin cambios) | Invariantes del puerto: no lanza; sin caso ⇒ `NoAplica`; determinismo de `Handled`/`Text`; `AssignCase` idempotente; id desconocido no lanza y deja no-listo; cuando `Handled` ⇒ `Text` no vacío, tags no null |
| `ClinicalResponderTests`, `ScriptedClinicalResponderTests` | Heredan la base; `CreateSubject` prepara un sujeto con caso asignado |
| `ClinicalFactMatcherTests` | "desde cuando le empezo el dolor" → campo `inicio_sintoma`; "buenos dias" → `-1`; normalización quita tildes/signos; empate resuelve por menor índice |
| `ClinicalCasesDataTests` | Valida el catálogo real de M14 (pruebas trazadas "hacia adelante" desde `catalogo-casos-clinicos-m14`) |
| Prueba manual de determinismo | 1000 llamadas con la misma entrada → mismo `Handled` y `Text` (lo que EditMode no cubre por volumen) |

Ejecución EditMode: Test Runner de Unity 6. El núcleo lógico también corre en el filtro de
prueba sin escena. Ningún agente ejecuta Unity: el verde es compuerta humana.

## Migration / Rollout

Sin migración. `ClinicalResponder` se integra donde M11 arme la sesión, inyectándole el
`cargarJson` adecuado. Hasta entonces, solo lo ejercen sus pruebas.

Rollback: revertir borra `Runtime/ClinicalResponse/` y sus pruebas; el puerto queda sin
implementación real.

## Open Questions

- [ ] ¿`ClinicalCaseLoader` usa `JsonUtility` (limitado con arrays anidados y `null`),
      Newtonsoft (`com.unity.nuget.newtonsoft-json`, hay que declararlo), o un mini-parser?
      Se resuelve en el spike de PR1 probando `JsonUtility` contra el esquema real de M14.
- [ ] ¿El umbral de "cuántas palabras de la pregunta deben cubrir un `ejemploDePregunta`"
      para contar como coincidencia? Arrancar en "todas las palabras de contenido de algún
      `ejemploDePregunta` aparecen en la pregunta" y ajustar con `ClinicalFactMatcherTests`.
- [ ] ¿La estrategia de carga real (`Resources/` vs. ruta de `Packages/`) la fija M11 o la
      documenta M15 como recomendación? Propuesta: M11 la fija; M15 solo expone el `Func`.
