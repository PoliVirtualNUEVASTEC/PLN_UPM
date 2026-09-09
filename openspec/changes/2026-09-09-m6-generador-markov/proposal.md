# Propuesta: M6 — Generador de diálogo por cadenas de Markov

## Intent

`Docs/MODULES.md` documenta hoy a M6 como **"solo doble"**: `Runtime/Dialogue/` en
`origin/main` únicamente contiene `Fakes/ScriptedDialogueGenerator.cs` — un `switch` fijo sobre
`Receptivity` con 3 plantillas de texto codificadas (`"Claro, digame en que le ayudo"`, `"No
tengo nada mas que hablar con usted"`, `"Lo escucho"`). No hay implementación real, y no existe
`openspec/specs/dialogo-*` ni carpeta de cambio archivada para este módulo — M6 nunca se
formalizó.

El propio contrato ya anticipa el enfoque: `Tests/EditMode/Core/DialogueGeneratorContract.cs`
tiene una prueba dedicada, `Generate_no_esta_obligado_a_ser_determinista`, con el comentario
*"el generador real usa Markov"* — una asimetría deliberada frente a `IIntentClassifier`
(`Es_determinista_para_la_misma_entrada` sí lo exige). Esto coincide exactamente con la
propuesta de trabajo de grado (versión enviada al jurado), que distingue dos módulos de IA: uno
de comprensión (BERT reducido, cambio `2026-09-09-m2-clasificador-bert-reducido`) y uno de
generación de respuestas ("cadenas de Markov y demás para darle respuesta").

Este cambio cierra ese vacío: implementa `MarkovDialogueGenerator`, un generador real que
construye cadenas de Markov de palabras a partir de un corpus de frases semilla por personalidad
y por estado de receptividad, y las usa para producir texto de respuesta variado (no plantillas
fijas) sin romper ninguna garantía que ya exige `DialogueGeneratorContract`.

Decisiones ya fijadas antes de este cambio (propuesta de trabajo de grado, confirmadas con el
usuario el 2026-09-08/09):

1. **Técnica de generación**: cadenas de Markov, no un modelo entrenado — coherente con el
   objetivo de mantener el módulo de generación ligero y 100% on-device, sin el costo de
   entrenar un segundo modelo neuronal además del clasificador de M2.
2. **Costo real**: 100% gratuito y offline. Una cadena de Markov es una tabla de transiciones
   construida en el dispositivo (o en build time) a partir de texto semilla — no descarga nada,
   no requiere GPU, no tiene dependencia externa ni siquiera puntual (a diferencia del
   entrenamiento de M2, que sí necesita descargar un encoder pre-entrenado una vez).

## Scope

### In Scope

- Corpus semilla nuevo `Data/Dialogue/<personalidad>.json` (uno por cada una de las 4
  personalidades de M5: `grosero`, `histerico`, `introvertido`, `empatico`), con frases de
  ejemplo agrupadas por estado de `Receptivity` (`Receptivo`, `Neutral`, `NoReceptivo`). Este
  corpus es nuevo y distinto del de M3 (`Data/Corpus/*.json` etiqueta lo que dice el *usuario*,
  no lo que responde el NPC) — no se puede reutilizar sin más.
- `Runtime/Dialogue/MarkovChainBuilder.cs`: construye una tabla de transiciones de palabras
  (n-grama de orden 2) a partir de una lista de frases semilla.
- `Runtime/Dialogue/MarkovDialogueGenerator.cs` implementando `IDialogueGenerator`: selecciona el
  bloque de frases semilla por `PersonalityId` + `Receptivity`, genera una frase nueva por paseo
  aleatorio sobre la cadena de Markov correspondiente, con reintento acotado y respaldo a una
  frase semilla verbatim si el paseo degenera (ver `Risks`) — garantía de "nunca vacío" que ya
  exige el contrato.
- `Tests/EditMode/Dialogue/MarkovDialogueGeneratorTests.cs : DialogueGeneratorContract` (hereda
  la misma base que ya pasa `ScriptedDialogueGenerator`).
- Primer `openspec/specs/generador-dialogo-m6/spec.md`: M6 nunca tuvo spec formal — este cambio
  la crea, formalizando el comportamiento que `DialogueGeneratorContract.cs` ya fija.
- Actualizar `Docs/MODULES.md` (sección M6), cerrando el estado "solo doble".

### Out of Scope

- **`IDialogueGenerator` no cambia.** Ni una firma nueva: este cambio reemplaza el *motor* de
  M6, no su contrato. No es cambio de M0.
- **`ScriptedDialogueGenerator` (el doble) no cambia.** Sigue existiendo, sigue pasando
  `DialogueGeneratorContract`, y cualquier prueba o escena que hoy dependa de él sigue
  funcionando igual.
