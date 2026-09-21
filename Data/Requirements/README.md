# M16 — Catálogo de requerimientos de sala de juntas

Casos de sala de juntas que un NPC-cliente "adquiere" al iniciar una sesión de levantamiento
de requerimientos. Es dato puro: ningún `.asmdef`, ninguna clase C#. El tipo que deserializa
este JSON (`RequirementCase`) y las pruebas que validan estos archivos contra el esquema los
define M16 (`Runtime/RequirementResponse/`,
`Tests/EditMode/RequirementResponse/RequirementCasesDataTests.cs`).

## Diferencia con otros `Data/`

- **`Data/Corpus/` (M3)**: frases etiquetadas para *entrenar/evaluar* el clasificador de
  intención (M2). No pertenece a un cliente ni a un caso; es la única fuente narrativa
  permitida para este catálogo (los 4 dominios de `juntas.json`: torneo de fútbol, tienda,
  colegio, aerolínea).
- **`Data/Dialogue/` (M6)**: lo que dice el NPC en turnos **sociales** (saludo, calma,
  presión) — nunca un requerimiento de negocio.
- **`Data/Cases/` (M14)**: catálogo clínico equivalente para el escenario de triaje; mismo
  patrón de archivo, dominio distinto.
- **`Data/Requirements/` (M16, este)**: lo que dice el NPC-cliente en turnos de **sala de
  juntas** — cuando el estudiante pregunta por una entidad, una regla de negocio o un dato del
  sistema que se está levantando, la respuesta sale de aquí, condicionada a la receptividad
  ganada (`receptividadMinima`).

## Regla de archivo

**Un caso == un archivo, nombre de archivo == `id`.** `caso-juntas-01.json` tiene
`"id": "caso-juntas-01"` (misma regla que M5 y M14). El descubrimiento de casos filtra por el
prefijo **`caso-juntas-`**: `matices.json` (banco de estilo por personalidad, M16 Fase 3) vive
en la misma carpeta y **no** es un caso.

