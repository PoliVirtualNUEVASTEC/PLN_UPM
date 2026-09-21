# Propuesta: M16 — Catálogo y respondedor de requerimientos de sala de juntas (`Runtime/RequirementResponse/`)

## Intent

`2026-09-16-m0-puerto-requerimientos-juntas` ya dejó en `main` el puerto `IRequirementResponder`,
el enum `RequirementOutcome`, el DTO `RequirementResponse` y los identificadores
`RequirementCaseId` / `RequirementId` — pero **sin nada que los implemente**: hoy el único
sujeto que satisface el contrato es el andamio `RespondedorDeRequerimientosDePrueba` que vive
dentro de `Tests/EditMode/Core/RequirementResponderContract.cs`.

Este cambio entrega M16, el módulo que hace que el NPC se comporte como el **cliente** de la
sala de juntas: ya tiene los requerimientos del proyecto "en la cabeza" y los suelta
**progresivamente**, a medida que el estudiante se gana su receptividad. Verificado en
`Data/Corpus/juntas.json` (los 5 bloques de tono de `AportaInformacion`): las frases del
estudiante confirman o parafrasean lo que el NPC ya dijo, no lo inventan — el estudiante
**extrae** información, no la aporta.

Segundo eslabón de la cadena **M0 (mergeado) → M16 (este cambio) → M10**. Sin M16, M10 no
tiene de dónde sacar granularidad "qué requerimiento específico se cubrió" y repetiría el
hueco `campoDe(clin)` que ya existe entre M9 y M15.

## Scope

### In Scope

- Nuevo ensamblado `NpcAi.RequirementResponse` en `Runtime/RequirementResponse/`, con
  `NpcAi.RequirementResponse.asmdef` referenciando **solo `NpcAi.Core`**
  (`noEngineReferences: false`, igual que `NpcAi.ClinicalResponse`: usa `JsonUtility`).
- `RequirementCase.cs`: POCO plano que refleja el esquema del catálogo (cliente + tabla de
  requerimientos con su receptividad mínima).
- `RequirementCaseLoader.cs`: de una cadena JSON a `RequirementCase` (deserialización +
  validación de esquema). Recibe `string`, no `TextAsset`, para poder probarse sin escena.
- `RequirementMatcher.cs`: normaliza lo que dijo el estudiante y lo empareja contra los
  `ejemplosDePregunta` de la tabla; devuelve el `RequirementId` emparejado o "sin coincidencia".
  Normalizador propio y mínimo (no puede referenciar `NpcAi.Nlu`, regla 3).
- `RequirementDisclosurePolicy.cs`: la **puerta de receptividad**. Compara el parámetro
  `Receptivity` contra la `receptividadMinima` del requerimiento emparejado y decide
  `Revelado` vs. `AunNoRevelado`. Es la pieza que M15 no tiene.
- `RequirementResponder.cs`: implementación real de `IRequirementResponder`. `AssignCase`
  resuelve el caso por `RequirementCaseId`; `Respond` empareja, aplica la puerta y arma el
  `RequirementResponse`.
- `Fakes/ScriptedRequirementResponder.cs`: doble determinista con una tabla mínima embebida,
  sin cargar archivos, para que M10/M11 integren sin depender de `Data/`.
- `Data/Requirements/`: `README.md` con el esquema + los archivos de caso versionados
  (`caso-juntas-0N.json`), transcritos a partir de los 4 dominios narrados en
  `Data/Corpus/juntas.json` (torneo de fútbol, tienda, colegio, aerolínea).
- Pruebas EditMode en `Tests/EditMode/RequirementResponse/`:
  - `RequirementResponderTests : RequirementResponderContract` (real).
  - `ScriptedRequirementResponderTests : RequirementResponderContract` (doble).
  - `RequirementMatcherTests` (normalización, coincidencia, no-coincidencia).
  - `RequirementDisclosurePolicyTests` (los 3 niveles × los 3 valores de `Receptivity`).
  - `RequirementCasesDataTests` (valida los `.json` reales contra el esquema).
- `Docs/MODULES.md`: sección M16.

### Out of Scope

- **`NpcAi.Core` no cambia.** Este cambio implementa el contrato v3; tocarlo sería un cambio
  de contrato v4 (regla 2). `RequirementResponderContract` se **hereda sin modificar**.
