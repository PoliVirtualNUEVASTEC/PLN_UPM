# M3 — Corpus y etiquetado

Frases en espanol colombiano hablado, etiquetadas por intencion y tono.

- Meta: **60-100 frases por categoria de intencion, por escenario**.
- Formato: JSON, un archivo por escenario (`emergencia.json`, `juntas.json`).
- Esquema por entrada: `{ "text": "...", "intent": "...", "tone": "...", "scenario": "...", "labeler": "..." }`.
- Los valores validos de `intent` y `tone` son exactamente los de `NpcAi.Core.Intent` y `NpcAi.Core.Tone`.

**Regla de calidad:** un 10 % del corpus lo etiquetan las dos personas por separado y se mide el acuerdo.
Si el acuerdo es bajo, las categorias estan mal definidas: se arregla el contrato de intenciones **antes** de entrenar nada.

Este trabajo no tiene codigo, arranca el dia 1 y no bloquea a nadie.
