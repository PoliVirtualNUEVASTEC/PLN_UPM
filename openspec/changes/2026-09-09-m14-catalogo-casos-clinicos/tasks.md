# Tasks: M14 — Catálogo de casos clínicos

## Review Workload Forecast

| Field | Value |
|---|---|
| Estimated changed lines | ~230 (README ~70, 3 JSON ~50 c/u, `git mv` del `.md` 0, sección MODULES ~15) |
| 400-line budget risk | Bajo |
| Chained PRs recommended | No |
| Suggested split | PR único |
| Chain strategy | n/a |

### Suggested Work Units

| Unit | Meta | PR | Prueba enfocada | Frontera de rollback |
|---|---|---|---|---|
| 1 | Catálogo de datos + README + sección MODULES | PR único | Manual: cada JSON valida contra el esquema del README; los 3 triajes esperados son II/IV/II; revisión cruzada de fidelidad | Borrar `Data/Cases/`; devolver `Casos_Medicos.md` a la raíz. Ningún código lo referencia |

## Phase 0: Guardrails (leer antes de escribir)

- [ ] 0.1 Frontera de escritura: SOLO `Data/Cases/`, `Docs/MODULES.md` (sección M14) y este
  directorio de cambio. Más el `git mv Casos_Medicos.md Data/Cases/Casos_Medicos.md`.
- [ ] 0.2 **Cero código.** M14 no crea ninguna clase C#, ningún `.asmdef`, ninguna prueba.
  El tipo `ClinicalCase` y las pruebas de datos son de M15. Si algo parece necesitar
  código, parar y avisar — está mal cortado.
- [ ] 0.3 No tocar `Data/Corpus/` (M3) ni `Data/Personalities/` (M5) ni `NpcAi.Core`.
- [ ] 0.4 `2026-09-09-m0-puerto-respuesta-clinica` puede no estar mergeado todavía: no
  importa, M14 no compila nada. Solo respeta la convención "nombre de archivo ==
  `ClinicalCaseId.Value`".
- [ ] 0.5 Módulo de datos: `/sdd-ff` es admisible aquí (no está en la lista de módulos de
  IA M1/M2/M4/M6), pero el equipo puede optar por el flujo completo dado que M14 fija un
  esquema del que dependen M15 y M9.

## Phase 1: Esquema y README

- [ ] 1.1 Redactar `Data/Cases/README.md`: propósito; esquema campo por campo (ver
  `design.md` → Esquema); regla `id` == nombre de archivo; mínimo de `hechos` por caso
  (arrancar en 8, confirmar con M15); `clave` es **solo** para M9, M15 tiene prohibido
  leerla; diferencia explícita con `Data/Corpus/` (M3, lo que dice el usuario) y
  `Data/Dialogue/` (M6, turnos sociales del NPC).
- [ ] 1.2 Fijar los valores válidos de `triajeEsperado`: `"I"`, `"II"`, `"III"`, `"IV"`,
  `"V"` (cadena romana). Documentar el significado clínico de cada uno en el README
  (tiempos de atención) para que M9 tenga referencia.

## Phase 2: Transcribir los 3 casos

- [ ] 2.1 `git mv Casos_Medicos.md Data/Cases/Casos_Medicos.md` (preservar historia).
- [ ] 2.2 `Data/Cases/caso-01.json` (Mariana, 38): `paciente` (cefalea 2 meses, no cede con
  acetaminofén ~6/día, mareo, inestabilidad, diaforesis, disartria; antecedentes Diabetes +
  Depresión; alergia Tramadol; FC 102, TA 163/99, FR 23, SatO2 94, Glasgow 15/15);
  `hechos` ≥ 8 (inicio, evolución, analgesia consumida, mareo, habla, antecedentes,
  alergias, presión); `clave` (`triajeEsperado: "II"`, `tiempoAtencion: "< 30 min"`,
  banderas rojas y cierre del original).