- **Uso de `IntentResult` para variar el contenido de la respuesta**: la firma de `Generate`
  recibe `IntentResult`, y este cambio lo acepta y no lanza con ningún valor, pero la primera
  implementación **no condiciona la cadena de Markov por intención** — solo por personalidad y
  receptividad, igual dimensionalidad que el doble actual. Condicionar el contenido también por
  `Intent` (p. ej. una respuesta distinta ante `PreguntaFueraDeTema` que ante `Empatia`) es una
  mejora natural de seguimiento, no parte de esta primera entrega: el corpus semilla ya es un
  esfuerzo nuevo de autoría (4 personalidades × 3 estados), multiplicarlo por 7 valores de
  `Intent` antes de validar que el enfoque funciona no es prudente.
- **M2 (clasificador)**: cambio aparte (`2026-09-09-m2-clasificador-bert-reducido`), un módulo
  por cambio (regla 1). `IntentResult` que M6 recibe hoy viene de `ScriptedIntentClassifier`
  (el doble de M2) en cualquier composición de prueba — eso no cambia con este cambio.
- **M8 (presentador)**: consumidor de `NpcReply`, sin cambios — la superficie de salida
  (`Text`/`EmotionTag`/`AnimationCue`) es la misma que ya produce el doble.
- Afinar la cadena de Markov con un modelo de lenguaje neuronal, n-gramas de orden mayor a 2,
  o suavizado estadístico (Kneser-Ney y similares): con un corpus semilla chico, un n-grama de
  orden 2 con respaldo a frase verbatim es suficiente y evita la complejidad de afinar
  hiperparámetros sin datos para validarlos.

## Capabilities

### New Capabilities

- `generador-dialogo-m6` (nuevo `openspec/specs/generador-dialogo-m6/spec.md`): primera spec
  formal de M6. Formaliza el comportamiento que `DialogueGeneratorContract.cs` ya fija en código
  (nunca `Text` vacío, `EmotionTag`/`AnimationCue` nunca `null`, funciona con
  `PersonalityId.None`, `Receptivo` ≠ `NoReceptivo`, determinismo NO exigido) como requisitos
  trazables, siguiendo el mismo patrón que `perfiles-personalidad-m5` hizo sobre el esquema de
  M4.

### Modified Capabilities

- Ninguna: `IDialogueGenerator` (parte de `contrato-nucleo-m0`) no cambia.

## Approach

**El seam ya existe y ya está probado**: `IDialogueGenerator` es la única superficie que ve el
resto del sistema (M8 consume `NpcReply`, nada más). `MarkovDialogueGenerator` es un adaptador
nuevo detrás de esa interfaz — mismo patrón que `BertIntentClassifier` para M2 y
`VoskRecognitionEngine` para M1.

**Por qué cadenas de Markov y no un modelo entrenado**: la propuesta de trabajo de grado ya fija
esta técnica para el módulo de generación, distinta de la de M2 (que sí entrena un modelo). Una
cadena de Markov de palabras es una tabla de transiciones — sin entrenamiento, sin descarga, sin
GPU: coherente con que el módulo completo sea "100% ligero, económico y funcional sin
dependencia de servicios externos" incluso más estrictamente que M2 (M2 sí tiene una dependencia
de internet puntual para descargar el encoder; M6 no tiene ninguna).

**Selección del bloque semilla**: `MarkovDialogueGenerator` indexa el corpus semilla por
`(PersonalityId, Receptivity)` — 4 personalidades × 3 estados = 12 bloques. Con
`PersonalityId.None`, usa un bloque neutro por defecto (mismo criterio que
`ScriptedDialogueGenerator` ya aplica con `"el NPC"` cuando no hay personalidad). Esto preserva
exactamente la dimensionalidad que el contrato ya prueba
(`Funciona_sin_personalidad_asignada`, `Un_NPC_no_receptivo_no_responde_igual_que_uno_receptivo`).

**Generación**: dado un bloque de frases semilla, se construye (una vez, no por cada `Generate`)
una tabla de transiciones de palabras de orden 2 (bigramas → siguiente palabra). Cada llamada a
`Generate` hace un paseo aleatorio desde una palabra inicial elegida al azar entre los inicios de
frase del bloque, hasta un token de fin de frase o un largo máximo. Si el paseo produce una
cadena vacía o degenerada (posible con muy pocas frases semilla), se reintenta un número acotado
de veces y, si sigue fallando, se devuelve una frase semilla del bloque tomada verbatim al azar —
la garantía de "nunca vacío" no depende de que la generación estadística salga bien.

**`EmotionTag`/`AnimationCue`**: siguen viniendo de una tabla fija por `Receptivity` (igual que
hoy en el doble), no de la cadena de Markov — son metadatos de control de animación/voz, no
texto, y no tiene sentido generarlos estocásticamente.

**El doble no se toca**: `ScriptedDialogueGenerator` sigue siendo la implementación de
referencia para pruebas rápidas y para cualquier composición que prefiera evitar la
variabilidad de un generador estocástico (p. ej. pruebas de integración que necesiten texto
exacto y predecible).

## Affected Areas

