# Simulación de triaje en VR — capa de contenido y orquestación (módulos nuevos)

Este documento descompone en cambios SDD discretos la capa que el bucle de simulación
de triaje necesita y que **no está** en el mapa de módulos M0–M13 (`Docs/MODULES.md`).
No es una especificación: es el plan de ruta para que cada cambio SDD posterior toque
exactamente un módulo (regla 1 de `openspec/config.yaml`).

> Estado a 2026-09-09. Ningún módulo de esta capa tiene código todavía. Los cambios
> `2026-09-09-m2-clasificador-bert-reducido` y `2026-09-09-m6-generador-markov` son
> independientes de esta capa y avanzan en paralelo.

## 1. Qué cubren los cambios de M2 y M6, y qué no

Los dos cambios SDD ya aterrizados afilan el **cerebro conversacional**:

- **M2** (`Runtime/Nlu`): entiende la frase del usuario → `Intent` + `Tone`.
- **M6** (`Runtime/Dialogue`): redacta la respuesta del NPC según personalidad y receptividad.

El bucle de la simulación —sala de espera, llamar al paciente por su nombre, asignarle
un caso clínico y una personalidad al azar, conversar para extraer la información, y
cerrar con una categoría de triaje— necesita además una **capa de contenido y
orquestación** que hoy no existe.

## 2. Decisiones tomadas (2026-09-09)

1. **El contenido del caso llega al NPC por un módulo dedicado nuevo**, no creciendo el
   contrato de M6 ni por un canal de estado ambiente. El módulo nuevo devuelve la
   respuesta con los hechos del caso; M6 queda como capa de estilo para los turnos
   sociales.
2. **Patrón enrutador**: un paso de enrutado separa el turno *clínico* (la enfermera
   pregunta por síntomas, antecedentes, signos vitales, alergias…) del turno *social*
   (saludo, calmar, presionar, interrumpir). Turno clínico → módulo clínico produce el
   `NpcReply` completo, modulado por personalidad/receptividad que también recibe.
   Turno social → M6 sin cambios. **M6 no se toca.**
3. **La generación estocástica (Markov de M6) nunca toca un hecho clínico**: el módulo
   clínico es el dueño de cualquier frase que contenga un dato del caso, y la produce de
   forma determinista o por plantilla para que el dato sobreviva textualmente.

## 3. Los cinco cambios SDD nuevos

Numeración de módulos (M14, M15) **sugerida**; el equipo la confirma al abrir el primer
cambio. Cada fila es su propio cambio SDD (propuesta → diseño → tareas → revisión
humana); ninguno usa `/sdd-ff` si el módulo es de IA.

> **Redactados el 2026-09-09** en `openspec/changes/` (propuesta + diseño + tareas cada uno,
> sin código): `2026-09-09-m0-puerto-respuesta-clinica`,
> `2026-09-09-m14-catalogo-casos-clinicos`, `2026-09-09-m15-respondedor-clinico`,
> `2026-09-09-m9-decision-triaje`, `2026-09-09-m11-armado-sesion`. Cada uno arrastra sus
> decisiones abiertas para su ronda de propuesta; abajo se resume solo lo esencial.

**Decisiones ya resueltas con el usuario (2026-09-09):** el puerto lleva un `ClinicalCaseId`
(string, espejo de `PersonalityId`), no un DTO de caso; la señal de enrutado es
`ClinicalResponse.Handled` (bool); M15 es **enrutador** (arma el `NpcReply` clínico
completo, M6 intacto) y **determinista** (plantillas, no Markov); **M15 es de Luis**.

### Cambio 1 — Contrato M0: puerto de respuesta clínica

- **Módulo**: M0 (`Runtime/Core`). **Cambio de contrato** → cambio SDD propio, revisado
  antes del merge por el otro dueño compartido de M0 o el asesor (regla 3).
- **Objetivo**: añadir un puerto nuevo (nombre tentativo `IClinicalResponder`) y sus
  DTO. El puerto recibe lo que dijo/preguntó la enfermera (texto, y opcionalmente el
  `IntentResult` de M2 para modular el tono) más una referencia al caso asignado, y
  devuelve un `NpcReply` con el hecho — o una señal de "este turno no es clínico" para
  que el llamador enrute a M6.