- **La implementación real de M10** (`IScenarioObjective` de sala de juntas) y el mecanismo
  por el que le llega la señal "se reveló el requerimiento X" (API del tipo concreto, patrón
  `RegistrarHechoObtenido`): es el cambio de M10.
- **El enrutado de turno** (quién llama a M16 antes que a M6, selección de caso y personalidad):
  no existe un M11 de sala de juntas; es otro cambio.
- **`Data/Corpus/juntas.json` no se toca.** Es corpus de entrenamiento de M2/M3 y sirve solo
  como fuente narrativa; el catálogo de M16 es un archivo propio y versionado aparte.
- **Reentrenar M2.** M16 consume el `IntentResult` que reciba, no lo produce.
- **Memoria de sesión de lo ya revelado.** El contrato exige determinismo para la misma tupla,
  así que M16 es sin estado entre turnos (ver Approach); acumular lo revelado es de M10.
- **Voz y animación reales.** M16 llena `EmotionTag`/`AnimationCue` con etiquetas; M8 las
  interpreta.
- `Runtime/Core/`, `Runtime/CoreChannels/`, `Runtime/ClinicalResponse/` (M15, de Luis Miguel),
  `Runtime/Scenarios/` (M10), `Runtime/Nlu/`, `Runtime/Dialogue/`: sin cambio.

## Capabilities

### New Capabilities

- `respondedor-requerimientos-m16` (nuevo): formaliza que M16 satisface
  `requerimientos-juntas-m0` heredando `RequirementResponderContract` sin modificarla, más sus
  requisitos propios — la respuesta sale de la tabla del caso y nunca se inventa; el contenido
  del requerimiento solo aparece en `Reply.Text` cuando `Outcome == Revelado`; el matiz de
  personalidad no altera el dato; la puerta de receptividad es dato, no código.
- `catalogo-requerimientos-m16` (nuevo): el esquema del catálogo y sus invariantes (un caso ==
  un archivo, nombre de archivo == `id`; mínimos de requerimientos y de `ejemplosDePregunta`;
  cobertura obligatoria de los niveles de receptividad), con trazabilidad a
  `RequirementCasesDataTests`.

### Modified Capabilities

- Ninguna. `IRequirementResponder`, `RequirementOutcome`, `RequirementResponse`,
  `RequirementCaseId` y `RequirementId` quedan byte-idénticos.

## Approach

**El seam ya existe.** `RequirementResponder` es un adaptador nuevo detrás de un puerto ya
mergeado — mismo patrón que `ClinicalResponder` (M15), `BertIntentClassifier` (M2) y
`MarkovDialogueGenerator` (M6). Ningún otro módulo cambia una línea.

**Un solo módulo, no dos.** El precedente literal separa catálogo (M14) de respondedor (M15)
porque son de dueños distintos. Acá catálogo y código son del mismo dueño y se entregan juntos:
sigue siendo *un* módulo y *un* cambio SDD (regla 1), con la carpeta de datos como parte del
módulo, no como módulo aparte.

**Revelación progresiva = Opción B, y la tabla es dato.** Cada requerimiento lleva su propio
nivel mínimo de `Receptivity`. Los pesos y umbrales viven en `Data/Requirements/`, no como
constantes C# (regla 7 al pie de la letra): agregar un caso o mover un umbral no cambia ni una
clase.

**M16 es sin estado entre turnos — constatación derivada del contrato, no preferencia.**
`RequirementResponderContract.Es_determinista_en_Outcome_texto_y_RequirementId_para_la_misma_entrada`
llama `Respond` tres veces con la misma tupla y exige salida idéntica. Por lo tanto la
"progresión" NO puede implementarse recordando qué se reveló antes: está gobernada
exclusivamente por el parámetro `Receptivity` de cada llamada. `AssignCase` es el único estado
(de sesión, no de turno) y debe ser idempotente.

**Recuperación por tabla, no por modelo.** `RequirementMatcher` normaliza (minúsculas, sin
tildes, sin signos) y busca el requerimiento cuyos `ejemplosDePregunta` mejor cubran las
palabras del estudiante. Empate = menor índice de la tabla, igual que `ClinicalFactMatcher`.
Sin `System.Random`, sin depender del orden de iteración de un `Dictionary`, sin cultura
dependiente (`ToLowerInvariant`).

