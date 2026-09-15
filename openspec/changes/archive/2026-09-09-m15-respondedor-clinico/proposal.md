# Propuesta: M15 — Respondedor clínico (`Runtime/ClinicalResponse/`)

## Intent

`2026-09-09-m0-puerto-respuesta-clinica` agrega el puerto `IClinicalResponder` a
`NpcAi.Core`, pero sin implementación: el contrato queda con una base de prueba y nada que
la satisfaga. Este cambio entrega M15, el módulo que hace que el NPC responda **como el
paciente del caso clínico asignado**.

M15 consume la **tabla de hechos** de M14 (`Data/Cases/caso-*.json` → bloque `hechos`):
cada entrada mapea unas frases de ejemplo (`ejemplosDePregunta`) a una `respuesta` en
primera persona. Cuando la enfermera dice algo que empareja con una entrada, M15 devuelve
esa `respuesta` envuelta en un `NpcReply`, con un matiz de personalidad. Cuando nada
empareja, devuelve `ClinicalResponse.NoAplica` (`Handled == false`) y el turno lo toma M6
(patrón enrutador, decidido por M11).

Decisiones ya fijadas (2026-09-09, `Docs/SIMULACION-TRIAJE-CAPA-NUEVA.md`):

1. M15 es **enrutador**, no envoltura: produce el `NpcReply` clínico completo; M6 no se
   toca.
2. M15 es **determinista** (lo exige el contrato del puerto): un hecho clínico no cambia de
   redacción entre turnos. Plantillas, no cadenas de Markov.
3. **M15 asignado a Luis.**

## Scope

### In Scope

- Nuevo ensamblado `NpcAi.ClinicalResponse` en `Runtime/ClinicalResponse/`, con
  `NpcAi.ClinicalResponse.asmdef` referenciando **solo `NpcAi.Core`**.
- `Runtime/ClinicalResponse/ClinicalCase.cs`: tipo plano (POCO) que refleja el esquema de
  M14 (`paciente`, `hechos`; **no** `clave`).
- `Runtime/ClinicalResponse/ClinicalCaseLoader.cs`: de una cadena JSON a `ClinicalCase`
  (deserialización + validación de esquema). Recibe `string`, no `TextAsset` — así el
  núcleo es probable sin Unity.
- `Runtime/ClinicalResponse/ClinicalFactMatcher.cs`: normaliza el texto de la enfermera y
  lo empareja contra los `ejemplosDePregunta` de la tabla de hechos; devuelve el `campo`
  emparejado o "sin coincidencia".
- `Runtime/ClinicalResponse/ClinicalResponder.cs`: implementación real de
  `IClinicalResponder`. `AssignCase` carga el caso por `ClinicalCaseId`; `Respond` empareja
  y arma el `NpcReply` (respuesta + matiz de personalidad + `EmotionTag`/`AnimationCue` de
  tabla fija por defecto).
- `Runtime/ClinicalResponse/Fakes/ScriptedClinicalResponder.cs`: doble determinista con una
  tabla de hechos mínima embebida, sin cargar archivos.
- Cómo M15 resuelve un `ClinicalCaseId` a los bytes de `Data/Cases/` (ver `design.md` → AD3:
  `Resources` vs. ruta de paquete vs. inyección de un proveedor).
- Pruebas EditMode en `Tests/EditMode/ClinicalResponse/`:
  - `ClinicalResponderTests : ClinicalResponderContract` (real, con un caso de prueba).
  - `ScriptedClinicalResponderTests : ClinicalResponderContract` (doble).
  - `ClinicalFactMatcherTests` (unidad: normalización, coincidencia, no-coincidencia).
  - `ClinicalCasesDataTests`: valida los 3 `Data/Cases/caso-*.json` reales contra el
    esquema (las pruebas que M14 dejó trazadas "hacia adelante").
- Actualizar `Docs/MODULES.md`: sección M15.

### Out of Scope

- **`IClinicalResponder` no cambia.** Este cambio lo implementa; modificarlo sería otro
  cambio de contrato (v3).
- **El esquema de M14.** Si M15 descubre que falta un campo, se agrega en el cambio de M14
  antes de archivar; M15 no redefine el esquema.
- **La `clave` del caso** (`triajeEsperado`, `banderasRojas`, `cierreEsperado`). `ClinicalCase`
  **no** tiene esos campos: `ClinicalCaseLoader` los ignora al deserializar. Una prueba
  verifica que ninguna respuesta de M15 contiene ese texto.
