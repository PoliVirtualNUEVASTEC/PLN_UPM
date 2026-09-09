# Propuesta: M14 — Catálogo de casos clínicos (`Data/Cases/`)

## Intent

La simulación de triaje asigna a cada NPC un **caso clínico** que el paciente "adquiere":
sus síntomas, antecedentes, alergias, signos vitales y el motivo de consulta. Hoy esos
casos viven como prosa en `Casos_Medicos.md` (3 casos) en la raíz del paquete — no hay dato
estructurado que un módulo pueda cargar, ni esquema, ni forma de agregar un caso sin
reescribir texto libre.

Este cambio crea M14: un módulo **solo de datos**, en `Data/Cases/`, con un archivo JSON
por caso y un esquema fijo. Sigue el patrón de M3 (`Data/Corpus`) y M5 (`Data/Personalities`):
"lo variable es dato, no código" (regla 7 del `CLAUDE.md` del repo). Agregar un caso será
crear un archivo JSON, cero clases.

M14 alimenta dos consumidores:

- **M15** (`2026-09-09-m15-respondedor-clinico`): usa la **tabla de hechos** del caso para
  responder como el paciente cuando la enfermera pregunta ("¿desde cuándo?", "¿alergias?").
- **M9** (`2026-09-09-m9-decision-triaje`): usa `triajeEsperado` y `banderasRojas` para
  medir el progreso del objetivo y evaluar la categoría que elige la enfermera al cerrar.

## Scope

### In Scope

- `Data/Cases/README.md`: propósito del catálogo, el esquema completo campo por campo, la
  diferencia con `Data/Corpus/` (M3, lo que dice el usuario) y con `Data/Dialogue/` (M6, lo
  que dice el NPC en turnos sociales), y la regla "un caso == un archivo, nombre de archivo
  == `id`".
- `Data/Cases/caso-01.json`, `caso-02.json`, `caso-03.json`: los 3 casos de
  `Casos_Medicos.md` transcritos al esquema.
- **Separación dato-del-paciente / clave-de-respuesta** dentro de cada archivo: un bloque
  con lo que el paciente sabe y dice (síntomas, antecedentes, alergias, signos vitales,
  tabla de hechos) y un bloque `clave` con lo que **no** debe salir por boca del NPC
  (`triajeEsperado`, `banderasRojas`, `cierreEsperado`). M15 tiene contractualmente
  prohibido leer `clave`; M9 es el único que la lee.
- Mover `Casos_Medicos.md` de la raíz del paquete a `Data/Cases/Casos_Medicos.md` como
  fuente narrativa de referencia junto a los JSON (hoy está sin trackear / recién versionado
  en la raíz).
- `openspec/specs/catalogo-casos-clinicos-m14/spec.md` (se crea al archivar): requisitos
  DEBE/NO DEBE sobre alcance, esquema, `id` == nombre de archivo, `triajeEsperado` válido,
  `signosVitales` completos, tabla de hechos no vacía, y la prohibición de que `clave`
  contamine las respuestas.
- Actualizar `Docs/MODULES.md`: sección M14 nueva.

### Out of Scope

- **El cargador JSON → objeto.** M14 es dato puro, sin código, igual que M3 no trae parser.
  El tipo C# `ClinicalCase` y su deserialización los define y prueba M15
  (`2026-09-09-m15-respondedor-clinico`), en `Tests/EditMode/ClinicalResponse/`, tal como
  las pruebas de datos de M5 viven en `Tests/EditMode/Receptivity/` (folder de M4, dueño del
  esquema).
- **El `readonly struct ClinicalCaseId`** vive en `NpcAi.Core` y lo agrega
  `2026-09-09-m0-puerto-respuesta-clinica`. M14 solo respeta la convención: el nombre de
  archivo (`caso-01`) es el `ClinicalCaseId.Value`.
- **La identidad del NPC** (nombre "Mariana", cuerpo señora mayor / joven, voz). Eso es de
  `2026-09-09-m11-armado-sesion`. El caso clínico es agnóstico del cuerpo que lo actúa: un
  mismo caso lo puede tomar cualquiera de los 2 NPC.