**Los tres desenlaces, en el orden en que los evalúa `Respond`:**

| Situación | `Outcome` | `Reply` | `RequirementId` |
|---|---|---|---|
| `IsReady == false`, o nada empareja | `NoAplica` | sin garantías | `None` (obligatorio) |
| Empareja pero `Receptivity` < `receptividadMinima` | `AunNoRevelado` | desvío, nunca vacío y distinto del texto real | poblado |
| Empareja y alcanza el umbral | `Revelado` | el hecho del caso | poblado |

Lo que protege el hecho no ganado es **el texto**, no la ausencia del identificador: el
invariante `Outcome != NoAplica ⇒ RequirementId.IsNone == false` ya está escrito en el puerto y
probado en la base de contrato.

**Matiz de personalidad sin tocar el dato.** Mismo criterio que M15: prefijo/sufijo fijo por
`PersonalityId`, con la frase literal del caso presente como subcadena intacta, verificado por
prueba. Con `PersonalityId.None`, sin matiz. `EmotionTag`/`AnimationCue` salen de tabla fija,
no derivados del texto.

**Nombre de carpeta y ensamblado.** Se elige `Runtime/RequirementResponse/` /
`NpcAi.RequirementResponse` / `Tests/EditMode/RequirementResponse/` / `Data/Requirements/`, que
reproduce exactamente la cadena ya establecida en el repo
(`IClinicalResponder` → `ClinicalResponse` → `NpcAi.ClinicalResponse` → `Runtime/ClinicalResponse/`).
Se descartan los 3 candidatos de la exploración: `BoardroomRequirements` ata el módulo a un
escenario, justo lo que el cambio M0 evitó a propósito al hacer los dos identificadores de
dominio y no de escenario; `RequirementMatch` y `RequirementCatalog` nombran una sola pieza
(el emparejador, los datos) de un módulo que entrega las cuatro.

## Affected Areas

| Área | Impacto | Descripción |
|---|---|---|
| `Runtime/RequirementResponse/` | Nuevo | Ensamblado `NpcAi.RequirementResponse` (ref: solo `NpcAi.Core`) |
| `Runtime/RequirementResponse/RequirementCase.cs`, `RequirementCaseLoader.cs`, `RequirementMatcher.cs`, `RequirementDisclosurePolicy.cs`, `RequirementResponder.cs` | Nuevo | Tipo, cargador, emparejador, puerta de receptividad, adaptador real |
| `Runtime/RequirementResponse/Fakes/ScriptedRequirementResponder.cs` | Nuevo | Doble determinista (regla 4) |
| `Data/Requirements/` | Nuevo | `README.md` del esquema + `caso-juntas-0N.json` |
| `Tests/EditMode/RequirementResponse/` | Nuevo | 5 archivos de prueba |
| `Docs/MODULES.md` | Modificado | Sección M16 |
| `Runtime/Core/`, `Tests/EditMode/Core/` | Sin cambio | Contrato v3 intacto; la base de contrato se hereda, no se toca |
| `Data/Corpus/juntas.json` | Sin cambio | Fuente narrativa, no catálogo |
| `Runtime/ClinicalResponse/`, `Runtime/Scenarios/`, `Runtime/Nlu/`, `Runtime/Dialogue/` | Sin cambio | Otros dueños / otros cambios |

## Risks

