# Especificacion: generador-dialogo-m6

## Purpose

Congela el alcance y las garantias del generador de dialogo real de M6
(`Runtime/Dialogue/`): que es un adaptador nuevo (`MarkovDialogueGenerator` +
`MarkovChainBuilder`) detras del puerto `IDialogueGenerator` que NO cambia, que nunca
devuelve texto vacio con un respaldo escalonado, que las etiquetas de emocion/animacion
son fijas por receptividad, que `Receptivo` y `NoReceptivo` dan texto distinto, que la
generacion NO es determinista a proposito, que la cadena de Markov solo recombina
palabras del corpus semilla, y que ese corpus (`Data/Dialogue/`) es dato de M6 —
distinto del de M3 — cuya fuente de verdad de forma y tamano esta en el `design.md` de
este cambio.

El puerto `IDialogueGenerator`, el DTO `NpcReply`, `PersonalityId` y `Receptivity` los
posee `contrato-nucleo-m0` y NO se redefinen aqui. El mecanismo de verificacion son las
14 pruebas EditMode nuevas de `Tests/EditMode/Dialogue/` (`MarkovChainBuilderTests`,
`MarkovDialogueGeneratorTests : DialogueGeneratorContract`) mas la base `DialogueGeneratorContract`
que ya pasaba `ScriptedDialogueGenerator`. Documenta y fija el M6 ya en verde; no lo
reescribe.

## ADDED Requirements

### Requirement: Un adaptador nuevo detras de un puerto que no cambia

M6 DEBE entregar `MarkovDialogueGenerator : IDialogueGenerator` en `Runtime/Dialogue/`,
con un helper interno `MarkovChainBuilder`. NO DEBE cambiar ninguna firma de
`IDialogueGenerator` ni de `NpcReply` (propiedad de `contrato-nucleo-m0`); no es un
cambio de M0. `Runtime/Dialogue/Fakes/ScriptedDialogueGenerator.cs` DEBE seguir
existiendo sin cambios como doble determinista de referencia. `MarkovDialogueGenerator`
DEBE pasar la misma bateria `DialogueGeneratorContract` que el doble, sin modificar la
clase base.

#### Scenario: El generador real hereda el mismo contrato que el doble

- Dado `MarkovDialogueGeneratorTests : DialogueGeneratorContract` con `CreateSubject()` = `new MarkovDialogueGenerator(corpus)`
- Cuando se corre la bateria heredada en el Test Runner EditMode
- Entonces pasan `Nunca_devuelve_texto_vacio`, `Las_etiquetas_nunca_son_nulas`, `Funciona_sin_personalidad_asignada`, `Un_NPC_no_receptivo_no_responde_igual_que_uno_receptivo` y `Generate_no_esta_obligado_a_ser_determinista`, y `ScriptedDialogueGeneratorTests` sigue en verde

### Requirement: Nunca devuelve texto vacio, con respaldo escalonado

Para toda combinacion `(PersonalityId, Receptivity)` — incluidas `PersonalityId.None` y
cualquier id ausente del corpus — `Generate` DEBE devolver un `NpcReply` cuyo `Text` NO
sea vacio ni solo espacios. El orden de respaldo DEBE ser: (1) paseo aleatorio sobre la
cadena del bloque `(personalidad, receptividad)`; (2) si el bloque propio no existe, un
bloque de respaldo que junta las frases de todas las personalidades para esa
receptividad; (3) tras un numero acotado de reintentos con paseo vacio, una frase
semilla del bloque tomada verbatim; (4) en el peor caso, una frase fija no vacia. NO
DEBE lanzar en ningun caso.

#### Scenario: Los tres estados de receptividad nunca dan texto vacio

- Dado `MarkovDialogueGenerator` con el corpus real de `Data/Dialogue/`
- Cuando se llama `Generate("empatico", estado, IntentResult.Unknown())` para cada valor de `Receptivity`
- Entonces ningun `reply.IsEmpty` es `true`

#### Scenario: En volumen, ninguna personalidad ni estado da texto vacio

- Dado el generador con el corpus real
- Cuando se llama `Generate` 250 veces por cada una de las 4 personalidades y los 3 estados (3000 llamadas)
- Entonces ninguna devuelve `Text` vacio

#### Scenario: Un id desconocido cae en el respaldo sin lanzar

