# Design: M0 — Puerto de requerimientos de sala de juntas (`IRequirementResponder`)

## Technical Approach

Extensión **puramente aditiva** de `NpcAi.Core`, copiando el molde del cambio de contrato
anterior (`2026-09-09-m0-puerto-respuesta-clinica`): un puerto, un enum, un DTO y **dos**
identificadores nuevos; `Contract.Version` `2 → 3`; entrada `## v3` en el changelog. Ningún
tipo de v1/v2 cambia un byte.

La diferencia de forma frente al precedente es el **tri-estado**: `IClinicalResponder`
devuelve un `bool Handled`; `IRequirementResponder` devuelve un `RequirementOutcome` de tres
valores más un `RequirementId` tipado, porque el turno de sala de juntas tiene un estado
intermedio real ("emparejó el tema, pero no se ganó la confianza") que un booleano borra.

### Unidad de módulo y grafo de referencias (regla `rules.design`)

    NpcAi.Core        ← único ensamblado de runtime tocado (M0)
    NpcAi.Core.Tests  ← único ensamblado de pruebas tocado

Sin cambios de referencias: `noEngineReferences: true` se mantiene. Los cinco tipos nuevos
son C# puro (`string`, `bool`, `struct`, `enum` y los tipos v1 `Utterance`, `IntentResult`,
`NpcReply`, `PersonalityId`, `Receptivity`). `NpcAi.CoreChannels` **no** gana canal (AD8).
`CoreAssemblyPurityTests` verifica la pureza por reflexión sobre el binario, sin edición.

## Architecture Decisions

| # | Decisión | Elección | Alternativas rechazadas | Razón |
|---|---|---|---|---|
| AD1 | Ubicación del enum nuevo | Al **final** de `Runtime/Core/Enums.cs`, después de `Receptivity` | Intercalarlo por afinidad semántica; archivo propio | Inserción al final = los cuatro enums de v1 quedan byte-idénticos y el diff es un solo hunk. Archivo propio rompería el precedente ("los enums viven juntos") |
| AD2 | Ubicación del puerto nuevo en `Ports.cs` | Sección nueva `// ---- Requerimientos de sala de juntas`, **inmediatamente después** de `// ---- Respuesta clinica` | Al final del archivo; dentro de la sección "Escenario" | Los dos puertos del patrón enrutador se leen juntos; el XML doc del nuevo referencia `IClinicalResponder` con `<see cref>`. Es una inserción pura: nada se reordena |
| AD3 | Factorías estáticas del DTO | **Solo** `RequirementResponse.NoAplica`; `AunNoRevelado` y `Revelado` se construyen con el constructor público | Tres factorías (`.AunNoRevelado(id, reply)`, `.Revelado(id, reply)`) | `ClinicalResponse` expone solo el centinela. Dos factorías más son superficie de contrato congelada (y dos casos de prueba más) sin invariante propio que proteger: el único estado que necesita un valor canónico es el "no aplica" |
| AD4 | Orden de miembros del DTO | `Outcome`, `Reply`, `RequirementId` — señal de enrutado primero | `Reply` primero | Espeja `ClinicalResponse` (`Handled`, `Reply`): el campo que decide el enrutado se lee antes que la carga |
| AD5 | Nombre del parámetro de receptividad | `receptivity` (inglés) | `receptividadActual` (candidato de la exploración) | Todos los identificadores de `Ports.cs` son inglés (`caseId`, `nurseUtterance`, `intent`, `personality`, `change`). Los comentarios van en español; los nombres, no |
| AD6 | Nombre del parámetro de enunciado | `studentUtterance` | `nurseUtterance` (copia literal); `utterance` | En sala de juntas quien habla es el estudiante-consultor, no una enfermera. `studentUtterance` mantiene el patrón "quién habla" del precedente |
| AD7 | `RequirementId.None` dentro del miembro estático `NoAplica` | Se usa `RequirementId.None` | `default` para esquivar la regla *Color Color* | Precedente probado en el contrato congelado de v1: `Dtos.cs:38` ya escribe `Intent.Desconocida` dentro del `static IntentResult Unknown(...)` de un struct con campo `Intent Intent`. Compila hoy. En miembros **de instancia** se escribe `this.RequirementId` |
| AD8 | `EventChannel<RequirementResponse>` | No en este cambio | Agregarlo ahora | No existe armador de sesión de sala de juntas; el cableado lo decide M16/M10. Canal sin consumidor = superficie muerta (mismo razonamiento que AD5 del cambio v2) |
| AD9 | Cómo se "ejerce" la base de contrato sin M16 | **Dos** stubs locales en el mismo archivo: uno no-listo y uno mínimo-listo, más una subclase concreta que los corre | Solo el stub no-listo (copia literal del precedente) | Con solo el stub no-listo, los `Assume.That(IsReady)` dejan los caminos `Revelado`/`AunNoRevelado` **omitidos**, no verdes: el criterio "pasa contra un stub mínimo" sería vacuo y el riesgo "el puerto se diseña sin M16 y no calza" quedaría sin mitigar (ver AD10) |
| AD10 | Alcance del stub mínimo-listo | Un caso (`caso-juntas-01`), un requerimiento (`presupuesto`), umbral `Receptivo`, emparejado por `Contains` | Stub con tabla/JSON; dejarlo para M16 | ~25 líneas que recorren las tres ramas del enum. Es andamio de prueba en `Tests/EditMode/Core/`, no una implementación de M16: no hay catálogo, ni matcher, ni `Data/` |