| Riesgo | Prob. | Mitigación |
|---|---|---|
| **Presupuesto de revisión.** M15 solo (caso+loader+matcher+adaptador) fueron ~280 líneas; M16 agrega la puerta de receptividad, el doble, 5 archivos de prueba y los datos → estimación de orden **muy por encima de las 400 líneas** | Alta | No bloqueante acá: `sdd-tasks` emite el pronóstico con el conteo real. Corte natural sugerido en 3 PRs encadenados: (1) asmdef + `RequirementCase` + loader + `Data/Requirements/` + pruebas de datos; (2) matcher + puerta + sus pruebas unitarias; (3) `RequirementResponder` + doble + las dos suites `: RequirementResponderContract` + `Docs/MODULES.md`. Estrategia de entrega de la sesión: `ask-on-risk` |
| El emparejador da falsos positivos o falsos negativos | Alta al inicio | Umbral de cobertura de palabras configurable como dato; `RequirementMatcherTests` con frases reales de `juntas.json`; ampliar `ejemplosDePregunta` es edición de datos, no reapertura del cambio. El criterio de aceptación es el contrato, no la exactitud de negocio |
| Determinismo roto por orden de iteración o cultura | Media | Tabla como `List` con orden explícito; `ToLowerInvariant`; el test de determinismo de la base de contrato lo detecta antes del merge |
| Se filtra el hecho en el desvío de `AunNoRevelado` (el NPC dice lo que todavía no se ganó) | Media | Prueba dedicada: con `AunNoRevelado`, `Reply.Text` no contiene la `respuesta` del requerimiento; ya hay además un `[Test]` heredado que exige texto distinto entre baja y alta receptividad |
| Cómo llegar a los bytes de `Data/Requirements/` fuera del Editor (Quest) | Media | Mismo problema ya enfrentado por M15 (su AD3); el núcleo `RequirementCaseLoader(string json)` es agnóstico y probable sin Unity — solo el "de dónde salen los bytes" se decide en `design.md` |
| `JsonUtility` no soporta `null` en campos numéricos ni parsea enums por nombre | Media | Gotcha ya documentada en `Data/Cases/README.md`; la serialización de `receptividadMinima` se resuelve explícitamente en `design.md` |
| Unity trae NUnit 3.5: `Is.AnyOf` (3.6+) no compila y tumba la assembly de pruebas | Baja | Gotcha conocida del repo (M5); usar comparaciones planas |
| M16 se vuelve un segundo M6 (empieza a redactar en vez de plantillar) | Media | Plantillas fijas y frase del caso intacta como subcadena, verificado por prueba; el contrato prohíbe la no-determinación |

## Rollback Plan

M16 es implementación nueva y puramente aditiva de un puerto que ya existe en `main`. Revertir
los commits borra `Runtime/RequirementResponse/`, `Tests/EditMode/RequirementResponse/` y
`Data/Requirements/` enteros, y quita la sección M16 de `Docs/MODULES.md`.
`IRequirementResponder` queda en `NpcAi.Core` sin implementación real (solo la base de contrato
y su andamio, exactamente como está hoy). Ningún módulo depende de `RequirementResponder` por
nombre hasta que exista el enrutador de sala de juntas. Sin migración de datos.

## Dependencies