- Dado el generador con el corpus real
- Cuando se llama `Generate(new PersonalityId("no-existe"), estado, IntentResult.Unknown())` para cada `Receptivity`
- Entonces no lanza y ningun `Text` es vacio

#### Scenario: Funciona sin personalidad asignada

- Dado el generador con el corpus real
- Cuando se llama `Generate(PersonalityId.None, Neutral, IntentResult.Unknown())`
- Entonces no lanza y `Text` no es vacio

### Requirement: Las etiquetas nunca son nulas y son fijas por receptividad

`NpcReply.EmotionTag` y `NpcReply.AnimationCue` NUNCA DEBEN ser `null`. NO DEBEN salir
de la cadena de Markov: son senales de control de animacion/voz y DEBEN venir de una
tabla fija por `Receptivity`, con los mismos valores que `ScriptedDialogueGenerator`:
`Receptivo` -> (`receptivo`, `asentir`), `NoReceptivo` -> (`molesto`, `cruzar_brazos`),
`Neutral` y cualquier otro -> (`neutral`, `idle`).

#### Scenario: Las etiquetas no son nulas

- Dado el generador con el corpus real
- Cuando se llama `Generate("empatico", Neutral, IntentResult.Unknown())`
- Entonces `reply.EmotionTag` y `reply.AnimationCue` no son `null`

### Requirement: Receptivo y NoReceptivo producen texto distinto

Para una misma personalidad, `Generate(..., Receptivity.Receptivo, ...)` y
`Generate(..., Receptivity.NoReceptivo, ...)` DEBEN producir `Text` distinto. La
garantia es estructural: el corpus se indexa por `(personalidad, receptividad)` y los
bloques `Receptivo` y `NoReceptivo` de cada personalidad tienen vocabulario disjunto por
diseno, no por azar.

#### Scenario: Un NPC no receptivo no responde igual que uno receptivo

- Dado el generador con el corpus real
- Cuando se llama `Generate("empatico", Receptivo, ...)` y `Generate("empatico", NoReceptivo, ...)`
- Entonces los dos `Text` difieren

### Requirement: Generate NO es determinista

Dos llamadas a `Generate` con la misma entrada PUEDEN devolver `Text` igual o distinto:
el contrato NO exige igualdad ni desigualdad. Lo unico exigible es que ninguna salida
sea vacia. Es una asimetria deliberada frente a `IIntentClassifier.Classify`, que SI es
determinista en `Intent`/`Tone`.

#### Scenario: Dos llamadas iguales no estan obligadas a coincidir

- Dado el generador con el corpus real
- Cuando se llama `Generate("empatico", Neutral, IntentResult.Unknown())` dos veces
- Entonces ninguna de las dos salidas es vacia y NO se aserta (des)igualdad entre ellas

### Requirement: La cadena de Markov solo recombina el corpus semilla

`MarkovChainBuilder` DEBE construir una tabla de transiciones de bigramas (orden 2) a
partir de una lista de frases semilla. `Walk(rng, largoMaximoPalabras)` DEBE devolver
unicamente palabras presentes en el corpus semilla — NUNCA inventa tokens fuera del
vocabulario de entrada — y DEBE respetar `largoMaximoPalabras` como cota dura contra
ciclos. `Walk` DEBE devolver `""` sin lanzar cuando la cadena no tiene bigramas (corpus
vacio, nulo, o solo frases de una palabra) o cuando `largoMaximoPalabras` es menor que
2. Ese `""` es el caso que `MarkovDialogueGenerator` cubre con el respaldo escalonado.

#### Scenario: El paseo solo usa palabras del corpus

- Dado un `MarkovChainBuilder` con varias frases semilla
- Cuando se hacen 200 paseos con semillas de `System.Random` distintas
- Entonces cada palabra de cada salida aparece en el corpus semilla y ninguna salida es vacia

#### Scenario: El paseo respeta la cota de largo maximo

- Dado un `MarkovChainBuilder` con una frase de 12 palabras
- Cuando se llama `Walk(rng, 5)`
- Entonces la salida tiene 5 palabras o menos

#### Scenario: Corpus degenerado devuelve cadena vacia sin lanzar

- Dado un `MarkovChainBuilder` con lista vacia, con `null`, o con una sola frase de una palabra
- Cuando se llama `Walk(rng, 20)`
- Entonces devuelve `""` y no lanza

#### Scenario: Cota menor que dos devuelve cadena vacia

- Dado un `MarkovChainBuilder` con una frase de varias palabras
- Cuando se llama `Walk(rng, 1)`
- Entonces devuelve `""`

