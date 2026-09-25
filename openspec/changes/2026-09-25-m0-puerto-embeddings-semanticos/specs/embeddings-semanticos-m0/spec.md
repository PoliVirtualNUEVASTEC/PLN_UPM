# Especificacion: embeddings-semanticos-m0

## Purpose

Agrega a `NpcAi.Core` la capacidad de contrato que hoy no existe: representar y producir un
vector de embedding semantico de una oracion. Puerto nuevo y separado (`ISentenceEmbedder`,
nombre de trabajo), aditivo puro sobre v3 — ningun tipo v1-v3 cambia. No define ni evalua la
calidad semantica del encoder real: el `.onnx` commiteado hoy es
`distilbert-base-multilingual-cased`, un encoder de proposito general sin entrenamiento de
similitud/parafrasis; esa evaluacion empirica queda para los cambios consumidores futuros
(M15/M16). Verificacion: `SentenceEmbedderContract` y los casos nuevos de `ContractTypeTests`,
ambos hacia adelante (aun no existen).

## ADDED Requirements

### Requirement: Extension aditiva del contrato a v4

`NpcAi.Core` DEBE agregar exactamente el puerto `ISentenceEmbedder` y el DTO
`SentenceEmbedding`, ambos en C# puro. Los enums, DTO y puertos de v1-v3 NO DEBEN cambiar
(nombre, valor, orden, cardinalidad, firma). `Contract.Version` DEBE pasar de `3` a `4` y
`Docs/CONTRACT-CHANGELOG.md` DEBE ganar `## v4`. `NpcAi.Core` DEBE mantener
`noEngineReferences: true` y cero referencias `NpcAi.*` ajenas.

#### Scenario: Solo se anaden el puerto y el DTO nuevos

- Dado el ensamblado `NpcAi.Core` tras el cambio
- Cuando se comparan sus miembros contra v3
- Entonces v1-v3 quedan identicos y solo aparecen `ISentenceEmbedder` y `SentenceEmbedding`,
  sin `UnityEngine*` ni `NpcAi.*` ajeno

#### Scenario: La version ata al changelog

- Dado `Contract.Version`
- Cuando se compara con el mayor `## v<N>` de `Docs/CONTRACT-CHANGELOG.md`
- Entonces ambos son `4`

### Requirement: DTO `SentenceEmbedding`

`SentenceEmbedding` DEBE ser un `readonly struct` en C# puro que exponga un vector de `float`
de solo lectura (p. ej. `Vector`) y su `Length`. DEBE existir `SentenceEmbedding.Empty` con
`Length == 0`. NO DEBE llevar una dimension fija como constante del contrato: `Length` es
implementation-defined y se descubre en runtime, nunca hardcodeada en `NpcAi.Core` (mismo
criterio que el numero de personalidades de M5 o de casos de M14: dato de la implementacion,
no del contrato).

#### Scenario: Empty es el vector vacio

- Dado `SentenceEmbedding.Empty`
- Cuando se lee `Length`
- Entonces es `0`

### Requirement: `IsReady` nunca lanza

Leer `IsReady` de `ISentenceEmbedder` NO DEBE lanzar en ningun estado. DEBE ser `false`
mientras el modelo/encoder no este cargado (mismo patron que `IIntentClassifier.IsReady`).

#### Scenario: Lectura segura en cualquier estado

- Dado un `ISentenceEmbedder` recien creado
- Cuando se lee `IsReady`
- Entonces no se lanza excepcion

### Requirement: `Embed` nunca lanza, ante ninguna entrada

`Embed(string text)` NO DEBE lanzar en ningun estado: ni con `IsReady == false`, ni ante
`null`, `""`, solo espacios, simbolos (`"!!!???"`), cadenas de 5000 caracteres, o cadenas de
solo numeros (`"123 456"`).

#### Scenario: Bateria de entradas atipicas no lanza

- Dado un `ISentenceEmbedder` listo y otro no listo
- Cuando se llama `Embed` con `null`, `""`, `"   "`, `"!!!???"`, `new string('a', 5000)` y
  `"123 456"`
- Entonces ninguna llamada lanza excepcion

### Requirement: Determinismo total del vector

Para el mismo texto de entrada, `Embed` DEBE devolver el mismo `SentenceEmbedding` bit-exacto
(elemento a elemento) en cada llamada, sin tolerancia. A diferencia de
`IIntentClassifier.Classify` (determinismo solo en `Intent`/`Tone`), aqui el vector completo es
la senal primaria de emparejamiento futuro: exige la misma garantia total que
`IClinicalResponder.Respond` sobre `Reply.Text`, no la asimetria de v1.

#### Scenario: Misma entrada, mismo vector

- Dado un `ISentenceEmbedder` listo
- Cuando se llama `Embed` tres veces con el mismo texto
- Entonces las tres devuelven vectores iguales elemento a elemento

### Requirement: Longitud consistente, dimension implementation-defined

Mientras `IsReady == true`, todo vector devuelto por `Embed` (para cualquier texto de entrada,
incluida la bateria de entradas atipicas) DEBE tener la misma `Length`. El contrato NO fija esa
longitud como constante: PUEDE variar entre implementaciones o reentrenamientos con otro
encoder (768 con DistilBERT, 384 con MiniLM); se descubre leyendo `Length` en runtime, nunca
comparando contra un numero fijo del contrato.