- **Fuera de alcance**: no modifica `IIntentClassifier`, `IDialogueGenerator`,
  `IReceptivityEngine` ni los enums existentes. El enrutado en sí (quién llama primero)
  se decide en el Cambio 5, no aquí.
- **Dueño**: compartido M0, conducido por Luis (dueño de M15).
- **Ya decidido**: `IClinicalResponder` recibe `Utterance` + `IntentResult`; el caso se
  vincula con `AssignCase(ClinicalCaseId, PersonalityId)` (patrón `Reset` de M4), no viaja
  como DTO; la señal de enrutado es `ClinicalResponse.Handled` (bool). `Contract.Version` 1 → 2.
- **Decisiones abiertas para su ronda de propuesta**:
  - ¿`Respond` necesita un `Reset()` para reusar la instancia entre sesiones, o M11 crea
    una instancia nueva por sesión?
  - ¿Se agrega el `EventChannel<ClinicalResponse>` ahora o cuando el Cambio 5 defina el
    cableado (Inspector vs. composición en código)?
  - Si el Cambio 4 elige "puerto nuevo" para la decisión de triaje, este mismo `v2` crece
    para incluir `ITriageBoard` + `Triage` + `TriajeVeredicto`.

### Cambio 2 — M14: Catálogo de casos clínicos (`Data/Cases/`)

- **Módulo**: M14 nuevo, **solo dato** (patrón de M3 `Data/Corpus` y M5
  `Data/Personalities`). Sin clases nuevas (regla de diseño de `config.yaml`).
- **Objetivo**: convertir `Casos_Medicos.md` (3 casos hoy) en `Data/Cases/*.json` con
  esquema fijo, más `Data/Cases/README.md` y una spec ligera (como M5, porque M15 y M9
  dependen de invariantes del dato).
- **Esquema tentativo por caso**: `id`, `edad`, `motivoConsulta`, `sintomas[]`,
  `antecedentes[]`, `alergias[]`, `signosVitales{fc, ta, fr, satO2, glasgow, temp}`,
  `banderasRojas[]`, `triajeEsperado` (I–V), `cierreEsperado` (texto), y una **tabla de
  hechos** para la recuperación: entradas `{campo, ejemplosDePregunta[], respuesta}` que
  M15 empareja contra lo que pregunta la enfermera. Esa tabla es el contrato M14 ↔ M15.
- **Fuera de alcance**: no incluye la identidad del NPC (nombre, cuerpo) — eso es del
  Cambio 5. No incluye lógica de emparejamiento — eso es M15.
- **Dueño sugerido**: Luis (dueño de M3 y M9, lado clínico/triaje).
- **Decisiones abiertas**: formato de `triajeEsperado` (romano vs. entero); si la tabla
  de hechos vive en el mismo JSON del caso o en un archivo aparte por caso; cuántos
  hechos mínimos por caso para que la conversación tenga sustancia.
- **Puede empezar en paralelo con el Cambio 1** (el esquema y el DTO de M0 se co-diseñan).

### Cambio 3 — M15: Respondedor clínico (`Runtime/ClinicalResponse/`)

- **Módulo**: M15 nuevo, `Runtime/ClinicalResponse/` (o nombre equivalente). Referencia
  solo `NpcAi.Core`; carga `Data/Cases/` como `TextAsset`. Módulo de IA → sin `/sdd-ff`.
- **Objetivo**: implementación real de `IClinicalResponder` + su doble determinista en
  `Fakes/`, ambos heredando una base de prueba de contrato nueva en `NpcAi.Core.Tests`
  (patrón del repo: cada puerto tiene su base abstracta).
- **Qué hace**: empareja lo que preguntó la enfermera contra la tabla de hechos del caso
  asignado; si hay coincidencia, produce un `NpcReply` con la respuesta (determinista o
  por plantilla, modulada por personalidad/receptividad); si no, devuelve la señal de
  "turno no clínico".
- **Fuera de alcance**: no decide personalidad ni caso (eso es Cambio 5); no genera
  texto social (eso es M6); `ClinicalCase` **no** incluye `clave` (triaje esperado,
  banderas) — así es estructuralmente imposible que M15 lo filtre.