- **El enrutado clínico/social.** M15 solo devuelve `Handled`. Quién llama a M15 antes que
  a M6 lo decide `2026-09-09-m11-armado-sesion`.
- **La receptividad (M4) en la respuesta clínica.** `Respond` no recibe `Receptivity`: un
  hecho es un hecho. La modulación emocional del turno es de M6. Si más adelante se quiere
  que un paciente `NoReceptivo` conteste seco o evasivo, es una extensión posterior (posible
  v3 del puerto), no esta entrega.
- **Selección de caso y personalidad al azar.** Eso es M11.
- **Voz y animación reales.** M15 llena `EmotionTag`/`AnimationCue` con etiquetas; M8 las
  interpreta.

## Capabilities

### New Capabilities

- `respondedor-clinico-m15` (nuevo): formaliza que M15 satisface `respuesta-clinica-m0`
  (heredando `ClinicalResponderContract` sin modificarla), más los requisitos propios: la
  respuesta sale de la tabla de hechos del caso y nunca se inventa; `clave` no se filtra; el
  matiz de personalidad no altera el dato.

### Modified Capabilities

- Ninguna. `IClinicalResponder`, `NpcReply`, `ClinicalResponse` no cambian.

## Approach

**El seam ya existe** (lo crea el cambio de M0). `ClinicalResponder` es un adaptador nuevo
detrás de `IClinicalResponder` — mismo patrón que `BertIntentClassifier` para M2 y
`MarkovDialogueGenerator` para M6. Ningún otro módulo cambia una línea.

**Recuperación por tabla de hechos, no por modelo.** M15 no entrena nada ni descarga nada.
La tabla `hechos` de cada caso es un mapeo explícito frases-de-ejemplo → respuesta.
`ClinicalFactMatcher` normaliza (minúsculas, sin tildes, sin signos) el texto de la
enfermera y busca el `campo` cuyo `ejemplosDePregunta` mejor cubra las palabras de la
pregunta. Es la misma idea que el `SemanticMatcher` que M2 deja como respaldo — pero M15 no
puede referenciar `NpcAi.Nlu` (solo `NpcAi.Core`), así que lleva su propio normalizador
mínimo.

**Determinista por construcción.** Sin `System.Random`, sin dependencia de orden de hash no
estable. Para la misma `(caso, personalidad, utterance, intent)`, `Respond` devuelve el
mismo `Handled` y el mismo `Reply.Text`. La prueba de contrato
`Es_determinista_en_Handled_y_en_el_texto_para_la_misma_entrada` lo exige.

**Matiz de personalidad sin tocar el dato.** El `Reply.Text` es
`plantilla(personalidad) aplicada a respuesta` — p. ej. `grosero` antepone "Ya le dije, ";
`empatico` antepone "Claro, doctora. "; `introvertido` la deja tal cual. La `respuesta`
literal del caso **siempre** aparece como subcadena intacta: una prueba lo verifica. Con
`PersonalityId.None`, sin matiz.

**`EmotionTag`/`AnimationCue` de tabla fija**, no derivados del texto — señales de control
para M8, igual criterio que `ScriptedDialogueGenerator` y que el diseño de M6.

**El doble** (`ScriptedClinicalResponder`) trae 3–4 hechos embebidos en código y ninguna
carga de archivo: sirve para que M4/M8/M11 integren contra M15 sin depender de `Data/Cases/`.

## Affected Areas

| Área | Impacto | Descripción |
|---|---|---|
| `Runtime/ClinicalResponse/` | Nuevo | Ensamblado `NpcAi.ClinicalResponse` (ref: `NpcAi.Core`) |
| `Runtime/ClinicalResponse/ClinicalCase.cs`, `ClinicalCaseLoader.cs`, `ClinicalFactMatcher.cs`, `ClinicalResponder.cs` | Nuevo | Tipo, cargador, emparejador, adaptador real |
| `Runtime/ClinicalResponse/Fakes/ScriptedClinicalResponder.cs` | Nuevo | Doble determinista |
| `Tests/EditMode/ClinicalResponse/` | Nuevo | 4 archivos de prueba (contrato real + doble, matcher, datos de M14) |
| `Docs/MODULES.md` | Modificado | Sección M15 |
| `Data/Cases/` | Sin cambio | M15 lo lee; el catálogo es de M14 |
| `NpcAi.Core`, `Runtime/Dialogue/` (M6) | Sin cambio | Contrato y M6 intactos |