## Esquema

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
      "ejemplosDePregunta": ["como identifican a cada equipo", "que datos manejan del equipo"],
      "respuesta": "Cada equipo se registra con su nombre y el ano de fundacion.",
      "emotionTag": "neutral",
      "animationCue": "idle"
    }
  ]
}
```

Campo por campo:

| Campo | Tipo | Notas |
|---|---|---|
| `id` | string | == nombre de archivo sin `.json` |
| `cliente.empresa` / `.rol` / `.proyecto` / `.contexto` | string | Contexto narrativo; `RequirementResponder.Respond` **no** lo consulta (mismo rol que `paciente` en M14, salvo `clave`) |
| `requerimientos` | array, **mínimo 4** (`MinimoRequerimientos`) | Tabla de recuperación + puerta de receptividad |
| `requerimientos[].id` | string | Único dentro del caso, estable, minúsculas, sin tildes. Va a `RequirementId` |
| `requerimientos[].receptividadMinima` | string | `"NoReceptivo"` \| `"Neutral"` \| `"Receptivo"` — **texto, no número ni enum**. `JsonUtility` mapea enums por valor numérico, no por nombre: un `"Receptivo"` contra un campo `Receptivity` quedaría en `0` (`Neutral`) en silencio. Con `string` el cargador controla el mapeo y un nombre desconocido o ausente descarta ese requerimiento en vez de aflojar la puerta sin avisar |
| `requerimientos[].ejemplosDePregunta` | string[], **mínimo 2** | Ver reglas de redacción abajo |
| `requerimientos[].respuesta` | string | En primera persona del cliente. Sale **intacta** cuando la puerta da `Revelado` |
| `requerimientos[].emotionTag` | string, opcional | Default `"neutral"` si se omite |
| `requerimientos[].animationCue` | string, opcional | Default `"idle"` si se omite |

## Reglas de redacción de `ejemplosDePregunta`

- **Mínimo 2 por requerimiento, mínimo 2 palabras de contenido cada uno**, y específicos del
  tema.
- El emparejador (`RequirementMatcher`) declara coincidencia cuando *todas* las palabras de
  *algún* ejemplo aparecen en la pregunta del estudiante. Un ejemplo genérico como
  `"el proyecto"` emparejaría casi cualquier frase y rompería el caso de control social
  (`"que clima hace hoy"` DEBE dar `NoAplica`). Por eso cada ejemplo lleva sustantivos propios
  del dominio (`"equipo"`, `"inventario"`, `"tripulante"`...), nunca palabras sueltas
  ambiguas.
- Los ejemplos de este catálogo se redactaron a partir de las frases reales de
  `Data/Corpus/juntas.json` (columna `SolicitudRespetuosa`/`SolicitudAgresiva`/etc. para el
  mismo tema), normalizadas a minúsculas y sin signos de puntuación — el propio
  `RequirementMatcher.Normalizar` hace ese trabajo en tiempo de ejecución, así que la forma
  exacta en el JSON no necesita tildes ni mayúsculas.

## Cobertura de receptividad

Cada caso DEBE declarar al menos un requerimiento con `receptividadMinima == "NoReceptivo"`,
al menos uno con `"Neutral"` y al menos uno con `"Receptivo"` (analogía con el mínimo de
`ClinicalCasesDataTests` de M14): la progresión de receptividad tiene que ser observable, no
un todo-o-nada disfrazado.

## `matices.json` — no es un caso

`Data/Requirements/matices.json` (M16 Fase 3) es el banco de estilo por personalidad que usa
`PersonalityStyleBank`: un prefijo de revelación y una lista de frases de desvío por cada
`PersonalityId` de `perfiles-personalidad-m5`. No declara `id` de caso ni
`receptividadMinima`; el descubrimiento de casos lo ignora por no calzar con el prefijo
`caso-juntas-`. Este README documenta el esquema de los **casos**; el esquema de
`matices.json` vive en `openspec/changes/2026-09-16-m16-catalogo-respondedor-juntas/design.md`
(AD6).

## Notas de transcripción desde `Data/Corpus/juntas.json`

`Data/Corpus/juntas.json` es la única fuente narrativa permitida: ningún requerimiento agrega
un hecho, cifra o entidad que ese corpus no mencione ya. Puntos donde la transcripción exigió
una decisión:

- **`caso-juntas-01` (torneo de fútbol) no incluye `calendario` ni `presupuesto`.** El corpus
  solo *pregunta* por el calendario del torneo (`SolicitudRespetuosa`/`SolicitudAgresiva`);
  ninguna frase `AportaInformacion` explica cómo se arma. Y el corpus no menciona
  "presupuesto" en ningún punto de los 4 dominios. Ambos aparecían en el ejemplo de
  `design.md` (que los declaraba como decisión pendiente del catálogo, no del diseño); se
  retiraron aquí por no estar narrados. Los 6 requerimientos reales de fútbol
  (`equipos`, `jugadores`, `partidos`, `estadio`, `arbitros`, `estadisticas`) alcanzan el
  mínimo sin necesidad de ese contenido. Consecuencia conocida (ya anticipada como riesgo R1
  del `design.md`): la frase de prueba `"cual es el presupuesto del proyecto"` de
  `RequirementResponderContract` no tiene match contra el catálogo real, así que los
  `[Test]` que dependen de ella quedan omitidos por `Assume` contra la implementación real
  (no contra el doble, que sí trae un requerimiento sintético `"presupuesto"` por diseño de
  Fase 2). Es contenido del catálogo, no un defecto de M16: forzar la palabra hubiera violado
  la regla de fidelidad al corpus.
- **`caso-juntas-04` (aerolínea) separa `pasajero` en dos requerimientos** —
  `pasajero_identificacion` (pasaporte/cédula y nacionalidad) y `pasajero_multivuelo` (un
  pasajero puede viajar en muchos vuelos) — porque el corpus narra esos dos hechos en líneas
  distintas (`AportaInformacion`, tono `Respetuoso` y tono `Neutral` respectivamente). No es
  contenido nuevo: es la misma información ya narrada, separada en dos entradas de la tabla
  porque cada una responde una pregunta distinta.
- **`caso-juntas-03` (colegio) solo llega a 4 requerimientos, no a 6.** El corpus narra,
  para este dominio, exactamente 4 hechos con `AportaInformacion` (rangos de edad, alergias y
  restricciones alimentarias, que los padres vean/cambien el menú, y el descuento del 20% a
  empleados). Otros temas del dominio (registro con usuario y contraseña, reserva del
  almuerzo, beneficio por pago anticipado) solo aparecen como **preguntas** en el corpus
  (`SolicitudRespetuosa`/`SolicitudAgresiva`/etc.), nunca como una frase que dé la respuesta.
  Inventar esa respuesta para llegar a 6 habría violado la regla de fidelidad al corpus. En
  vez de eso, Jefferson bajó `MinimoRequerimientos` de 6 a 4 globalmente (commit `aa44564`,
  2026-09-18, tarea 1.5 de `tasks.md`) — este archivo con 4 requerimientos **sí cumple** el
  mínimo vigente, no es un riesgo abierto.

## Ampliar el catálogo

Agregar un caso es crear `Data/Requirements/caso-juntas-0N.json` con este esquema — cero
clases, cero cambio de código, siempre que el contenido salga de
`Data/Corpus/juntas.json` (o de la fuente narrativa que corresponda). Ampliar
`ejemplosDePregunta` de un requerimiento existente (más cobertura de preguntas) es edición de
datos, no reapertura del cambio SDD.