- [ ] 2.3 `Data/Cases/caso-02.json` (María Rosa, 53): odinofagia 5 días, sin
  expectoración ni disnea, otitis+faringitis tratada hace 2 semanas, COVID-19 el año
  anterior con buena recuperación; antecedente HTA; FC 70, TA 151/97, FR 17, SatO2 95,
  Temp 36.4; `hechos` ≥ 8; `clave` (`triajeEsperado: "IV"`, `tiempoAtencion: "1 a 4 h o
  consulta prioritaria"`, signos de alarma para volver, cierre).
- [ ] 2.4 `Data/Cases/caso-03.json` (Sofía, 34): TEC leve hace 2 horas por caída desde su
  propia altura, golpe temporo-parietal, hematoma ~4 cm sin ruptura de piel, sin pérdida de
  conciencia, orientada, dolor 7/10; FC 102, TA 130/85, FR 19, SatO2 100, Glasgow 15/15;
  `hechos` ≥ 8; `clave` (`triajeEsperado: "II"`, `tiempoAtencion: "< 30 min"`, banderas:
  zona del pterión / arteria meníngea media, riesgo de deterioro tardío con intervalo
  lúcido, magnitud del hematoma, dolor + taquicardia refleja).
- [ ] 2.5 En cada archivo, revisar que ninguna `hechos[].respuesta` contiene el valor de
  `clave.triajeEsperado` ni una `banderaRoja` textual (el paciente no se autodiagnostica).
- [ ] 2.6 Marcar en el README los puntos donde `Casos_Medicos.md` está corrupto por OCR
  ("intervalo lúdico" → "lúcido"; radios/diámetros sin número en casos 1 y 3) y qué se
  asumió al transcribir.

## Phase 3: Revisión y documentación

- [ ] 3.1 Revisión cruzada de fidelidad clínica por el asesor o un segundo integrante del
  equipo (mismo espíritu que el doble etiquetado del 10% de `Data/Corpus/README.md`).
- [ ] 3.2 Agregar la sección **M14 — Catálogo de casos clínicos** a `Docs/MODULES.md`:
  carpeta `Data/Cases`, dueño Luis, qué hace, esquema resumido, consumidores (M15, M9),
  estado ("dato real, 3 casos, meta de ampliación documentada en el README").
- [ ] 3.3 Revisar que el diff no toca `Runtime/`, `NpcAi.Core`, `Data/Corpus/`,
  `Data/Personalities/` ni nada fuera de la frontera de 0.1.

## Phase 4: Cierre (acciones del autor)

- [ ] 4.1 `git add` solo de `Data/Cases/`, `Docs/MODULES.md` y el directorio de cambio;
  `git diff --cached` antes de commitear.
- [ ] 4.2 Checklist "Antes de mergear" del `README.md` (para un módulo de datos: diff
  acotado, spec/design/tasks archivados, decisiones registradas; no aplica "pruebas
  EditMode propias" porque M14 no tiene código — la verificación ejecutable llega con M15).
- [ ] 4.3 Al cerrar: mover el cambio a `openspec/changes/archive/`, crear
  `openspec/specs/catalogo-casos-clinicos-m14/spec.md` (requisitos DEBE/NO DEBE con
  Dado/Cuando/Entonces: alcance 3 casos + cero clases, esquema, `id` == archivo,
  `triajeEsperado` válido, `signosVitales` completos, `hechos` ≥ mínimo, `clave` no se
  filtra; trazabilidad **hacia adelante** a las pruebas de M15), `archive-report.md`.
- [ ] 4.4 PR mergeado a `main` por el autor (regla 8).
- [ ] 4.5 Registrar en el documento de contexto y en Engram: "M14 catálogo de casos
  clínicos — esquema paciente/hechos/clave, 3 casos" (regla 10).

## Notas

- M14 puede avanzar **en paralelo** con `2026-09-09-m0-puerto-respuesta-clinica`.
- El esquema de M14 se congela **de facto** cuando M15 lo consume: si M15 pide un campo
  nuevo, se agrega en M14 antes de archivar cualquiera de los dos cambios.
- Ampliar el catálogo a más de 3 casos después de mergear es edición de datos, no
  reapertura de este cambio.