- **El enum `Triage` (I–V)** como tipo de código. M14 guarda `triajeEsperado` como cadena
  (`"II"`); si M9 crea un enum, mapea a esa cadena. M14 no depende de M9.
- **Ampliar el catálogo más allá de 3 casos.** Es edición de datos posterior, no reapertura
  de este cambio.

## Capabilities

### New Capabilities

- `catalogo-casos-clinicos-m14` (nuevo): primera spec formal del catálogo. Formaliza el
  esquema y los invariantes que M15 y M9 asumen, con trazabilidad a las pruebas de datos
  que entrega M15.

### Modified Capabilities

- Ninguna. M14 no toca `NpcAi.Core` ni ningún módulo de `Runtime/`.

## Approach

**Un archivo por caso, nombre == `id`** — misma regla que M5 (`grosero.asset` ↔
`personalityId: grosero`). Permite que M15 resuelva un `ClinicalCaseId` a un archivo sin
índice aparte.

**Dos bloques por archivo: paciente y clave.**

```json
{
  "id": "caso-01",
  "paciente": {
    "edad": 38,
    "acompanamiento": "sola",
    "motivoConsulta": "Dolor de cabeza que no cede desde hace 2 meses",
    "sintomas": ["...", "..."],
    "antecedentes": ["Diabetes Mellitus", "Depresion"],
    "alergias": ["Tramadol"],
    "medicacionActual": ["Acetaminofen, ~6 comprimidos/dia"],
    "signosVitales": {
      "fcLpm": 102, "taMmHg": "163/99", "frRpm": 23,
      "satO2Pct": 94, "glasgow": "15/15", "temperaturaC": null
    }
  },
  "hechos": [
    { "campo": "inicio_sintoma",
      "ejemplosDePregunta": ["desde cuando", "hace cuanto", "cuando empezo"],
      "respuesta": "Hace unos dos meses, doctora, y no se me quita." },
    { "campo": "alergias",
      "ejemplosDePregunta": ["es alergica", "alergica a algun medicamento", "alergias"],
      "respuesta": "Si, soy alergica al Tramadol." }
  ],
  "clave": {
    "triajeEsperado": "II",
    "tiempoAtencion": "< 30 min",
    "banderasRojas": ["deficit neurologico focal agudo (disartria)", "crisis hipertensiva 163/99", "..."],
    "cierreEsperado": "Explicar la clasificacion y el paso prioritario a urgencias en < 30 min; notificar al medico de urgencias."
  }
}
```

**`hechos` es el contrato M14 ↔ M15.** Cada entrada: `campo` (clave estable), `ejemplosDePregunta`
(frases con las que la enfermera podría preguntar eso — M15 empareja contra ellas), `respuesta`
(el hecho, en primera persona, como lo diría el paciente). M15 nunca inventa un hecho: si no
hay entrada que empareje, devuelve `Handled == false` y el turno va a M6.

**`clave` es dato de evaluación, no de conversación.** El paciente no sabe su propia
clasificación de triaje ni cuáles son sus banderas rojas. La spec de M14 exige que M15 no
lea `clave`; M9 la usa para el puntaje y el veredicto de cierre.

**Signos vitales como objeto de campos nombrados**, no cadena libre, para que M9 pueda
razonar sobre ellos (p. ej. "TA 163/99 es crisis hipertensiva") y M15 los pueda leer para
responder "¿cuánto tengo de presión?". `temperaturaC: null` cuando el caso no la reporta.

## Affected Areas

| Área | Impacto | Descripción |
|---|---|---|
| `Data/Cases/README.md` | Nuevo | Esquema y reglas del catálogo |
| `Data/Cases/caso-01.json`, `caso-02.json`, `caso-03.json` | Nuevo | Los 3 casos estructurados |
| `Data/Cases/Casos_Medicos.md` | Movido | Desde la raíz del paquete; fuente narrativa de referencia |
| `Casos_Medicos.md` (raíz) | Borrado | Se mueve a `Data/Cases/` |
| `Docs/MODULES.md` | Modificado | Sección M14 nueva |
| `openspec/specs/catalogo-casos-clinicos-m14/spec.md` | Nuevo (al archivar) | Primera spec formal |
| `Runtime/`, `NpcAi.Core` | Sin cambio | M14 no tiene código |

