# Design: M16 — Catálogo y respondedor de requerimientos de sala de juntas

## Technical Approach

Adaptador nuevo detrás de un puerto **ya mergeado**. `IRequirementResponder`,
`RequirementOutcome`, `RequirementResponse`, `RequirementCaseId` y `RequirementId` viven en
`NpcAi.Core` desde el contrato v3 (`2026-09-16-m0-puerto-requerimientos-juntas`, PRs #39-#42).
M16 no toca ni un byte de ellos: implementa la costura.

Forma general = **espejo de M15** (`Runtime/ClinicalResponse/`): POCO de caso, cargador
`string → caso`, emparejador de texto propio, adaptador que junta todo, doble determinista en
`Fakes/`, datos versionados en `Data/`. Dos piezas que M15 **no** tiene, y que son la razón de
ser de M16:

1. **`RequirementDisclosurePolicy`** — la puerta de receptividad. M15 responde o no responde;
   M16 responde, desvía o no aplica.
2. **`PersonalityStyleBank`** — banco de estilo por personalidad (prefijo de revelación +
   frases de desvío) cargado de `Data/`, no una tabla `switch` en C# (AD6).

### Unidad de módulo y grafo de referencias (regla `rules.design`)

    NpcAi.RequirementResponse -> [NpcAi.Core]

Un solo ensamblado de runtime, un solo módulo (regla 1 del repo). Referencia **solo
`NpcAi.Core`**: no `NpcAi.Core.Channels` (nada se cablea por Inspector en esta entrega, AD8 de
M0 sigue vigente), no `NpcAi.Nlu`, no `NpcAi.ClinicalResponse` (regla 3). `noEngineReferences:
false`, igual que `NpcAi.ClinicalResponse`: `RequirementCaseLoader` y `PersonalityStyleBank`
usan `UnityEngine.JsonUtility`. El resto del módulo es C# puro.

### Gotcha bloqueante: colisión de nombres `RequirementResponse`

El namespace del módulo (`NpcAi.RequirementResponse`) choca con el nombre del DTO
(`NpcAi.Core.RequirementResponse`) — **exactamente** el problema que `ClinicalResponder.cs:47-49`
documenta para `NpcAi.ClinicalResponse` y que `NpcAi.Receptivity` ya tuvo con
`Core.Receptivity`. Toda firma y todo `return` que mencione el DTO dentro del módulo **debe**
escribirse `Core.RequirementResponse`. Vale para `RequirementResponder.cs` y para
`Fakes/ScriptedRequirementResponder.cs`. No es opcional: sin el prefijo, no compila.

## Architecture Decisions

| # | Decisión | Elección | Alternativas rechazadas | Razón |
|---|---|---|---|---|
| AD1 | Serialización de `receptividadMinima` | **`string`** en el JSON (`"NoReceptivo"` / `"Neutral"` / `"Receptivo"`), campo `public string` en la clase `Raw*`, mapeo explícito a `Receptivity` con un `switch` en el cargador | `int` crudo (`-1`/`0`/`1`); campo tipado `Receptivity` directo en la clase `Raw*` | Cierra la pregunta que M15/AD3 dejó abierta. `JsonUtility` mapea enums **por valor numérico**, no por nombre: un `"Receptivo"` contra un campo `Receptivity` deja el valor en `0` (`Neutral`) **en silencio** — o sea, afloja o endurece la puerta sin que nada falle. `int` crudo evita el bug pero vuelve el catálogo ilegible para quien lo redacta (y `-1` como "el nivel más bajo" es contraintuitivo). Con `string`, `JsonUtility` no interviene en la semántica y el valor desconocido es detectable. Mismo tratamiento y misma razón que `temperaturaC` en M14 (`number\|null` → `string\|null`) |
| AD2 | Valor de `receptividadMinima` inválido o ausente | Se **descarta ese requerimiento** en el cargador (no se aborta el caso); si tras descartar quedan menos de `MinimoRequerimientos`, `TryParse` devuelve `false` | Mapear a `Neutral` por defecto; abortar el caso entero | Copia literal del `Where(...)` de `ClinicalCaseLoader` (descarta hechos incompletos, rechaza si no llega al mínimo). Fail-closed real: un requerimiento con la puerta rota **no existe**, así que es estructuralmente imposible que se revele. Un default a `Neutral` sería la versión silenciosa del bug que AD1 evita |
| AD3 | Cobertura de los 3 niveles | La valida `RequirementCasesDataTests`, **no** el cargador | Invariante del cargador, junto al mínimo de requerimientos | Es lo que pide literalmente la decisión #66 ("regla de validación de datos análoga a `ClinicalCasesDataTests`"). Además el modo de falla es el correcto: un caso sin los 3 niveles **falla una prueba con nombre**, en vez de dejar `IsReady == false` y degradar todo a `NoAplica` sin decir por qué |
| AD4 | Dónde vive la puerta de receptividad | Clase propia `RequirementDisclosurePolicy`, estática, que decide **solo** entre `Revelado` y `AunNoRevelado` | Un `if` dentro de `RequirementResponder.Respond`; que la política también devuelva `NoAplica` | Es la pieza nueva frente a M15: separada, se prueba con las 9 combinaciones (3 niveles × 3 receptividades) sin montar un responder ni un caso. Que **nunca** devuelva `NoAplica` es deliberado: "no aplica" es ausencia de match o de caso, responsabilidad del adaptador, no de la puerta |
| AD5 | `Intent` no filtra | `Respond` **ignora** el parámetro `intent`; el turno aplica si y solo si el texto empareja | `PreguntaFueraDeTema` → `NoAplica` sin consultar la tabla; exigir `AportaInformacion`/`SolicitudRespetuosa` | Decisión cerrada de negocio (mismo criterio que M15). Verificado en `juntas.json`: la misma pregunta aparece etiquetada como `AportaInformacion`, `SolicitudRespetuosa` y `SolicitudAgresiva` según el tono — filtrar por `Intent` haría que la puerta dependiera del humor del estudiante, no de su receptividad ganada. El parámetro queda en la firma porque es del contrato v3; el XML doc del módulo dice explícitamente que no se usa, para que ninguna revisión lo lea como olvido |
| AD6 | Origen del desvío y del matiz de personalidad | `Data/Requirements/matices.json`: 4 entradas (`grosero`, `histerico`, `introvertido`, `empatico`), cada una con `prefijoRevelado` y `desvios[]`. Lo carga `PersonalityStyleBank` | Tabla `switch` en `RequirementResponder` (lo que hizo M15 en `ConMatiz`); un `desvio` por requerimiento dentro de cada caso | Decisión cerrada: el desvío es **banco genérico por `PersonalityId`**, no por requerimiento. Rule 7 del `CLAUDE.md` del paquete es literal: "agregar una personalidad no debe cambiar ni una clase" — y el desvío es una "respuesta base". Divergencia consciente frente a M15 (ver R6). Beneficio estructural gratis: el banco **nunca ve** `requerimiento.Respuesta`, así que "el desvío filtra el hecho" es imposible por construcción, no por prueba (mismo razonamiento que AD4 de M15 con el bloque `clave`) |
| AD7 | Selección determinista dentro del banco de desvíos | `desvios[indiceDelRequerimientoEnLaTabla % desvios.Count]` | Una sola frase por personalidad; `Random`; hash de `RequirementId.Value` | Da variedad entre requerimientos sin memoria de turno y sin romper el determinismo que exige `Es_determinista_en_Outcome_texto_y_RequirementId_para_la_misma_entrada`. Se descarta el hash de string: `string.GetHashCode()` no está garantizado estable entre runtimes (Mono vs. IL2CPP), y el índice de la tabla ya es estable y editable por quien redacta el caso — misma lógica que el desempate por menor índice de AD8 de M15 |
| AD8 | `emotionTag` / `animationCue` | Campos **opcionales** del JSON por requerimiento, con defaults `"neutral"` / `"idle"` en el cargador | Tabla `switch` por id de requerimiento (lo que hizo M15/AD6) | Los ids de requerimiento son libres y por caso (`equipos`, `presupuesto`, `sedes`...): un `switch` en C# obligaría a tocar una clase cada vez que un caso nuevo quiera un gesto propio, contra el criterio de éxito "agregar un caso no requiere tocar ninguna clase C#". Siguen siendo tabla fija (nunca derivados del texto), solo que la tabla es dato |
| AD9 | De dónde salen los bytes de `Data/Requirements/` | `RequirementResponder(Func<RequirementCaseId,string> cargarJson, string maticesJson)`: el núcleo recibe texto, el IO queda afuera | `Resources.Load` dentro del módulo; vendorizar los JSON en código | Copia literal de AD3 de M15. Deja el módulo probable con JSON en memoria y deja que quien arme la sesión (M10/M11) elija la estrategia por plataforma (Editor vs. Quest). La pregunta "cuál estrategia" sigue abierta y **no es de M16** (ver R4) |
| AD10 | `matices.json` inválido, ausente o sin la personalidad asignada | Fallback en código a un prefijo vacío y **un** desvío neutro constante | Dejar `Reply.Text` vacío; lanzar | El puerto exige que con `AunNoRevelado` `Reply.Text` NO sea vacío ni solo espacios. El fallback no es "contenido variable" (que sería dato por regla 7): es la garantía del contrato, y por eso vive en código, en una sola línea, documentada como tal |
| AD11 | `RequirementMatcher` duplica `ClinicalFactMatcher` | Se acepta la copia adaptada (~70 líneas) dentro de `NpcAi.RequirementResponse` | Extraer el normalizador a `NpcAi.Core`; referenciar `NpcAi.ClinicalResponse` | Referenciar M15 viola la regla 3 del repo. Mover el normalizador a `NpcAi.Core` sería un **cambio de contrato v4** con co-revisión obligatoria (regla 2) — desproporcionado para 70 líneas y fuera del alcance de este cambio. Costo aceptado y registrado en R5 |

## Data Flow

    (armador de sesión de sala de juntas — M10/M11, NO es de este cambio)
              │  elige RequirementCaseId + PersonalityId + provee cargarJson y maticesJson
              ▼
    new RequirementResponder(cargarJson, maticesJson)
              │   PersonalityStyleBank.TryParse(maticesJson) → banco  (si falla: fallback AD10)
              ▼
    responder.AssignCase(caseId, personality)             ← una vez por sesión
              │   cargarJson(caseId) → JSON → RequirementCaseLoader.TryParse → RequirementCase
              │   (si no existe o no valida: IsReady = false, sin lanzar)
              ▼
    responder.Respond(utterance, intent, receptivity)     ← por turno; `intent` se ignora (AD5)
              │
              ▼
    RequirementMatcher.Match(Normalizar(utterance.Text), caso.Requerimientos) → índice | -1
              │
      ┌───────┴──────────────────────────────┐
    índice >= 0                          índice == -1  (o !IsReady)
      │                                        │
      ▼                                        ▼
    RequirementDisclosurePolicy.Decidir(          Core.RequirementResponse.NoAplica
        receptivity, req.ReceptividadMinima)      (RequirementId.None)  → enrutador llama a M6
      │
      ├── Revelado ──────► texto = banco.PrefijoRevelado(personalidad) + req.Respuesta
      │                    (req.Respuesta queda INTACTA como subcadena)
      │
      └── AunNoRevelado ─► texto = banco.Desvio(personalidad, índice)
                           (el banco no conoce req.Respuesta: no puede filtrarla)
      │
      ▼
    new Core.RequirementResponse(outcome,
        new NpcReply(texto, req.EmotionTag, req.AnimationCue),
        new RequirementId(req.Id))

## File Inventory

| Archivo | Acción | Rol |
|---|---|---|
| `Runtime/RequirementResponse/NpcAi.RequirementResponse.asmdef` | Crear | `references: ["NpcAi.Core"]`, `noEngineReferences: false` |
| `Runtime/RequirementResponse/RequirementCase.cs` | Crear | POCOs `RequirementCase`, `Cliente`, `Requerimiento` |
| `Runtime/RequirementResponse/RequirementCaseLoader.cs` | Crear | `TryParse(string, out RequirementCase)` + clases `Raw*` + mapeo de `receptividadMinima` |
| `Runtime/RequirementResponse/RequirementMatcher.cs` | Crear | `Normalizar`, `Match` → índice o `-1` |
| `Runtime/RequirementResponse/RequirementDisclosurePolicy.cs` | Crear | `Decidir(Receptivity actual, Receptivity minima)` → `Revelado` \| `AunNoRevelado` |
| `Runtime/RequirementResponse/PersonalityStyleBank.cs` | Crear | `TryParse(string, out PersonalityStyleBank)`, `PrefijoRevelado`, `Desvio` + clases `Raw*` |
| `Runtime/RequirementResponse/RequirementResponder.cs` | Crear | `IRequirementResponder` real |
| `Runtime/RequirementResponse/Fakes/ScriptedRequirementResponder.cs` | Crear | Doble determinista, tabla embebida, sin IO (regla 4) |
| `Data/Requirements/README.md` | Crear | Esquema, reglas de archivo, reglas de redacción de `ejemplosDePregunta` |
| `Data/Requirements/matices.json` | Crear | Banco de estilo: 4 personalidades |
| `Data/Requirements/caso-juntas-01..04.json` | Crear | Los 4 dominios de `Data/Corpus/juntas.json` |
| `Tests/EditMode/RequirementResponse/NpcAi.RequirementResponse.Tests.asmdef` | Crear | Ref: `NpcAi.Core`, `NpcAi.Core.Tests`, `NpcAi.RequirementResponse`, TestRunner; `includePlatforms: ["Editor"]` |
| `Tests/EditMode/RequirementResponse/RequirementCaseLoaderTests.cs` | Crear | Unidad del cargador (ver R6) |
| `Tests/EditMode/RequirementResponse/RequirementMatcherTests.cs` | Crear | Normalización, match, no-match, desempate |
| `Tests/EditMode/RequirementResponse/RequirementDisclosurePolicyTests.cs` | Crear | 3 niveles × 3 receptividades |
| `Tests/EditMode/RequirementResponse/PersonalityStyleBankTests.cs` | Crear | Carga, rotación determinista, fallback (ver R6) |
| `Tests/EditMode/RequirementResponse/RequirementResponderTests.cs` | Crear | `: RequirementResponderContract` sobre la implementación real |
| `Tests/EditMode/RequirementResponse/ScriptedRequirementResponderTests.cs` | Crear | `: RequirementResponderContract` sobre el doble |
| `Tests/EditMode/RequirementResponse/RequirementCasesDataTests.cs` | Crear | Valida los 4 `.json` reales contra el esquema y las invariantes |
| `Docs/MODULES.md` | Modificar | Sección M16 |
| `Runtime/Core/`, `Tests/EditMode/Core/` | Sin cambio | Contrato v3 intacto; la base de contrato se hereda, no se toca |

Los `.meta` de todo archivo y carpeta nuevos **se versionan** (convención vigente del repo).

## Interfaces / Contracts

### `Runtime/RequirementResponse/RequirementCase.cs`

```csharp
using System;
using System.Collections.Generic;
using NpcAi.Core;

namespace NpcAi.RequirementResponse
{
    /// <summary>
    /// Caso de sala de juntas ya cargado y validado. Espejo del esquema de
    /// <c>Data/Requirements/README.md</c>. El NPC es el CLIENTE: ya tiene todos los
    /// requerimientos "en la cabeza" y los suelta segun la receptividad ganada.
    /// </summary>
    public sealed class RequirementCase
    {
        public string Id { get; }
        public Cliente Cliente { get; }
        public IReadOnlyList<Requerimiento> Requerimientos { get; }

        public RequirementCase(string id, Cliente cliente, IReadOnlyList<Requerimiento> requerimientos)
        {
            Id             = id ?? string.Empty;
            Cliente        = cliente;
            Requerimientos = requerimientos ?? Array.Empty<Requerimiento>();
        }
    }

    /// <summary>
    /// Contexto narrativo del cliente. Lo consume quien redacta el caso y las pruebas de
    /// datos; <c>RequirementResponder.Respond</c> NO lo consulta (mismo rol que
    /// <c>Paciente</c> en M15).
    /// </summary>
    public sealed class Cliente
    {
        public string Empresa { get; }
        public string Rol { get; }
        public string Proyecto { get; }
        public string Contexto { get; }

        public Cliente(string empresa, string rol, string proyecto, string contexto)
        {
            Empresa  = empresa  ?? string.Empty;
            Rol      = rol      ?? string.Empty;
            Proyecto = proyecto ?? string.Empty;
            Contexto = contexto ?? string.Empty;
        }
    }

    /// <summary>
    /// Una entrada de la tabla de requerimientos: el contrato dato &lt;-&gt; codigo de M16.
    /// <see cref="ReceptividadMinima"/> es la puerta (Opcion B: el umbral es POR
    /// requerimiento, no por caso ni por personalidad).
    /// </summary>
    public sealed class Requerimiento
    {
        public string Id { get; }
        public Receptivity ReceptividadMinima { get; }
        public IReadOnlyList<string> EjemplosDePregunta { get; }
        public string Respuesta { get; }
        public string EmotionTag { get; }
        public string AnimationCue { get; }

        public Requerimiento(
            string id, Receptivity receptividadMinima, IReadOnlyList<string> ejemplosDePregunta,
            string respuesta, string emotionTag, string animationCue)
        {
            Id                 = id ?? string.Empty;
            ReceptividadMinima = receptividadMinima;
            EjemplosDePregunta = ejemplosDePregunta ?? Array.Empty<string>();
            Respuesta          = respuesta ?? string.Empty;
            EmotionTag         = string.IsNullOrWhiteSpace(emotionTag)   ? "neutral" : emotionTag;
            AnimationCue       = string.IsNullOrWhiteSpace(animationCue) ? "idle"    : animationCue;
        }
    }
}
```

### `Runtime/RequirementResponse/RequirementCaseLoader.cs` (piezas mecánicas)

```csharp
public static class RequirementCaseLoader
{
    private const int MinimoRequerimientos = 6;

    public static bool TryParse(string json, out RequirementCase caso);

    /// <summary>
    /// AD1: el JSON trae el nivel por NOMBRE porque JsonUtility mapea enums por valor
    /// numerico, no por nombre — un "Receptivo" contra un campo Receptivity quedaria en 0
    /// (Neutral) en silencio. Comparacion Ordinal, sin cultura.
    /// </summary>
    private static bool TryMapearReceptividad(string nombre, out Receptivity nivel) =>
        // "NoReceptivo" -> Receptivity.NoReceptivo; "Neutral" -> Neutral;
        // "Receptivo" -> Receptivo; cualquier otra cosa (o null) -> false (AD2)
        ...;

    // Espejo exacto del esquema de Data/Requirements/README.md. Nombres en minusculas a
    // proposito: JsonUtility empareja por nombre exacto. [Serializable] es obligatorio en
    // toda clase anidada dentro de otra.
    [Serializable] private sealed class RawCase
    {
        public string id;
        public RawCliente cliente;
        public RawRequerimiento[] requerimientos;
    }

    [Serializable] private sealed class RawCliente
    {
        public string empresa;
        public string rol;
        public string proyecto;
        public string contexto;
    }

    [Serializable] private sealed class RawRequerimiento
    {
        public string   id;
        public string   receptividadMinima;   // "NoReceptivo" | "Neutral" | "Receptivo"  (AD1)
        public string[] ejemplosDePregunta;
        public string   respuesta;
        public string   emotionTag;           // opcional; default "neutral"  (AD8)
        public string   animationCue;         // opcional; default "idle"     (AD8)
    }
}
```

El cuerpo de `TryParse` copia la forma de `ClinicalCaseLoader.TryParse`: `try/catch` alrededor
de `JsonUtility.FromJson<RawCase>`, rechazo si `raw == null` o `id` vacío, `Where(...)` que
descarta requerimientos incompletos o con `receptividadMinima` inválida (AD2), y
`if (requerimientos.Count < MinimoRequerimientos) return false;`.

### `Runtime/RequirementResponse/RequirementDisclosurePolicy.cs`

```csharp
public static class RequirementDisclosurePolicy
{
    /// <summary>
    /// La puerta. Aprovecha que Receptivity esta numerado -1/0/1 a proposito (ver el
    /// comentario del enum en Runtime/Core/Enums.cs): el orden es comparable.
    /// NUNCA devuelve NoAplica — eso es ausencia de match o de caso, y lo decide
    /// RequirementResponder (AD4).
    /// </summary>
    public static RequirementOutcome Decidir(Receptivity actual, Receptivity minima) =>
        (int)actual >= (int)minima
            ? RequirementOutcome.Revelado
            : RequirementOutcome.AunNoRevelado;
}
```

### `Runtime/RequirementResponse/RequirementMatcher.cs`

Copia adaptada de `ClinicalFactMatcher` (AD11): `Normalizar` idéntica (minúsculas,
`FormD` + descarte de `NonSpacingMark`, no alfanumérico → espacio, colapso de espacios,
`ToLowerInvariant`) y

```csharp
/// <summary>
/// Indice del primer Requerimiento cuyos EjemplosDePregunta quedan completamente cubiertos
/// por las palabras del texto normalizado, o -1. Recorrer en orden resuelve el empate por
/// menor indice por construccion: determinismo sin Random y sin orden de Dictionary.
/// </summary>
public static int Match(string textoNormalizado, IReadOnlyList<Requerimiento> requerimientos);
```

### `Runtime/RequirementResponse/PersonalityStyleBank.cs`

```csharp
public sealed class PersonalityStyleBank
{
    public static bool TryParse(string json, out PersonalityStyleBank banco);

    /// <summary>Prefijo fijo por personalidad; "" para None/desconocida (AD10).</summary>
    public string PrefijoRevelado(PersonalityId personalidad);

    /// <summary>
    /// Desvio determinista: desvios[indiceRequerimiento % desvios.Count] (AD7). NUNCA recibe
    /// ni conoce el texto del requerimiento, asi que no puede filtrarlo (AD6).
    /// </summary>
    public string Desvio(PersonalityId personalidad, int indiceRequerimiento);

    [Serializable] private sealed class RawBanco   { public RawMatiz[] matices; }
    [Serializable] private sealed class RawMatiz
    {
        public string   personalidad;      // "grosero" | "histerico" | "introvertido" | "empatico"
        public string   prefijoRevelado;
        public string[] desvios;
    }
}
```

### `Runtime/RequirementResponse/RequirementResponder.cs`

```csharp
public sealed class RequirementResponder : IRequirementResponder
{
    /// <param name="cargarJson">De un RequirementCaseId al JSON del caso, o null. Puede ser
    /// null: AssignCase nunca lanza sin importar como se construyo (AD9).</param>
    /// <param name="maticesJson">Contenido de Data/Requirements/matices.json. Si es null o no
    /// valida, se usa el banco de fallback (AD10).</param>
    public RequirementResponder(Func<RequirementCaseId, string> cargarJson, string maticesJson);

    public bool IsReady { get; }
    public void AssignCase(RequirementCaseId caseId, PersonalityId personality);

    // "Core." es obligatorio: NpcAi.RequirementResponse choca con NpcAi.Core.RequirementResponse.
    // El parametro intent NO se usa (AD5): el turno aplica si y solo si el texto empareja.
    public Core.RequirementResponse Respond(
        Utterance studentUtterance, IntentResult intent, Receptivity receptivity);
}
```

### `Runtime/RequirementResponse/Fakes/ScriptedRequirementResponder.cs`

Doble determinista, C# puro, **sin** `UnityEngine` y **sin** IO: tabla de 6 `Requerimiento`
embebidos que cubre los 3 niveles, prefijos y desvíos constantes. Reconoce como "casos
existentes" los mismos 4 ids del catálogo real (`caso-juntas-01..04`); cualquier otro id
—incluido `"no-existe"`, que usa la base de contrato— deja `IsReady` en `false`. Reutiliza
`RequirementMatcher` y `RequirementDisclosurePolicy` (mismo criterio que
`ScriptedClinicalResponder`, que reutiliza `ClinicalFactMatcher`), así la paridad doble/real no
depende de replicar lógica a mano.

## Esquema del catálogo — `Data/Requirements/`

### Reglas de archivo

- **Un caso == un archivo, nombre de archivo == `id`.** `caso-juntas-01.json` tiene
  `"id": "caso-juntas-01"` (misma regla que M5 y M14).
- El descubrimiento de casos filtra por el prefijo **`caso-juntas-`**: `matices.json` vive en
  la misma carpeta y NO es un caso.
- Redacción de `ejemplosDePregunta`: mínimo 2 por requerimiento, **mínimo 2 palabras de
  contenido cada uno** y específicos del tema. El matcher declara coincidencia cuando *todas*
  las palabras de *algún* ejemplo aparecen en la pregunta; un ejemplo genérico como
  `"el proyecto"` emparejaría casi cualquier frase y rompería el caso de control
  (`"que clima hace hoy"` DEBE dar `NoAplica`).

### `caso-juntas-01.json` — ejemplo completo (patrón a replicar en 02-04)

Dominio: torneo de fútbol, transcrito de `Data/Corpus/juntas.json`. 7 requerimientos:
**2 en `NoReceptivo`, 2 en `Neutral`, 3 en `Receptivo`** (cumple la decisión #66: mínimo 1 por
nivel).

```json
{
  "id": "caso-juntas-01",
  "cliente": {
    "empresa": "Liga municipal de futbol",
    "rol": "Coordinador del torneo",
    "proyecto": "Sistema de gestion del torneo",
    "contexto": "Hoy llevan todo en cuadernos y hojas de calculo sueltas."
  },
  "requerimientos": [
    {
      "id": "equipos",
      "receptividadMinima": "NoReceptivo",
      "ejemplosDePregunta": ["como identifican los equipos", "que datos manejan de cada equipo"],
      "respuesta": "Cada equipo se registra con su nombre y el ano de fundacion."
    },
    {
      "id": "jugadores",
      "receptividadMinima": "NoReceptivo",
      "ejemplosDePregunta": ["que datos manejan de cada jugador", "como registran a los jugadores"],
      "respuesta": "De cada jugador guardamos documento, nombre y posicion."
    },
    {
      "id": "partidos",
      "receptividadMinima": "Neutral",
      "ejemplosDePregunta": ["como se arma un partido", "quien juega de local y de visitante"],
      "respuesta": "Un partido enfrenta a dos equipos, uno local y uno visitante, en un estadio."
    },
    {
      "id": "arbitros",
      "receptividadMinima": "Neutral",
      "ejemplosDePregunta": ["como asignan los arbitros", "que datos manejan del arbitro"],
      "respuesta": "Cada arbitro tiene codigo de licencia y nombre, y puede pitar varios partidos.",
      "emotionTag": "neutral",
      "animationCue": "asentir"
    },
    {
      "id": "estadisticas",
      "receptividadMinima": "Receptivo",
      "ejemplosDePregunta": ["como llevan el rendimiento", "como registran goles y tarjetas"],
      "respuesta": "Registramos goles y tarjetas por jugador y por partido."
    },
    {
      "id": "calendario",
      "receptividadMinima": "Receptivo",
      "ejemplosDePregunta": ["como se arma el calendario", "como programan las fechas"],
      "respuesta": "El calendario se arma por fechas, y cada fecha agrupa varios partidos."
    },
    {
      "id": "presupuesto",
      "receptividadMinima": "Receptivo",
      "ejemplosDePregunta": ["presupuesto del proyecto", "cuanto piensan invertir"],
      "respuesta": "El presupuesto del proyecto es de doscientos millones de pesos.",
      "emotionTag": "reservado",
      "animationCue": "cruzar_brazos"
    }
  ]
}
```

`presupuesto` no sale de la narrativa de `juntas.json`: está para que la frase de prueba de
`RequirementResponderContract` empareje y los casos 4-9 de la base de contrato corran verdes en
vez de quedar omitidos por `Assume`. **Ver R1** — no es una decisión que tome este diseño.

Campo por campo:

| Campo | Tipo | Notas |
|---|---|---|
| `id` | string | == nombre de archivo sin `.json` |
| `cliente.empresa` / `.rol` / `.proyecto` / `.contexto` | string | Contexto narrativo; `Respond` no lo consulta |
| `requerimientos` | array, **mínimo 6** | Tabla de recuperación + puerta |
| `requerimientos[].id` | string | Estable, minúsculas, sin tildes. Va a `RequirementId` |
| `requerimientos[].receptividadMinima` | string | `"NoReceptivo"` \| `"Neutral"` \| `"Receptivo"` — **texto, no número ni enum** (AD1) |
| `requerimientos[].ejemplosDePregunta` | string[], mínimo 2 | Ver reglas de redacción arriba |
| `requerimientos[].respuesta` | string | En primera persona del cliente. Sale **intacta** cuando `Revelado` |
| `requerimientos[].emotionTag` | string, opcional | Default `"neutral"` (AD8) |
| `requerimientos[].animationCue` | string, opcional | Default `"idle"` (AD8) |

### `matices.json`

```json
{
  "matices": [
    { "personalidad": "grosero",
      "prefijoRevelado": "Ya se lo dije, ",
      "desvios": ["Eso no se lo voy a explicar ahora.", "No estamos para esos detalles."] },
    { "personalidad": "empatico",
      "prefijoRevelado": "Claro, con gusto. ",
      "desvios": ["Prefiero que primero nos conozcamos un poco mas.", "Lleguemos a eso en un momento."] },
    { "personalidad": "histerico",
      "prefijoRevelado": "¡Uf, si! ",
      "desvios": ["¡No, no, eso todavia no!", "¡Ay, espere, eso lo vemos despues!"] },
    { "personalidad": "introvertido",
      "prefijoRevelado": "",
      "desvios": ["Preferiria no hablar de eso todavia.", "Mmm... mas adelante."] }
  ]
}
```

Los 4 ids replican `Data/Personalities/` (M5) y `Data/Dialogue/` (M6). `PersonalityId.None` o
desconocida → prefijo vacío y el desvío de fallback (AD10), mismo criterio que el
`_ => respuesta` de `ClinicalResponder.ConMatiz`.

## Testing Strategy

| Archivo | Qué fija |
|---|---|
| `RequirementResponderContract` (heredada, **sin tocar**) | Los 10 `[Test]` del puerto v3: no lanza en ningún estado × los 3 valores de `Receptivity`; sin caso ⇒ `NoAplica`; `Revelado` ⇒ texto no vacío y tags no `null`; `AunNoRevelado` ⇒ desvío no vacío; `Outcome != NoAplica ⇔ !RequirementId.IsNone`; determinismo triple; baja ≠ alta receptividad; `AssignCase` idempotente; caso desconocido ⇒ `IsReady == false` |
| `RequirementResponderTests : RequirementResponderContract` | Hereda los 10. `CreateSubject()` construye `RequirementResponder` con un `Func` que devuelve un **fixture JSON embebido** de `caso-juntas-01` (≥ 6 requerimientos, 3 niveles, con `presupuesto` en `Receptivo`) y un `matices.json` embebido. Más 3 pruebas propias: (a) con `Revelado`, `Reply.Text` **contiene** `requerimiento.respuesta` intacta; (b) con `AunNoRevelado`, `Reply.Text` **no contiene** `requerimiento.respuesta`; (c) el matiz de personalidad cambia el prefijo pero no el dato |
| `ScriptedRequirementResponderTests : RequirementResponderContract` | Los mismos 10 sobre el doble: paridad real (regla 4 del repo) |
| `RequirementCaseLoaderTests` | Carga válida; JSON vacío/basura ⇒ `false` sin lanzar; `receptividadMinima` en nombre mapea a los 3 valores; `receptividadMinima` desconocida/ausente ⇒ ese requerimiento se descarta (AD2); por debajo del mínimo ⇒ `false`; `emotionTag`/`animationCue` ausentes ⇒ defaults (AD8) |
| `RequirementMatcherTests` | `"como identifican hoy a cada equipo"` → índice de `equipos`; `"que clima hace hoy"` → `-1`; normalización quita tildes y signos; empate resuelve por menor índice |
| `RequirementDisclosurePolicyTests` | Matriz completa 3 × 3 (`receptividadMinima` × `Receptivity`): 6 combinaciones `Revelado`, 3 `AunNoRevelado`; y que `Decidir` nunca devuelve `NoAplica` |
| `PersonalityStyleBankTests` | Carga de las 4 personalidades; rotación determinista (mismo índice ⇒ misma frase, tres llamadas); índice fuera de rango no lanza; personalidad desconocida y `PersonalityId.None` caen en el fallback con texto no vacío (AD10) |
| `RequirementCasesDataTests` | Sobre los `.json` **reales** (patrón `ClinicalCasesDataTests`: `AssetDatabase.FindAssets("t:TextAsset")` filtrado por `Data/Requirements/` + prefijo `caso-juntas-`): (1) están exactamente los 4 casos; (2) cada uno carga y su `id` == nombre de archivo; (3) ≥ 6 requerimientos, cada uno con ≥ 2 `ejemplosDePregunta`; (4) **cada caso tiene ≥ 1 requerimiento en cada uno de los 3 niveles** (decisión #66, AD3); (5) ids de requerimiento únicos dentro del caso; (6) `matices.json` trae las 4 personalidades de M5; (7) ninguna `respuesta` aparece en ningún `desvio` del banco |

Ejecución: Unity 6 Test Runner, EditMode, sin escena y sin VR. `AssetDatabase` obliga a
`includePlatforms: ["Editor"]` en el asmdef de pruebas (igual que M15). **Ningún agente ejecuta
Unity: el verde es compuerta humana.**

Gotchas de prueba heredadas del repo: Unity trae NUnit 3.5 — `Is.AnyOf` (3.6+) no compila y
tumba la assembly entera (M5); usar comparaciones planas. Nada de `Debug.Log` en runtime.

## Threat Matrix

N/A — este cambio no toca enrutado de procesos, comandos de shell, subprocesos, automatización
de VCS/PR, clasificación de archivos ejecutables ni integración de procesos. Es un ensamblado
de C# puro + `JsonUtility` sobre archivos de datos versionados en el repo.

## Migration / Rollout — presupuesto de revisión y corte de PRs

Sin migración de datos: M16 es puramente aditivo sobre un puerto que ya está en `main`.

### Estimación de líneas por pieza

| Pieza | Líneas est. |
|---|---|
| `NpcAi.RequirementResponse.asmdef` | 16 |
| `RequirementCase.cs` | 95 |
| `RequirementCaseLoader.cs` | 110 |
| `RequirementMatcher.cs` | 70 |
| `RequirementDisclosurePolicy.cs` | 35 |
| `PersonalityStyleBank.cs` | 85 |
| `RequirementResponder.cs` | 105 |
| `Fakes/ScriptedRequirementResponder.cs` | 80 |
| **Runtime** | **~596** |
| `Data/Requirements/README.md` | 70 |
| `matices.json` | 35 |
| `caso-juntas-01..04.json` (4 × ~65) | 260 |
| **Datos** | **~365** |
| `NpcAi.RequirementResponse.Tests.asmdef` | 27 |
| `RequirementCaseLoaderTests.cs` | 60 |
| `RequirementMatcherTests.cs` | 70 |
| `RequirementDisclosurePolicyTests.cs` | 60 |
| `PersonalityStyleBankTests.cs` | 55 |
| `RequirementResponderTests.cs` (incluye fixture JSON embebido) | 110 |
| `ScriptedRequirementResponderTests.cs` | 20 |
| `RequirementCasesDataTests.cs` | 140 |
| **Pruebas** | **~542** |
| `Docs/MODULES.md` | 25 |
| **Total autorado** | **~1.528** |

Más ~160 líneas de `.meta` generados por Unity (≈ 20 archivos y carpetas × 8), que aparecen en
el diff pero no son texto autorado.

**≈ 3,8 × el presupuesto de 400 líneas.** Un PR único no es viable.

### Corte propuesto — 4 PRs encadenados

Cada corte es autónomo: compila, tiene sus pruebas en verde por sí mismo, y revertirlo no deja
nada a medias. El encadenamiento sigue el patrón ya usado en M0: PR1 apunta a la rama de la
feature y cada hijo apunta a la rama del PR anterior.

| PR | Rama | Contenido | Est. | Cierre verificable |
|---|---|---|---|---|
| **PR1** | `feat/m16-caso-y-cargador` → rama de la feature | `asmdef`, `RequirementCase.cs`, `RequirementCaseLoader.cs`, `Data/Requirements/README.md`, asmdef de pruebas, `RequirementCaseLoaderTests.cs` | ~378 | El módulo existe y compila; el cargador está probado contra JSON en memoria, sin depender todavía de `Data/` |
| **PR2** | `feat/m16-matcher-puerta-doble` → rama de PR1 | `RequirementMatcher.cs`, `RequirementDisclosurePolicy.cs`, `Fakes/ScriptedRequirementResponder.cs`, `RequirementMatcherTests.cs`, `RequirementDisclosurePolicyTests.cs`, `ScriptedRequirementResponderTests.cs` | ~335 | El doble pasa los 10 `[Test]` de `RequirementResponderContract`: el puerto ya tiene un sujeto real en paridad (regla 4) |
| **PR3** | `feat/m16-responder` → rama de PR2 | `PersonalityStyleBank.cs`, `Data/Requirements/matices.json`, `PersonalityStyleBankTests.cs`, `RequirementResponder.cs`, `RequirementResponderTests.cs` | ~390 | La implementación real pasa los 10 `[Test]` heredados + sus 3 propias (dato intacto / no filtra / matiz no altera) |
| **PR4** | `feat/m16-catalogo` → rama de PR3 | `caso-juntas-01..04.json`, `RequirementCasesDataTests.cs`, `Docs/MODULES.md` | ~425 | Los 4 dominios reales validan contra el esquema y contra la cobertura de los 3 niveles |

PR4 queda ~6% por encima del presupuesto, y 260 de sus 425 líneas son JSON de datos (carga
cognitiva baja por línea). Si el conteo real de `sdd-tasks` lo confirma por encima, el corte
natural es partirlo en `caso-01/02` + `caso-03/04`, reteniendo `RequirementCasesDataTests` en
el último. **`sdd-tasks` confirma con el conteo real; esta tabla es un pronóstico, no un
compromiso.** Estrategia de entrega de la sesión: `ask-on-risk`.

### Rollback

Revertir los commits borra `Runtime/RequirementResponse/`,
`Tests/EditMode/RequirementResponse/` y `Data/Requirements/` enteros, y quita la sección M16 de
`Docs/MODULES.md`. `IRequirementResponder` queda en `NpcAi.Core` sin implementación real (solo
la base de contrato y su andamio, como está hoy). Ningún módulo lo referencia por nombre hasta
que exista el enrutador de sala de juntas. Sin migración de datos.

## Open Questions / Riesgos del diseño

- **R1 — La frase de prueba del contrato no sale de `juntas.json`.**
  `RequirementResponderContract` usa `"cual es el presupuesto del proyecto"` sobre
  `caso-juntas-01` y exige que con `Receptivo` dé `Revelado` y con `NoReceptivo` dé
  `AunNoRevelado`. Pero la decisión cerrada es que los 4 casos transcriben los 4 dominios de
  `Data/Corpus/juntas.json`, y **ahí no se narra ningún presupuesto**. O `caso-juntas-01.json`
  incorpora `presupuesto` como requerimiento legítimo (lo que hace el ejemplo de arriba), o los
  **casos 4-9 de la base de contrato quedan omitidos por `Assume`** —no rojos, pero tampoco
  verdes— para la implementación real. Ya estaba anticipado como R3 en el `design.md` de M0.
  **Este diseño no lo decide**: es contenido del catálogo. Recomendación para
  `sdd-spec`/`sdd-tasks`: incorporarlo (un cliente de un torneo sí tiene presupuesto, y es el
  único camino a los 10 casos verdes).
- **R2 — Consecuencia observable de "`NoReceptivo` también revela".** Con
  `receptividadMinima == "NoReceptivo"`, `(int)actual >= (int)minima` da `Revelado` para los
  **tres** valores de `Receptivity`: ese requerimiento **nunca** produce `AunNoRevelado`. Es
  exactamente lo que pide la decisión (3 escalones reales), pero implica que el escenario
  "baja y alta receptividad dan texto distinto" solo es observable en requerimientos de umbral
  `Neutral` o `Receptivo`. `sdd-spec` no debe escribir un escenario que lo exija sobre un
  requerimiento de umbral `NoReceptivo`.
- **R3 — Presupuesto de revisión.** ~1.528 líneas autoradas ≈ 3,8× el budget de 400. Ver el
  corte de 4 PRs. `Decision needed before apply: Yes` (lo formaliza `sdd-tasks`).
- **R4 — De dónde salen los bytes fuera del Editor (Quest).** Heredada tal cual de la pregunta
  abierta de M15/AD3. El núcleo (`TryParse(string)`) es agnóstico y probable sin Unity, y AD9
  deja la estrategia afuera; pero nadie ha decidido todavía si es `Resources/`, `StreamingAssets`
  o lectura de `Packages/`. **No bloquea M16** (las pruebas usan `AssetDatabase` en el Editor y
  JSON en memoria); es de M10/M11.
- **R5 — Duplicación del normalizador.** `RequirementMatcher` copia ~70 líneas de
  `ClinicalFactMatcher` porque la regla 3 prohíbe la referencia y mover el normalizador a
  `NpcAi.Core` sería un cambio de contrato v4 con co-revisión (regla 2). Un bug de
  normalización habrá que arreglarlo dos veces. Aceptado, no mitigado.
- **R6 — Este diseño agrega piezas que la propuesta no listó.** La propuesta enumera 5 archivos
  de runtime y 5 de prueba. Este diseño usa 7 y 7: agrega `PersonalityStyleBank.cs` +
  `Data/Requirements/matices.json` (AD6: el banco de desvío tiene que ser dato por la regla 7,
  y meterlo como `switch` privado en `RequirementResponder` haría que agregar una personalidad
  cambiara una clase), y agrega `RequirementCaseLoaderTests` + `PersonalityStyleBankTests`
  (`strict_tdd: true` en `openspec/config.yaml` exige una prueba en rojo por cada pieza de
  producción). Suma ~200 líneas al pronóstico frente a lo que presupuestó la propuesta.
  `sdd-tasks` debe precificarlo; si el presupuesto aprieta, la salida es replegar el banco a un
  `switch` estilo `ClinicalResponder.ConMatiz` (~-100 líneas) **a costa de la regla 7**.
- **R7 — Acoplamiento suave doble ↔ catálogo real.** El doble reconoce `caso-juntas-01..04`
  como ids existentes. Si el catálogo real cambia de nomenclatura, hay que mover el doble en el
  mismo commit o `ScriptedRequirementResponderTests` empieza a omitir por `Assume` sin fallar.
  Mismo acoplamiento que `ScriptedClinicalResponder` ya tiene con `caso-01..03`.
