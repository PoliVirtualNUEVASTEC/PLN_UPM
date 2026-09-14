# M14 — Catálogo de casos clínicos

Casos clínicos que un NPC-paciente "adquiere" al iniciar una sesión de triaje. Es dato
puro: ningún `.asmdef`, ninguna clase C#. El tipo que deserializa este JSON (`ClinicalCase`)
y las pruebas que validan estos archivos contra el esquema los define M15
(`Runtime/ClinicalResponse/`, `Tests/EditMode/ClinicalResponse/ClinicalCasesDataTests.cs`).

## Diferencia con otros `Data/`

- **`Data/Corpus/` (M3)**: frases etiquetadas para *entrenar/evaluar* el clasificador de
  intención (M2). No pertenece a un paciente ni a una sesión.
- **`Data/Dialogue/` (M6)**: lo que dice el NPC en turnos **sociales** (saludo, calma,
  presión) — nunca un hecho clínico.
- **`Data/Cases/` (M14, este)**: lo que dice el NPC en turnos **clínicos** — cuando la
  enfermera pregunta por síntomas, antecedentes, signos vitales o alergias, la respuesta
  sale de aquí, nunca de M6.

## Regla de archivo

**Un caso == un archivo, nombre de archivo == `id`.** `caso-01.json` tiene `"id": "caso-01"`.
Así M15 resuelve un `ClinicalCaseId` a un archivo sin índice aparte (misma regla que M5:
`grosero.asset` ↔ `personalityId: grosero`).

## Esquema

```json
{
  "id": "caso-01",
  "paciente": {
    "edad": 38,
    "acompanamiento": "sola",
    "motivoConsulta": "Dolor de cabeza que no cede desde hace 2 meses",
    "sintomas": ["..."],
    "antecedentes": ["Diabetes Mellitus", "Depresion"],
    "alergias": ["Tramadol"],
    "medicacionActual": ["Acetaminofen, ~6 comprimidos/dia"],
    "signosVitales": {
      "fcLpm": 102,
      "taMmHg": "163/99",
      "frRpm": 23,
      "satO2Pct": 94,
      "glasgow": "15/15",
      "temperaturaC": null
    }
  },
  "hechos": [
    {
      "campo": "inicio_sintoma",
      "ejemplosDePregunta": ["desde cuando", "hace cuanto", "cuando empezo"],
      "respuesta": "Hace unos dos meses, doctora, y no se me quita."
    }
  ],
  "clave": {
    "triajeEsperado": "II",
    "tiempoAtencion": "< 30 min",
    "banderasRojas": ["..."],
    "cierreEsperado": "..."
  }
}
```

Campo por campo:

| Campo | Tipo | Notas |
|---|---|---|
| `id` | string | == nombre de archivo sin `.json` |
| `paciente.edad` | int | |
| `paciente.acompanamiento` | string | p. ej. `"sola"` |
| `paciente.motivoConsulta` | string | |
| `paciente.sintomas` | string[] | |
| `paciente.antecedentes` | string[] | `[]` si no tiene |
| `paciente.alergias` | string[] | `[]` si niega alergias |
| `paciente.medicacionActual` | string[] | `[]` si no toma nada |
| `paciente.signosVitales.fcLpm` | int | frecuencia cardiaca, lpm |
| `paciente.signosVitales.taMmHg` | string | `"163/99"` (formato compuesto, no se separa) |
| `paciente.signosVitales.frRpm` | int | frecuencia respiratoria, rpm |
| `paciente.signosVitales.satO2Pct` | int | saturación de oxígeno, % |
| `paciente.signosVitales.glasgow` | string | `"15/15"` |
| `paciente.signosVitales.temperaturaC` | number \| null | `null` si el caso no la reporta |
| `hechos` | array, mínimo 8 | tabla de recuperación, ver abajo |
| `hechos[].campo` | string | clave estable, p. ej. `"alergias"` |
| `hechos[].ejemplosDePregunta` | string[], mínimo 2 | frases con las que la enfermera podría preguntar esto |
| `hechos[].respuesta` | string | en primera persona, tal como lo diría el paciente |
| `clave.triajeEsperado` | string | uno de `"I"`, `"II"`, `"III"`, `"IV"`, `"V"` (cadena romana) |
| `clave.tiempoAtencion` | string | p. ej. `"< 30 min"` |
| `clave.banderasRojas` | string[] | |
| `clave.cierreEsperado` | string | qué debe explicarle el estudiante al paciente al cerrar |

### Categorías de triaje (`clave.triajeEsperado`)

| Categoría | Significado | Tiempo de atención |
|---|---|---|
| I | Resucitación | Inmediato |
| II | Emergencia | < 30 min |
| III | Urgencia | < 1 h |
| IV | Urgencia menor | 1 a 4 h, o consulta prioritaria |
| V | Consulta simple | Sin urgencia clínica |

## `clave` es solo para M9

El paciente **no sabe** su propia clasificación de triaje ni cuáles son sus banderas rojas.
**M15 tiene prohibido leer `clave`**; solo `ClinicalCase` (el tipo que arma M15) puede omitir
ese bloque al deserializar. `clave` es exclusiva de M9 (`IScenarioObjective`), que la usa para
medir el progreso de la enfermera y evaluar el cierre. Ninguna `hechos[].respuesta` debe
contener el texto de `clave.triajeEsperado` ni de una `banderaRoja` — el paciente no se
autodiagnostica.

## Notas de transcripción desde `Casos_Medicos.md`

`Data/Cases/Casos_Medicos.md` (movido desde la raíz del paquete) es la fuente narrativa. Al
transcribir a JSON se encontraron puntos donde el original tiene ruido de OCR/transcripción:

- **Caso 3 — "Intervalo lúdico"**: el encabezado original dice "Intervalo lúdico"; el texto
  correcto en literatura de triaje es **"intervalo lúcido"** (período de conciencia normal
  antes del deterioro neurológico en un hematoma epidural). Se usa la forma correcta
  (`"lucido"`, sin el error de OCR) en `clave.banderasRojas`.
- **Caso 3 — magnitud del hematoma**: el original da "hematoma ... de aproximadamente 4cm
  de radio" en la descripción del paciente, pero en la sección de banderas rojas repite
  "Volumétrico ( de radio es equivalente a un diámetro de aproximadamente)" sin los números
  (perdidos en la transcripción original). Se usa el valor ya dado antes en el mismo
  documento: **radio ≈ 4 cm (diámetro ≈ 8 cm)**.
- **Caso 3 — "Dolor severo ()" y "taquicardia leve ()"**: la sección de banderas rojas repite
  estas frases sin número, pero los valores ya están dados en los signos vitales del mismo
  caso: dolor **7/10**, FC **102 lpm**. Se usan esos valores.
- **Caso 2 — "fiebre alta ()"**: el original no da un umbral numérico para la fiebre de
  alarma. Se usa el umbral estándar de la literatura de urgencias, **≥ 39 °C**, marcado en
  `hechos` como criterio de reconsulta, no como dato medido del paciente (su temperatura
  actual, 36.4 °C, sí es un valor medido real).

Cualquier corrección a estas asunciones es edición de datos (no reabre este cambio SDD):
basta con editar el `.json` correspondiente tras revisión cruzada con el asesor clínico.

## Ampliar el catálogo

Agregar un caso es crear `Data/Cases/caso-0N.json` con este esquema — cero clases, cero
cambio de código. Ampliar `hechos` de un caso existente (más cobertura de preguntas) es
edición de datos, no reapertura de este cambio.