## Risks

| Riesgo | Prob. | Mitigación |
|---|---|---|
| La tabla de hechos de 3 casos es muy chica y la conversación se siente pobre | Alta al inicio | Aceptado para la primera entrega: el criterio es que el esquema soporte la conversación, no que el catálogo sea grande. Ampliar `hechos` por caso es edición de datos. Fijar un mínimo (p. ej. 8 hechos/caso) en la spec |
| M15 termina necesitando un campo que el esquema no tiene | Media | El esquema se congela **junto con** M15 (los dos cambios son cercanos en el tiempo y M15 depende de M14). Si M15 pide un campo, se agrega en M14 antes de archivar cualquiera de los dos |
| `clave` se filtra a una respuesta del NPC | Media | Requisito DEBE/NO DEBE explícito en la spec de M14 + prueba en M15 (`ClinicalCasesDataTests` / `ClinicalResponderTests`) que verifica que ninguna `respuesta` de `hechos` contiene el texto de `triajeEsperado` ni de `banderasRojas` |
| Transcribir `Casos_Medicos.md` introduce errores clínicos (el OCR del PDF original ya tiene ruido: "intervalo lúdico" por "lúcido", radios sin número) | Media | Revisión cruzada por el asesor o un segundo integrante (mismo espíritu que el doble etiquetado del 10% de M3). Marcar en el README los puntos donde el original está corrupto y qué se asumió |
| `Data/Cases/` sin `.meta` al mover archivos rompe referencias en Unity | Baja | Los JSON se cargan por ruta/`Resources`/`TextAsset`, no por GUID de `.meta`; Unity regenera los `.meta` al reimportar. No hay prefab ni escena que referencie estos archivos todavía |

## Rollback Plan

M14 es dato aditivo sin consumidores mergeados (M15 y M9 vienen después). Revertir los
commits borra `Data/Cases/` y devuelve `Casos_Medicos.md` a la raíz. Nada de `Runtime/` se
ve afectado. No hay migración.

## Dependencies

- `2026-09-09-m0-puerto-respuesta-clinica`: solo comparte la **convención** de nombres
  (`ClinicalCaseId.Value` == nombre de archivo). M14 puede redactarse en paralelo; no
  necesita el puerto compilado.
- `Casos_Medicos.md` (fuente narrativa), ya en la raíz del paquete.
- Ninguna dependencia de red ni de código.

## Success Criteria

- [ ] `Data/Cases/` tiene exactamente 3 archivos `caso-*.json` + `README.md` +
      `Casos_Medicos.md`.
- [ ] Cada `caso-*.json` valida contra el esquema del README: bloques `paciente`, `hechos`,
      `clave`; `id` == nombre de archivo; `triajeEsperado` ∈ {`I`,`II`,`III`,`IV`,`V`};
      `signosVitales` con los 6 campos nombrados; `hechos` con al menos el mínimo fijado.
- [ ] Los 3 casos reproducen fielmente `Casos_Medicos.md` (triaje esperado: II, IV, II).
- [ ] Ninguna `respuesta` de `hechos` contiene el `triajeEsperado` ni una bandera roja
      textual (verificado por inspección ahora, por prueba cuando M15 exista).
- [ ] `Docs/MODULES.md` tiene la sección M14.
- [ ] El diff no toca `Runtime/`, `NpcAi.Core` ni ninguna carpeta fuera de `Data/Cases/`,
      `Docs/MODULES.md` y `openspec/` (más el borrado de `Casos_Medicos.md` en la raíz).

## Decisiones del usuario (confirmadas 2026-09-09)

1. El caso clínico es un **módulo de datos nuevo** (M14), patrón M3/M5.
2. El NPC responde los hechos del caso vía M15 (módulo dedicado), que consume la **tabla de
   hechos** de M14.
3. La numeración M14 es sugerida; el equipo la confirma al abrir el cambio.