## Risks

| Riesgo | Prob. | Mitigación |
|---|---|---|
| El emparejador da falsos positivos (responde un hecho ante una pregunta que no era esa) o falsos negativos (manda a M6 algo que sí era clínico) | Alta al inicio | Umbral de cobertura de palabras configurable; `ClinicalFactMatcherTests` con casos reales de las 3 consultas; ampliar `ejemplosDePregunta` en M14 es edición de datos. Aceptado que la primera versión no sea perfecta: el criterio de aceptación es el contrato, no la exactitud clínica |
| Determinismo roto por orden de iteración de `Dictionary` o por cultura al normalizar | Media | Usar `List`/orden explícito de la tabla de hechos; `ToLowerInvariant`; `ClinicalResponderContract.Es_determinista...` lo detecta antes de mergear |
| `clave` se cuela en `ClinicalCase` o en una respuesta | Media | `ClinicalCaseLoader` no mapea `clave`; prueba dedicada: ninguna `respuesta` contiene `triajeEsperado` ni banderas rojas del caso real |
| Cómo llegar a los bytes de `Data/Cases/` en runtime (fuera del Editor, en el Quest) no está resuelto | Media | AD3 lo decide explícitamente (probable: `Resources/` o un proveedor inyectado por M11). El núcleo (`ClinicalCaseLoader(string json)`) es agnóstico y probado sin Unity; solo el "de dónde salen los bytes" depende de la plataforma |
| M15 se vuelve un segundo M6 (empieza a "redactar" en vez de plantillar) | Media | AD5: el matiz de personalidad es prefijo/sufijo fijo; la `respuesta` del caso aparece intacta como subcadena, verificado por prueba |

## Rollback Plan

`ClinicalResponder` es implementación nueva y aditiva de un puerto que ya existe. Revertir
los commits borra `Runtime/ClinicalResponse/` y `Tests/EditMode/ClinicalResponse/`. El
puerto `IClinicalResponder` queda en `NpcAi.Core` sin implementación real (solo la base de
prueba). Ningún módulo depende de `ClinicalResponder` por nombre hasta que M11 lo cablee.
No hay migración.

## Dependencies

- **`2026-09-09-m0-puerto-respuesta-clinica` mergeado**: define `IClinicalResponder`,
  `ClinicalResponse`, `ClinicalCaseId` y `ClinicalResponderContract`. M15 no compila sin
  esto.
- **`2026-09-09-m14-catalogo-casos-clinicos` mergeado**: define el esquema y los 3
  `Data/Cases/caso-*.json` que `ClinicalCasesDataTests` valida y que `ClinicalResponder`
  carga.
- `contrato-nucleo-m0` v2, `perfiles-personalidad-m5` (para los nombres de personalidad del
  matiz).
- Ninguna dependencia de red, GPU ni servicios.

## Success Criteria

- [ ] `ClinicalResponderTests : ClinicalResponderContract` y
      `ScriptedClinicalResponderTests : ClinicalResponderContract` pasan la batería heredada
      completa sin modificar la base.
- [ ] Para las 3 consultas reales, M15 responde con hechos del caso ante preguntas típicas
      de anamnesis y devuelve `Handled == false` ante un saludo o una frase social.
- [ ] Ninguna respuesta de M15 contiene el `triajeEsperado` ni una bandera roja del caso.
- [ ] `Respond` es determinista en `Handled` y `Reply.Text` (1000 llamadas de prueba
      manual, misma entrada → misma salida).
- [ ] `NpcAi.ClinicalResponse.asmdef` referencia solo `NpcAi.Core`.
- [ ] `Docs/MODULES.md` tiene la sección M15.
- [ ] El diff no toca `NpcAi.Core`, `Runtime/Dialogue/`, `Data/Cases/` ni ninguna carpeta
      fuera de `Runtime/ClinicalResponse/`, `Tests/EditMode/ClinicalResponse/`, `Docs/` y
      `openspec/`.

## Decisiones del usuario (confirmadas 2026-09-09)

1. **Enrutador**: M15 arma el `NpcReply` clínico completo; M6 intacto.
2. **Determinista**: plantillas, no Markov.
3. **M15 → Luis**, módulo nuevo de primera clase.