## Data Flow

    (armador de sesión de sala de juntas — futuro, NO es de este cambio)
              │  elige RequirementCaseId + PersonalityId
              ▼
    IRequirementResponder.AssignCase(caseId, personality)   ← una vez por sesión
              │
              ▼   por cada turno del estudiante:
    M1 ISpeechToText  → Utterance      ──┐
    M2 IIntentClassifier → IntentResult ──┤
    M4 IReceptivityEngine.Current → Receptivity ──┤   (por VALOR, no por referencia)
                                                  ▼
      IRequirementResponder.Respond(utterance, intent, receptivity) → RequirementResponse
                                                  │
              ┌───────────────────┬───────────────┴───────────────┐
         NoAplica            AunNoRevelado                    Revelado
              │                   │                                │
              ▼                   ▼                                ▼
      M6 IDialogueGenerator   Reply (desvío) → M8        Reply (el hecho) → M8
                                  │                                │
                                  └────── RequirementId ───────────┘
                                          (telemetría → M10 / M13)

`NpcAi.Core` solo define formas. Quién llama en qué orden es del cambio de enrutado, fuera
de alcance.

## File Inventory

| Archivo | Acción | Rol |
|---|---|---|
| `Runtime/Core/RequirementCaseId.cs` (+ `.meta`) | Crear | `readonly struct` espejo exacto de `ClinicalCaseId` — identifica el **caso** |
| `Runtime/Core/RequirementId.cs` (+ `.meta`) | Crear | Mismo molde — identifica **un requerimiento dentro del caso** |
| `Runtime/Core/Enums.cs` | Modificar | `+ enum RequirementOutcome` al final (AD1) |
| `Runtime/Core/Dtos.cs` | Modificar | `+ readonly struct RequirementResponse` al final |
| `Runtime/Core/Ports.cs` | Modificar | `+ interface IRequirementResponder` en sección nueva (AD2) |
| `Runtime/Core/Contract.cs` | Modificar | `Version` `2 → 3` |
| `Docs/CONTRACT-CHANGELOG.md` | Modificar | `+ ## v3 — 2026-09-16 — Puerto de requerimientos de sala de juntas (M16)` |
| `Tests/EditMode/Core/RequirementResponderContract.cs` (+ `.meta`) | Crear | Base abstracta + 2 stubs + subclase concreta (AD9) |
| `Tests/EditMode/Core/ContractTypeTests.cs` | Modificar | `+ 8` casos nuevos; `1` caso existente editado (ver Riesgo R1) |

Los `.meta` de los tres archivos nuevos **se versionan** (convención vigente del repo).

## Interfaces / Contracts

### `Runtime/Core/RequirementCaseId.cs` y `Runtime/Core/RequirementId.cs`

Copia literal del molde de `ClinicalCaseId.cs` cambiando el nombre del tipo y el `<summary>`.
Comentarios sin tildes, como el resto de `Runtime/`.