- **Dueño**: **Luis** (confirmado 2026-09-09).
- **Decisiones abiertas**: parser JSON (`JsonUtility` vs. Newtonsoft vs. mini-parser);
  umbral de coincidencia del emparejador; de dónde salen los bytes de `Data/Cases/` en el
  Quest (proveedor `Func<ClinicalCaseId,string>` inyectado por M11).

### Cambio 4 — M9: Decisión de triaje y evaluación (`Runtime/Scenarios/Emergency`)

- **Módulo**: M9. Hoy solo tiene el doble `ScriptedScenarioObjective`; este cambio
  entrega la implementación real del escenario de emergencia.
- **Objetivo**: (a) `Progress01` = fracción de la información clave / banderas rojas del
  caso que la enfermera ya obtuvo (necesita M14 para saber qué es "clave" por caso);
  (b) el cierre: la enfermera envía una categoría I–V, se compara con `triajeEsperado`,
  la sesión termina con acierto/desacierto y justificación.
- **Decisiones abiertas para su ronda de propuesta**:
  - ¿"Enviar categoría de triaje" necesita un puerto nuevo
    (`ITriageDecision.Submit(Triage) → TriageOutcome`, cambio de M0) o vive como API
    interna de la implementación real de M9 que llama el proyecto VR anfitrión?
  - Si hay puerto nuevo, el enum `Triage` (I–V) vive en M0; si no, vive en M9.
  - **Los enums `Intent`/`Tone` NO ganan categorías de triaje**: el triaje es salida del
    escenario, no clasificación de un enunciado. (Restricción, no decisión.)
- **Dueño sugerido**: Luis (dueño de M9).

### Cambio 5 — M11: Armado de sesión (`Samples~/Harness`)

- **Módulo**: M11 (ya existe como carpeta con README; sin escena todavía). Es la raíz de
  composición — `Docs/MODULES.md` ya le asigna este rol.
- **Objetivo**: al iniciar sesión, elegir cuerpo de NPC (señora mayor / joven), caso al
  azar de M14, personalidad al azar de M5, y cablear el pipeline
  M1 → M2 → (enrutador: M15 | M6) → M4 → M8, con M13 registrando.
- **Ya decidido**: el enrutado vive en M11 ("pregunta a M15; si `!Handled`, pregunta a
  M6"); la lógica testeable (`SessionDirector`) va en `Runtime/Harness/`, fuera de
  `Samples~/`; elección de cuerpo, caso y personalidad **por separado** y al azar con
  semilla reproducible, sin repetir el último caso.
- **Decisiones abiertas para su ronda de propuesta**:
  - ¿`Data/Npcs/` (identidad mínima del NPC) es parte de este cambio o un micro-módulo
    aparte (M16) para respetar "un cambio = una carpeta de `Data/`"?
  - ¿M13 se cablea por canales (`UtteranceChannel` + canal de `NpcReply`) o por llamada
    directa desde `SessionDirector`?
- **Dueño**: Jefferson (dueño de M11).

## 4. Grafo de dependencias y orden sugerido

```text
Cambio 1 (puerto M0) ──┬──> Cambio 3 (M15 respondedor) ──┐
                       │                                  ├──> Cambio 5 (M11 armado)
Cambio 2 (M14 casos) ──┴──> Cambio 4 (M9 triaje) ─────────┘
```

- Cambios 1 y 2 arrancan en paralelo (esquema del caso y DTO de M0 co-diseñados).
- Cambios 3 y 4 necesitan 1 + 2 mergeados.
- Cambio 5 es el último: integra todo en una escena.
- M2 y M6 son ortogonales a esta cadena.

## 5. Fuera del alcance de este paquete

- **Llamar al paciente por su nombre en la sala de espera** y **la locomoción**
  sala de espera → sala de triaje: escena, prefabs, navegación, animación e IK son del
  **proyecto VR anfitrión**, no de `com.poli.npc-ai`. El paquete aporta, como mucho, los
  nombres de los NPC como dato (Cambio 5) y, si hiciera falta, un evento "sesión
  iniciada". No es un cambio SDD de este repo.
- **UI del botón de triaje** (el widget en sí): del proyecto anfitrión. El paquete
  expone la operación de enviar la categoría y evaluar (Cambio 4); el botón la invoca.