### Requirement: El corpus semilla es dato de M6

M6 DEBE entregar exactamente 4 archivos `Data/Dialogue/<personalidad>.json` —
`grosero`, `histerico`, `introvertido`, `empatico`, los mismos 4 ids que los `.asset` de
`perfiles-personalidad-m5` — mas un `README.md`. Cada `.json` DEBE tener una lista de
frases de ejemplo por cada valor de `Receptivity` (`Receptivo`, `Neutral`,
`NoReceptivo`) y el nombre de archivo DEBE coincidir con la personalidad. El corpus es
lo que dice el **NPC**; es distinto de `Data/Corpus/` de M3, que etiqueta lo que dice el
**usuario**, y NO DEBE reutilizarse cruzado. Ampliar o ajustar frases DEBE ser editar un
`.json` y NO DEBE tocar ninguna clase de `Runtime/`. El minimo de frases por bloque
(`>= 5`) y el esquema JSON tienen su fuente de verdad en el `design.md` de este cambio
(seccion "File Inventory") y en `Data/Dialogue/README.md`.

`Generate` DEBE aceptar el `IntentResult` de la firma sin lanzar, pero esta primera
entrega NO DEBE indexar ni condicionar la cadena por `Intent`: solo por
`(personalidad, receptividad)`. Condicionar por `Intent` es mejora de seguimiento (ver
`proposal.md` -> Out of Scope).

#### Scenario: El corpus de disco trae exactamente las cuatro personalidades

- Dado el contenido de `Data/Dialogue/`
- Cuando se listan los `.json` bajo esa carpeta
- Entonces hay exactamente 4 y sus nombres son `grosero`, `histerico`, `introvertido` y `empatico`

#### Scenario: El generador consume el corpus indexado por personalidad y receptividad

- Dado `new MarkovDialogueGenerator(corpus)` con los 4 `TextAsset` de `Data/Dialogue/`
- Cuando se llama `Generate(id, estado, IntentResult.Unknown())` con cada personalidad y estado
- Entonces la salida sale del bloque `(id, estado)` de ese corpus y `IntentResult` no altera la seleccion

## Trazabilidad (requisito -> prueba en verde)

| Requisito | Prueba(s) que ya lo demuestran |
|---|---|
| Un adaptador nuevo detras de un puerto que no cambia | `MarkovDialogueGeneratorTests` heredadas de `DialogueGeneratorContract` (5); `ScriptedDialogueGeneratorTests` sigue en verde; inspeccion del diff (no toca `Runtime/Core/`) |
| Nunca devuelve texto vacio, con respaldo escalonado | `DialogueGeneratorContract.Nunca_devuelve_texto_vacio`, `...Funciona_sin_personalidad_asignada`; `MarkovDialogueGeneratorTests.Ninguna_personalidad_ni_estado_devuelve_texto_vacio_en_volumen`, `...Un_id_desconocido_usa_el_respaldo_y_no_lanza` |
| Las etiquetas nunca son nulas y son fijas por receptividad | `DialogueGeneratorContract.Las_etiquetas_nunca_son_nulas`; inspeccion de `MarkovDialogueGenerator.EtiquetasDe` contra `ScriptedDialogueGenerator` |
| Receptivo y NoReceptivo producen texto distinto | `DialogueGeneratorContract.Un_NPC_no_receptivo_no_responde_igual_que_uno_receptivo` |
| Generate NO es determinista | `DialogueGeneratorContract.Generate_no_esta_obligado_a_ser_determinista` |
| La cadena de Markov solo recombina el corpus semilla | `MarkovChainBuilderTests.El_paseo_solo_usa_palabras_del_corpus_semilla`, `...El_paseo_respeta_la_cota_de_largo_maximo`, `...Corpus_vacio_produce_paseo_vacio_sin_lanzar`, `...Corpus_nulo_produce_paseo_vacio_sin_lanzar`, `...Una_frase_de_una_sola_palabra_no_lanza_y_no_aporta_cadena`, `...Largo_maximo_menor_que_dos_produce_paseo_vacio` |
| El corpus semilla es dato de M6 | `MarkovDialogueGeneratorTests.El_corpus_de_disco_trae_exactamente_las_cuatro_personalidades_de_M5`; `design.md` seccion "File Inventory" y `Data/Dialogue/README.md` (inspeccion) para esquema y minimo por bloque |