| Área | Impacto | Descripción |
|---|---|---|
| `Data/Dialogue/` | Nuevo | Corpus semilla de frases por personalidad × receptividad |
| `Runtime/Dialogue/MarkovChainBuilder.cs` | Nuevo | Construye la tabla de transiciones de orden 2 |
| `Runtime/Dialogue/MarkovDialogueGenerator.cs` | Nuevo | Implementación real de `IDialogueGenerator` |
| `Tests/EditMode/Dialogue/MarkovDialogueGeneratorTests.cs` | Nuevo | Hereda `DialogueGeneratorContract` |
| `openspec/specs/generador-dialogo-m6/spec.md` | Nuevo | Primera spec formal de M6 |
| `Docs/MODULES.md` | Modificado | Cerrar el estado "solo doble" en la sección M6 |
| `Runtime/Dialogue/Fakes/ScriptedDialogueGenerator.cs` | Sin cambio | Sigue siendo el doble determinista |
| `Runtime/Core/`, `Runtime/CoreChannels/` | Sin cambio | `IDialogueGenerator` no cambia |

## Risks

| Riesgo | Prob. | Mitigación |
|---|---|---|
| Corpus semilla nuevo y chico (recién creado en este mismo cambio) produce cadenas de Markov repetitivas o poco naturales | Alta al inicio | Aceptado para esta primera entrega — el criterio de aceptación es el contrato (nunca vacío, `Receptivo` ≠ `NoReceptivo`, etc.), no la calidad literaria; ampliar el corpus semilla es iteración natural una vez el equipo vea el resultado en la escena real |
| Paseo aleatorio sobre una cadena con muy pocas transiciones entra en bucle o termina en cadena vacía | Media | Reintento acotado + respaldo a frase semilla verbatim (ver `Approach`); cubierto explícitamente por `Nunca_devuelve_texto_vacio` del contrato heredado |
| Autoría del corpus semilla (4 personalidades × 3 estados, texto nuevo) es trabajo humano no trivial, distinto del corpus de M3 | Media | Este cambio entrega un primer bloque mínimo (suficiente para pasar el contrato y dar variedad básica); no se bloquea en tener un corpus grande antes de empezar, a diferencia de M2 que sí depende de que M3 avance primero |
| Confusión entre este corpus nuevo (`Data/Dialogue/`, lo que dice el NPC) y el de M3 (`Data/Corpus/`, lo que dice el usuario) | Baja | Nombres de carpeta y README distintos; `Data/Dialogue/README.md` deja explícito el propósito y la diferencia desde el primer commit |
| Generación no determinista dificulta pruebas automatizadas de contenido exacto | Ninguna (por diseño) | El contrato ya exige explícitamente que `Generate` NO sea determinista (`Generate_no_esta_obligado_a_ser_determinista`) — no es un riesgo nuevo, es el comportamiento esperado |

## Rollback Plan

`MarkovDialogueGenerator` es una implementación nueva y aditiva de un puerto que ya existe;
`ScriptedDialogueGenerator` no se toca. Revertir los commits de este cambio deja el generador
real en el estado "solo doble" que ya está en `main` hoy — el sistema sigue funcionando (con
texto fijo en vez de variado), porque ningún otro módulo depende de `MarkovDialogueGenerator`
por nombre, solo de `IDialogueGenerator`. No hay migración de datos ni cambio de
`Contract.Version`.

## Dependencies

- `contrato-nucleo-m0`, archivado y estable: define `IDialogueGenerator`, `NpcReply`,
  `PersonalityId`, `Receptivity`. Este cambio no lo modifica.
- `perfiles-personalidad-m5`, ya mergeado: define las 4 personalidades (`grosero`, `histerico`,
  `introvertido`, `empatico`) cuyo nombre (`PersonalityId.Value`) este cambio usa para indexar
  el corpus semilla.
- Ninguna dependencia externa ni de red: a diferencia de M2, la cadena de Markov no descarga
  nada — el corpus semilla se versiona junto con el código.

## Success Criteria

- [ ] `MarkovDialogueGenerator` pasa exactamente la misma batería `DialogueGeneratorContract`
      que `ScriptedDialogueGenerator`, sin modificar la clase base.
- [ ] Para las 4 personalidades y los 3 estados de receptividad, `Generate` nunca devuelve texto
      vacío en 1000 llamadas consecutivas de prueba manual (cubre el caso de paseo degenerado
      con corpus semilla chico).
- [ ] `openspec/specs/generador-dialogo-m6/spec.md` existe y tiene trazabilidad a
      `MarkovDialogueGeneratorTests`.
- [ ] `Docs/MODULES.md` (sección M6) deja de decir "solo doble": pasa a describir el estado real
      (generador por cadenas de Markov, corpus semilla usado, tamaño del corpus por bloque).
- [ ] El diff de este cambio no toca `Runtime/Core/`, `Runtime/CoreChannels/`, ni ninguna carpeta
      fuera de `Data/Dialogue/`, `Runtime/Dialogue/`, `Tests/EditMode/Dialogue/`, `Docs/` y
      `openspec/`.

## Decisiones del usuario (confirmadas 2026-09-08/09)

1. **Técnica de generación**: cadenas de Markov, coherente con el objetivo de un módulo ligero
   y económico — no se entrena un segundo modelo neuronal para este módulo.
2. **Costo/dependencias**: 100% offline y sin ninguna dependencia externa, ni siquiera puntual
   (a diferencia de la descarga única del encoder en M2).