#### Scenario: La longitud no cambia entre textos distintos

- Dado un `ISentenceEmbedder` listo
- Cuando se llama `Embed` con varios textos distintos, incluida la bateria de entradas atipicas
- Entonces todos los vectores devueltos tienen la misma `Length`

### Requirement: No listo devuelve vector vacio, nunca lanza

Con `IsReady == false`, `Embed` DEBE devolver `SentenceEmbedding.Empty` (`Length == 0`) para
cualquier entrada, y NO DEBE lanzar. Mismo patron duro que `IClinicalResponder`/
`IRequirementResponder` con `IsReady == false` (no la recomendacion blanda de
`IIntentClassifier.Classify` con `IntentResult.Unknown`).

#### Scenario: No listo siempre devuelve el vector vacio

- Dado un `ISentenceEmbedder` con `IsReady == false`
- Cuando se llama `Embed` con cualquier texto de la bateria de entradas atipicas
- Entonces cada llamada devuelve `SentenceEmbedding.Empty` sin lanzar

### Requirement: Conformidad con `SentenceEmbedderContract`

Toda implementacion real de `ISentenceEmbedder` en `Runtime/<Modulo>/` y todo doble en
`Runtime/<Modulo>/Fakes/` DEBEN heredar de `NpcAi.Core.Tests.SentenceEmbedderContract` y pasar
el 100% de sus `[Test]` heredados, sin escena de Unity ni entorno de VR. La base DEBE definir
dos stubs locales desechables (uno no listo, uno listo con vector fijo de prueba) y una
subclase concreta que pruebe que el contrato es satisfacible, cubriendo: `IsReady` sin lanzar
en ningun estado; `Embed` sin lanzar ante la bateria completa de entradas atipicas (listo y no
listo); determinismo bit-exacto con la misma entrada repetida; longitud constante entre
entradas de texto distintas; y `SentenceEmbedding.Empty` para todo `Embed` cuando no esta
listo.

#### Scenario: La suite contractual pasa contra el stub de prueba

- Dado el ensamblado de pruebas de `NpcAi.Core`
- Cuando se ejecutan las pruebas derivadas de `SentenceEmbedderContract`
- Entonces el 100% resulta en verde contra la subclase concreta de prueba

### Requirement: Entrada `## v4` en `Docs/CONTRACT-CHANGELOG.md`

La entrada DEBE seguir el molde de `## v1`-`## v3`: seccion "Tipos nuevos" (`ISentenceEmbedder`,
`SentenceEmbedding`, C# puro); invariantes del puerto (`IsReady`, `Embed`, determinismo,
consistencia de longitud, comportamiento no listo); seccion "Asimetria de determinismo" que
ubique este determinismo total del vector junto a las asimetrias ya registradas de v1
(`Classify`/`Generate`) y v2 (`ClinicalResponder`); seccion "Decisiones y proceso" (regla 10)
que registre AD1 (dimension implementation-defined) y AD2 (determinismo total) como resueltas,
la advertencia honesta sobre la calidad semantica de `distilbert-base-multilingual-cased`
heredada de la propuesta, y la co-revision de M0 (pendiente hasta el merge, por el otro dueno
compartido o el asesor).

#### Scenario: La entrada existe y tiene las secciones exigidas

- Dado `Docs/CONTRACT-CHANGELOG.md` tras el cambio
- Cuando se lee `## v4`
- Entonces incluye Tipos nuevos, invariantes del puerto, Asimetria de determinismo y
  Decisiones y proceso

## Fuera de alcance (explicito)

- `IIntentClassifier`, `IntentResult` y todo tipo de v1-v3: byte-identicos.
- Implementacion real de M2 (`BertIntentClassifier`, `export_onnx`, re-export del `.onnx`),
  consumo en M15/M16, `Runtime/Nlu/Fakes/` y cualquier `Runtime/<Modulo>/Fakes/` (los stubs de
  este cambio viven solo en `Tests/EditMode/Core/`).
- `Docs/MODULES.md` y `Runtime/CoreChannels/`.

## Trazabilidad (requisito -> prueba)

Trazabilidad hacia adelante: `SentenceEmbedderContract` y los casos nuevos de
`ContractTypeTests` los crea la fase apply de este cambio; aun no existen.

| Requisito | Prueba(s) previstas |
|---|---|
| Extension aditiva a v4 | `ContractTypeTests` (casos nuevos); `ContractVersionChangelogTests`; `CoreAssemblyPurityTests` |
| DTO `SentenceEmbedding` | `ContractTypeTests` (`Empty`, `Length`) |
| `IsReady` nunca lanza | `SentenceEmbedderContract` |
| `Embed` nunca lanza | `SentenceEmbedderContract` (bateria de entradas atipicas) |
| Determinismo total | `SentenceEmbedderContract` (misma entrada, 3 llamadas) |
| Longitud consistente | `SentenceEmbedderContract` (varios textos) |
| No listo devuelve vacio | `SentenceEmbedderContract` |
| Conformidad con `SentenceEmbedderContract` | Ejecucion contra el stub de prueba (y, hacia adelante, M2) |
| Entrada `## v4` | Lectura de `Docs/CONTRACT-CHANGELOG.md` (revision humana + `ContractVersionChangelogTests`) |