- **`2026-09-16-m0-puerto-requerimientos-juntas` mergeado a `main`** (PRs #39-#42, contrato v3):
  define `IRequirementResponder`, `RequirementOutcome`, `RequirementResponse`,
  `RequirementCaseId`, `RequirementId` y `RequirementResponderContract`. M16 no compila sin esto.
  Ese cambio SDD **todavía no está archivado** (`openspec/specs/` aún no tiene
  `requerimientos-juntas-m0`), así que la spec de M16 debe referenciarlo desde la carpeta del
  cambio o archivarse M0 primero.
- `contrato-nucleo-m0` v3 y `perfiles-personalidad-m5` (nombres de personalidad para el matiz).
- `Data/Corpus/juntas.json` como fuente narrativa (solo lectura).
- M10 depende de este cambio, no al revés.
- Ninguna dependencia de red, GPU ni servicios. Sin co-revisión obligatoria de M0: este cambio
  no toca el contrato.

## Success Criteria

- [ ] `RequirementResponderTests : RequirementResponderContract` y
      `ScriptedRequirementResponderTests : RequirementResponderContract` pasan los 10 `[Test]`
      heredados **sin modificar la base** y sin que ninguno quede omitido por `Assume`.
- [ ] `RequirementCasesDataTests` valida todos los `Data/Requirements/caso-juntas-0N.json`
      contra el esquema, incluida la regla "nombre de archivo == `id`".
- [ ] Para cada caso real, con `Receptivity.Receptivo` el NPC revela el hecho ante una pregunta
      típica del dominio; con `NoReceptivo` devuelve `AunNoRevelado` con desvío no vacío; ante
      una frase social devuelve `NoAplica` con `RequirementId.None`.
- [ ] El texto de `AunNoRevelado` nunca contiene la `respuesta` del requerimiento.
- [ ] `Respond` es determinista en `Outcome`, `Reply.Text` y `RequirementId` para la misma tupla.
- [ ] `NpcAi.RequirementResponse.asmdef` referencia solo `NpcAi.Core`.
- [ ] Agregar un caso o cambiar un umbral no requiere tocar ninguna clase C#.
- [ ] Sin `Debug.Log` en runtime.
- [ ] `Docs/MODULES.md` tiene la sección M16.
- [ ] El diff no toca `Runtime/Core/`, `Runtime/CoreChannels/`, `Runtime/ClinicalResponse/`,
      `Runtime/Scenarios/`, `Runtime/Nlu/` ni `Data/Corpus/`.

## Decisiones del usuario (Jefferson, ya confirmadas)

1. Cadena **M0 → M16 → M10**; M0 ya está en `main`, este cambio es el segundo eslabón.
2. M16 es **un solo módulo** (catálogo de datos + emparejador/respondedor de código juntos), no
   dos, pese a que el precedente M14/M15 los separa.
3. El NPC es el **cliente** que ya tiene los requerimientos y los revela progresivamente.
4. Mecánica de revelación = **Opción B**: nivel mínimo de `Receptivity` **por requerimiento**.
5. Pesos y umbrales van como **dato externo** en `Data/`, no como constantes C#.
6. `RequirementResponse` expone `RequirementId` explícito (evita el hueco `campoDe(clin)` de
   M15).
7. **Determinismo obligatorio**: plantillas fijas, nunca generación tipo Markov.
8. Base narrativa = los 4 dominios de `Data/Corpus/juntas.json`, pero el catálogo de M16 es un
   archivo propio y versionado, no ese corpus.

## Preguntas abiertas (ronda de propuesta)

Ninguna bloquea escribir la spec, pero todas cambian el contenido del catálogo o el alcance del
primer corte. Se responden, se corrigen o se saltan antes de `sdd-spec`/`sdd-design`.

1. **Regla de negocio — ¿qué se puede revelar en `NoReceptivo`?** La exploración asumió "en
   `NoReceptivo` nada se revela", lo que deja el nivel mínimo con solo dos valores útiles
   (`Neutral` y `Receptivo`). ¿Se confirma que ningún requerimiento, ni el más trivial, es
   revelable con el cliente molesto, o hay información "de cortesía" que sí sale siempre?
2. **Regla de negocio — ¿el estudiante pregunta o confirma?** La base de contrato usa la frase
   *"cual es el presupuesto del proyecto"* (una **pregunta**) con `Intent.AportaInformacion`,
   mientras que en `juntas.json` ese intent corresponde al estudiante **confirmando o
   parafraseando**. ¿M16 debe emparejar ambos usos contra la misma tabla ignorando `Intent`, o
   `Intent` debe filtrar algo (p. ej. `PreguntaFueraDeTema` → `NoAplica` sin consultar la
   tabla)?
3. **Contenido — ¿de dónde sale la frase de desvío?** ¿Cada requerimiento trae su propio
   `desvio` en el JSON (más expresivo, más datos que escribir), o hay un banco de desvíos por
   caso o por personalidad (menos datos, menos específico)?
4. **Alcance del primer corte.** ¿Los 4 dominios de `juntas.json` se transcriben como 4 casos en
   esta entrega, o basta 1-2 casos completos y el resto queda como ampliación de datos
   posterior (que por regla 7 no reabre el cambio)? Esto mueve bastante el pronóstico de líneas.
5. **Mínimos del esquema.** M14 fijó "mínimo 8 hechos, mínimo 2 `ejemplosDePregunta`". ¿Qué
   mínimos equivalentes se exigen acá, y se obliga a que cada caso cubra los niveles de
   receptividad usados (para que la progresión sea observable y no un todo-o-nada disfrazado)?
6. **Confirmación de nombre.** Se eligió `Runtime/RequirementResponse/` /
   `NpcAi.RequirementResponse` / `Data/Requirements/` por espejo exacto del precedente de M15.
   Si preferís uno de los 3 candidatos de la exploración, se cambia acá y no en `sdd-apply`.