```csharp
using System;

namespace NpcAi.Core
{
    /// <summary>
    /// Identificador de un requerimiento dentro de un caso de sala de juntas.
    /// <para>
    /// Deliberadamente NO es un enum: la tabla de requerimientos y su receptividad minima
    /// son dato de M16 (<c>Data/</c>), no parte del contrato. Identifica el TEMA, no el
    /// contenido: el hecho del requerimiento solo viaja en <c>Reply.Text</c> y solo cuando
    /// <see cref="RequirementOutcome.Revelado"/>.
    /// </para>
    /// </summary>
    public readonly struct RequirementId : IEquatable<RequirementId>
    {
        /// <summary>Identificador estable en minusculas y sin tildes, p. ej. "presupuesto".</summary>
        public readonly string Value;

        public RequirementId(string value)
        {
            Value = string.IsNullOrWhiteSpace(value) ? null : value.Trim().ToLowerInvariant();
        }

        /// <summary>Valor por defecto: ningun requerimiento identificado.</summary>
        public static readonly RequirementId None = default;

        public bool IsNone => string.IsNullOrEmpty(Value);

        public bool Equals(RequirementId other) =>
            string.Equals(Value, other.Value, StringComparison.Ordinal);

        public override bool Equals(object obj) => obj is RequirementId other && Equals(other);

        public override int GetHashCode() => Value == null ? 0 : Value.GetHashCode();

        public override string ToString() => Value ?? "(none)";

        public static bool operator ==(RequirementId a, RequirementId b) => a.Equals(b);
        public static bool operator !=(RequirementId a, RequirementId b) => !a.Equals(b);
    }
}
```

`RequirementCaseId` es idéntico salvo el nombre del tipo, el ejemplo del `<summary>`
(`"caso-juntas-01"`) y el texto de la nota (el catálogo de casos es dato de M16).

### `Runtime/Core/Enums.cs` (al final)

```csharp
    /// <summary>
    /// Resultado de un turno de requerimientos de sala de juntas. Producido por M16.
    /// Primer resultado tri-estado del contrato: <c>NoAplica = 0</c> a proposito, como el
    /// resto de los enums (regla centinela de v1).
    /// </summary>
    public enum RequirementOutcome
    {
        NoAplica      = 0,
        AunNoRevelado = 1,
        Revelado      = 2,
    }
```

### `Runtime/Core/Dtos.cs` (al final)

```csharp
    /// <summary>
    /// Respuesta del respondedor de requerimientos. Producido por M16.
    /// <para>
    /// <see cref="RequirementOutcome.NoAplica"/>: el turno no es de requerimientos, el
    /// llamador enruta a <see cref="IDialogueGenerator"/> (M6); <c>Reply</c> NO tiene
    /// garantias y <c>RequirementId</c> DEBE ser <see cref="Core.RequirementId.None"/>.
    /// <see cref="RequirementOutcome.AunNoRevelado"/>: emparejo un requerimiento pero la
    /// receptividad no alcanzo su umbral; <c>Reply</c> es un desvio, nunca silencio.
    /// <see cref="RequirementOutcome.Revelado"/>: <c>Reply</c> va tal cual a M8 y dice el
    /// hecho. En los dos ultimos, <c>RequirementId</c> DEBE venir poblado.
    /// </para>
    /// </summary>
    public readonly struct RequirementResponse
    {
        public readonly RequirementOutcome Outcome;       // NoAplica => enrutar a M6
        public readonly NpcReply           Reply;         // valido si Outcome != NoAplica
        public readonly RequirementId      RequirementId; // poblado si Outcome != NoAplica

        public RequirementResponse(RequirementOutcome outcome, NpcReply reply, RequirementId requirementId)
        {
            Outcome            = outcome;
            Reply              = reply;
            this.RequirementId = requirementId;   // 'this.' explicito: regla Color Color (AD7)
        }

        /// <summary>Centinela de "este turno no es de requerimientos".</summary>
        public static RequirementResponse NoAplica =>
            new RequirementResponse(RequirementOutcome.NoAplica, default, RequirementId.None);
    }
```

### `Runtime/Core/Ports.cs` (sección nueva tras "Respuesta clinica")

