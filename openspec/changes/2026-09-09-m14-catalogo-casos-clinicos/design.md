# Design: M14 — Catálogo de casos clínicos

## Technical Approach

Datos puros en `Data/Cases/`, sin código. Un archivo JSON por caso, esquema fijo con tres
bloques: `paciente` (lo que el NPC sabe y dice), `hechos` (tabla de recuperación para M15),
`clave` (dato de evaluación para M9, prohibido para M15). El nombre de archivo es el
`ClinicalCaseId`.

### Unidad de módulo y grafo de referencias

    Data/Cases/   — fuera de todo .asmdef; archivos .json cargados como TextAsset

Mismo tratamiento que `Data/Corpus/*.json` (M3) y `Data/Personalities/*.asset` (M5): no
compila, no referencia nada. El tipo C# que los deserializa (`ClinicalCase`) lo define M15
dentro de `NpcAi.ClinicalResponse`, no M14.

## Architecture Decisions

| # | Decisión | Elección | Alternativas rechazadas | Razón |
|---|---|---|---|---|
| AD1 | Formato | JSON, un archivo por caso | Un solo `casos.json`; ScriptableObject `.asset` | JSON un-archivo-por-caso espeja M3 (`emergencia.json`/`juntas.json` son por escenario) y M5 (un `.asset` por personalidad); es la unidad natural de edición y de `git blame`. `.asset` obligaría a un editor de Unity y a un tipo serializable de algún módulo — rompe "cero clases" |
| AD2 | `id` y nombre de archivo | Iguales (`caso-01.json` ↔ `"id": "caso-01"`) | `id` libre + índice `cases.json` | Regla ya probada en M5; deja que M15 resuelva `ClinicalCaseId → archivo` sin índice |
| AD3 | Separar `paciente` de `clave` | Dos bloques en el mismo archivo | Dos archivos (`caso-01.json` + `caso-01.clave.json`); un solo bloque plano | Un archivo mantiene el caso como unidad; dos bloques dejan la frontera explícita y verificable ("M15 no lee `clave`"). Dos archivos duplican el `id` y el manejo de rutas |
| AD4 | Tabla de hechos | Lista de `{campo, ejemplosDePregunta[], respuesta}` | Diccionario `campo → respuesta` sin ejemplos; pares pregunta→respuesta literales | Los `ejemplosDePregunta` son el material que M15 empareja; sin ellos M15 tendría que adivinar el mapeo pregunta→campo. `respuesta` en primera persona porque va casi verbatim a la boca del NPC |
| AD5 | `triajeEsperado` | Cadena romana `"I"`..`"V"` | Entero `1`..`5`; enum en M14 | La cadena romana es la que usa `Casos_Medicos.md` y la literatura de triaje colombiano; no crea acoplamiento con un enum de M9. Si M9 hace un enum `Triage`, mapea a esta cadena en su frontera |
| AD6 | `signosVitales` | Objeto de 6 campos nombrados (`fcLpm`, `taMmHg`, `frRpm`, `satO2Pct`, `glasgow`, `temperaturaC`), `null` si no se reporta | Cadena libre ("FC 102, TA 163/99...") | M9 necesita leer valores para evaluar (crisis hipertensiva, taquicardia); M15 los necesita para responder "¿cuánto tengo de presión?". `taMmHg` y `glasgow` quedan como cadena (`"163/99"`, `"15/15"`) por su formato compuesto |
| AD7 | Dónde viven las pruebas de datos | En el cambio de M15 (`Tests/EditMode/ClinicalResponse/ClinicalCasesDataTests.cs`) | Un asmdef de prueba propio de M14 | Precedente de M5: sus pruebas de datos viven en `Tests/EditMode/Receptivity/` (folder de M4, dueño del esquema C#). El esquema C# (`ClinicalCase`) es de M15; sus pruebas también. Hasta que M15 exista, M14 se valida por inspección (checklist de `Success Criteria`) |

## Data Flow

    Casos_Medicos.md (narrativa)
              │  transcripción manual + revisión cruzada
              ▼
    Data/Cases/caso-01.json  { paciente, hechos, clave }
              │
      ┌───────┴────────────────────────────┐
      ▼ (ClinicalCaseId → archivo)          ▼ (solo bloque clave)
    M15 IClinicalResponder                 M9 IScenarioObjective real
    - lee paciente + hechos                - lee clave.triajeEsperado
    - NUNCA lee clave                      - lee clave.banderasRojas
    - empareja utterance ↔ hechos[].ejemplosDePregunta
    - devuelve hechos[].respuesta o Handled=false

## File Inventory

| Archivo | Rol |
|---|---|
| `Data/Cases/README.md` | Esquema campo por campo; reglas (`id` == archivo, mínimo de hechos, `clave` es solo para M9); diferencia con `Data/Corpus/` y `Data/Dialogue/` |
| `Data/Cases/caso-01.json` | Caso 1 (Mariana, 38, cefalea 2 meses) — `triajeEsperado: "II"` |
| `Data/Cases/caso-02.json` | Caso 2 (María Rosa, 53, odinofagia 5 días) — `triajeEsperado: "IV"` |
| `Data/Cases/caso-03.json` | Caso 3 (Sofía, 34, TEC leve) — `triajeEsperado: "II"` |
| `Data/Cases/Casos_Medicos.md` | Fuente narrativa, movida desde la raíz |

## Esquema (versión de referencia — la fuente de verdad es `Data/Cases/README.md`)

- `id` (string, == nombre de archivo)
- `paciente.edad` (int), `paciente.acompanamiento` (string)
- `paciente.motivoConsulta` (string)
- `paciente.sintomas` (string[])
- `paciente.antecedentes` (string[])
- `paciente.alergias` (string[])  — `[]` si niega alergias
- `paciente.medicacionActual` (string[])
- `paciente.signosVitales` { `fcLpm` int, `taMmHg` string, `frRpm` int, `satO2Pct` int, `glasgow` string, `temperaturaC` number|null }
- `hechos` (array, mínimo 8): { `campo` string, `ejemplosDePregunta` string[] (mínimo 2), `respuesta` string (primera persona) }
- `clave` { `triajeEsperado` "I".."V", `tiempoAtencion` string, `banderasRojas` string[], `cierreEsperado` string }

## Testing Strategy

| Verificación | Cuándo | Cómo |
|---|---|---|
| Estructura de cada archivo vs. esquema | Ahora (este cambio) | Inspección contra la checklist de `Success Criteria` |
| `id` == nombre de archivo; `triajeEsperado` válido; `signosVitales` completos; `hechos` ≥ 8 | Cuando exista M15 | `Tests/EditMode/ClinicalResponse/ClinicalCasesDataTests.cs` (entregado por `2026-09-09-m15-respondedor-clinico`) |
| `clave` no se filtra a `hechos[].respuesta` | Cuando exista M15 | Prueba en el mismo archivo: ninguna `respuesta` contiene `triajeEsperado` ni una `banderaRoja` como substring |
| Fidelidad a `Casos_Medicos.md` | Ahora | Revisión cruzada por el asesor o segundo integrante |

## Migration / Rollout

Sin migración de código. `Casos_Medicos.md` se mueve con `git mv` para preservar historia.
Rollback: `git revert` borra `Data/Cases/` y devuelve el `.md` a la raíz.

## Open Questions

- [ ] ¿Mínimo de `hechos` por caso? Propuesto 8; se ajusta cuando M15 muestre cuánta
      cobertura de preguntas necesita una consulta creíble.
- [ ] ¿`banderasRojas` y `cierreEsperado` en `clave` como texto libre, o estructurados
      (para que M9 puntúe por bandera)? Se decide con M9 (`2026-09-09-m9-decision-triaje`),
      que es su consumidor; M14 arranca con texto libre.
- [ ] ¿El campo `npcSugerido` (señora mayor / joven) va en el caso o lo decide M11 al azar
      sin restricción? Propuesta: no va en el caso — cualquier NPC puede actuar cualquier
      caso; M11 elige cuerpo y caso de forma independiente.