```csharp
    // ------------------------------------------------------------------ Requerimientos de sala de juntas

    /// <summary>
    /// M16 — responde como el cliente del caso de sala de juntas asignado, revelando un
    /// requerimiento solo cuando la receptividad actual alcanza el umbral de ESE
    /// requerimiento. Mismo patron enrutador que <see cref="IClinicalResponder"/>: si el
    /// turno no es de requerimientos devuelve <see cref="RequirementResponse.NoAplica"/> y el
    /// llamador enruta a <see cref="IDialogueGenerator"/> (M6). A diferencia de
    /// <see cref="IClinicalResponder"/>, recibe la <see cref="Receptivity"/> por VALOR como
    /// quinto dato de entrada: el gating es dato de contrato, no una referencia viva a
    /// <see cref="IReceptivityEngine"/>.
    /// </summary>
    public interface IRequirementResponder
    {
        /// <summary>
        /// <c>false</c> hasta que <see cref="AssignCase"/> vincule un
        /// <see cref="RequirementCaseId"/> existente. Leer NO DEBE lanzar en ningun estado.
        /// </summary>
        bool IsReady { get; }

        /// <summary>
        /// Vincula el caso y la personalidad (estado de sesion, no de turno; mismo patron que
        /// <see cref="IClinicalResponder.AssignCase"/> y <see cref="IReceptivityEngine.Reset"/>).
        /// Con el mismo par DEBE ser determinista e idempotente. Con un
        /// <see cref="RequirementCaseId"/> desconocido NO DEBE lanzar y DEBE dejar
        /// <see cref="IsReady"/> en <c>false</c>.
        /// </summary>
        void AssignCase(RequirementCaseId caseId, PersonalityId personality);

        /// <summary>
        /// NO DEBE lanzar en ningun estado (sin <see cref="AssignCase"/> previo, con
        /// <paramref name="studentUtterance"/> vacio o <c>default</c>, con
        /// <paramref name="intent"/> <c>default</c>, con cualquiera de los tres valores de
        /// <paramref name="receptivity"/>, con solo simbolos o cadenas muy largas). Con
        /// <see cref="IsReady"/> en <c>false</c> DEBE devolver
        /// <see cref="RequirementResponse.NoAplica"/>.
        /// <para>
        /// Invariante transversal: <c>Outcome != RequirementOutcome.NoAplica</c> DEBE implicar
        /// <c>RequirementId.IsNone == false</c>. Con <see cref="RequirementOutcome.NoAplica"/>
        /// el <c>RequirementId</c> DEBE ser <see cref="Core.RequirementId.None"/> y
        /// <c>Reply</c> NO tiene garantias. Con <see cref="RequirementOutcome.AunNoRevelado"/>
        /// y con <see cref="RequirementOutcome.Revelado"/>, <c>Reply.Text</c> NO DEBE ser
        /// vacio ni solo espacios y <c>Reply.EmotionTag</c> / <c>Reply.AnimationCue</c> NO
        /// DEBEN ser <c>null</c>. El contenido del requerimiento solo DEBE aparecer en
        /// <c>Reply.Text</c> cuando <c>Outcome == RequirementOutcome.Revelado</c>: en
        /// <see cref="RequirementOutcome.AunNoRevelado"/> el texto es un desvio, nunca el
        /// hecho no ganado.
        /// </para>
        /// DEBE ser determinista en <c>Outcome</c>, <c>Reply.Text</c> y <c>RequirementId</c>
        /// para la misma tupla
        /// <c>(RequirementCaseId, PersonalityId, Utterance, IntentResult, Receptivity)</c>
        /// (tags y latencia NO obligados). Misma asimetria deliberada que
        /// <see cref="IClinicalResponder.Respond"/> frente a
        /// <see cref="IDialogueGenerator.Generate"/>, que NO es determinista.
        /// </summary>
        RequirementResponse Respond(Utterance studentUtterance, IntentResult intent, Receptivity receptivity);
    }
```

## Testing Strategy

### `Tests/EditMode/Core/RequirementResponderContract.cs` (nuevo)

`public abstract class RequirementResponderContract` con
`protected abstract IRequirementResponder CreateSubject();` y métodos `[Test]` **no
abstractos**, igual que `ClinicalResponderContract`. Constantes compartidas:
`CasoCualquiera = new RequirementCaseId("caso-juntas-01")`,
`PersonalidadCualquiera = new PersonalityId("empatico")`,
`UtteranceDePrueba() = new Utterance("cual es el presupuesto del proyecto", 1f, 1.4f)`.
Los caminos que exigen sujeto listo se protegen con `Assume.That`, como el precedente.

| # | `[Test]` | Qué fija |
|---|---|---|
| 1 | `Reporta_si_esta_listo_sin_lanzar` | Leer `IsReady` no lanza |
| 2 | `Sin_caso_asignado_Respond_devuelve_NoAplica` | Stub no-listo y sujeto no-listo degradan a `NoAplica` |
| 3 | `Respond_no_lanza_en_ningun_estado` | `default`, símbolos, 5000 caracteres **× los tres valores de `Receptivity`** |
| 4 | `Cuando_revela_el_texto_no_es_vacio_y_los_tags_no_son_nulos` | Con `Receptivo` y `Outcome == Revelado`: `Reply.Text` no vacío, tags no `null` |
| 5 | `Cuando_aun_no_revela_responde_con_un_desvio_no_vacio` | Con `NoReceptivo` y `Outcome == AunNoRevelado`: `Reply.Text` no vacío (nunca silencio) |
| 6 | `El_RequirementId_esta_poblado_si_y_solo_si_el_turno_aplica` | Las dos direcciones del invariante `Outcome != NoAplica ⇔ !RequirementId.IsNone` |
| 7 | `Es_determinista_en_Outcome_texto_y_RequirementId_para_la_misma_entrada` | 3 llamadas idénticas ⇒ mismo `Outcome`, `Reply.Text` y `RequirementId` |
| 8 | `Baja_receptividad_y_alta_receptividad_no_dan_el_mismo_texto` | El gating es observable: el parámetro `Receptivity` cambia la salida |
| 9 | `AssignCase_es_idempotente_con_el_mismo_par` | Reasignar el mismo par no mueve `IsReady` ni la respuesta |
| 10 | `AssignCase_con_caso_desconocido_no_lanza_y_deja_no_listo` | `new RequirementCaseId("no-existe")` ⇒ no lanza, `IsReady == false` |

**Stubs locales en el mismo archivo** (AD9/AD10), ambos `private sealed`:

- `RespondedorDeRequerimientosNoListo` — `IsReady => false`,
  `Respond => RequirementResponse.NoAplica`. Espejo de `RespondedorNoListo`.
- `RespondedorDeRequerimientosDePrueba` — reconoce solo `"caso-juntas-01"`; si el texto
  contiene `"presupuesto"` devuelve `Revelado` cuando
  `(int)receptivity >= (int)Receptivity.Receptivo` y `AunNoRevelado` en otro caso, siempre con
  `new RequirementId("presupuesto")`; cualquier otro texto ⇒ `NoAplica`.

Más `public sealed class RequirementResponderContractStubTests : RequirementResponderContract`
que devuelve el stub de prueba en `CreateSubject()`. Sin esa subclase concreta NUnit no
ejecuta ni un `[Test]` de la base en este cambio.

**Convención que hereda M16**: su implementación real y su doble deben reconocer
`caso-juntas-01` y emparejar la frase de prueba, o los casos 4-9 quedarán omitidos por
`Assume` (no fallarán). Mismo acoplamiento suave que `"caso-01"` en el precedente.

### `Tests/EditMode/Core/ContractTypeTests.cs` (aditivo)

Sección nueva `// --- v3: requerimientos de sala de juntas ---`, al final del bloque de DTO:

| # | `[Test]` | Qué fija |
|---|---|---|
| 1-3 | `RequirementCaseId_normaliza_el_valor_recibido` / `_None_es_el_valor_por_defecto_y_hashea_a_cero` / `_compara_por_valor_normalizado_y_Ordinal` | Juego completo, copia del de `ClinicalCaseId` |
| 4-6 | `RequirementId_…` (los mismos tres) | Juego completo para el segundo identificador |
| 7 | `RequirementResponse_NoAplica_no_aplica_y_no_trae_requerimiento` | `Outcome == NoAplica` **y** `RequirementId.IsNone` |
| 8 | `El_enum_RequirementOutcome_esta_congelado_en_la_v3` | `AssertEnumCongelado(typeof(RequirementOutcome), {("NoAplica",0),("AunNoRevelado",1),("Revelado",2)})` — reusa el helper existente sin tocarlo |

`Los_enums_tienen_el_valor_seguro_en_cero` **no se edita**: el centinela `0` del enum nuevo se
afirma dentro del caso 8, para que ningún caso de v1 cambie (Success Criteria). El helper
`AssertEnumCongelado` ya ordena por nombre, así que sirve tal cual.

`Solo_PersonalityId_implementa_IEquatable_en_la_v1` **no se edita**: su lista es cerrada y
nominalmente de v1; `ClinicalCaseId` tampoco figura ahí pese a implementar `IEquatable<T>`.

### Sin cambio

`ContractVersionChangelogTests` (ata `Version` al mayor `## v<N>`) y `CoreAssemblyPurityTests`
(pureza por reflexión) se ejecutan sin editarse.

Ejecución: Unity 6 Test Runner, EditMode. Ningún agente ejecuta Unity: **el verde es compuerta
humana**.

## Migration / Rollout — orden de commits y corte de PRs

Corte verificado, no asumido: se leyó `ContractVersionChangelogTests.cs`. Su única atadura es
`Contract.Version` ↔ mayor `^##\s+v(\d+)` del changelog. **PR1 no toca ninguno de los dos**,
así que la prueba queda verde con `Version == 2` y `max == 2`. El corte es seguro.

| PR | Rama | Contenido | Estado esperado |
|---|---|---|---|
| **PR1** | `feat/m0-requerimientos-tipos` → rama de la feature | `RequirementCaseId.cs`, `RequirementId.cs`, `+ enum` en `Enums.cs`, `+ DTO` en `Dtos.cs`, `+ 8` casos en `ContractTypeTests.cs` | Verde. Compila solo: el DTO no referencia el puerto. `Version` sigue en `2`, sin `## v3` |
| **PR2** | `feat/m0-puerto-requerimientos` → **rama de PR1** (chain) | `+ puerto` en `Ports.cs`, `RequirementResponderContract.cs`, `Contract.cs` `2→3`, `## v3` en el changelog, edición de `Version_del_contrato_es_dos` | Verde. El bump, la entrada del changelog y el pin literal van en **el mismo commit** |

Orden de commits dentro de PR2 (TDD, `strict_tdd: true`): (1) `RequirementResponderContract`
+ stubs en rojo; (2) el puerto → verde; (3) bump + changelog + pin, un solo commit atómico.

Si `sdd-tasks` pronostica < 400 líneas reales, PR único sigue siendo válido: el corte es una
opción, no una obligación.

Sin migración de datos. Un consumidor de v2 recompilado contra v3 sigue compilando: nada se
quitó ni se renombró. `Contract.Version` sigue siendo `const` — riesgo heredado y aceptado
desde v1.

**Rollback**: revertir los commits deja `Version == 2`, sin `## v3`, sin el puerto, el enum,
el DTO ni los dos identificadores (sus archivos se borran enteros). Ningún módulo depende de
`IRequirementResponder` por nombre todavía.

## Threat Matrix

N/A — este cambio no toca enrutado de procesos, comandos de shell, subprocesos, automatización
de VCS/PR, clasificación de archivos ejecutables ni integración de procesos. Es superficie de
tipos C# puros en un ensamblado sin referencias externas.

## Open Questions / Riesgos del diseño

- **R1 — Inconsistencia detectada en la propuesta, NO resuelta aquí.**
  `ContractTypeTests.Version_del_contrato_es_dos()` afirma `Assert.AreEqual(2, Contract.Version)`.
  Al subir a `3` **ese caso existente de v2 DEBE editarse** (renombrar a
  `Version_del_contrato_es_tres`, valor `3`, comentario `v3`), o el suite queda rojo. La
  Success Criteria de la propuesta dice "sin modificar ningún caso existente de v1/v2".
  Precedente: el cambio v1→v2 editó exactamente ese caso (hoy se llama `…_es_dos` y su
  comentario dice "v2"). Queda marcado como riesgo para que `sdd-spec` / la co-revisión
  decidan si se reformula el criterio; el diseño NO lo cambia por su cuenta.
- **R2** — El stub mínimo-listo (AD9/AD10) es superficie de prueba que la propuesta no
  presupuestó explícitamente (~25-30 líneas extra). Empuja el pronóstico de `sdd-tasks` hacia
  arriba. Alternativa si el presupuesto aprieta: dejar solo el stub no-listo y aceptar que 6
  de los 10 casos queden **omitidos** (`Assume`) hasta M16 — a costa de no mitigar el riesgo
  "el puerto se diseña sin M16 delante y luego no calza".
- **R3** — `caso-juntas-01`, `presupuesto` y la frase de prueba quedan acopladas entre esta
  base de contrato y el cambio de M16. Es acoplamiento suave (`Assume` omite, no falla), pero
  el cambio de M16 debe alinear su `Data/` con esos valores o los casos 4-9 no correrán nunca.
- **R4** — El nombre simple `RequirementId` como campo y como tipo (regla *Color Color*) está
  probado por precedente de v1 (`Dtos.cs:38`), pero si el compilador de Unity 6 se quejara en
  el miembro estático `NoAplica`, la salida sin riesgo es escribir `default` en vez de
  `RequirementId.None` (mismo valor, cero cambio de contrato).
