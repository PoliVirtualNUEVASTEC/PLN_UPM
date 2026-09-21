using System;
using NpcAi.Core;

namespace NpcAi.RequirementResponse
{
    /// <summary>
    /// Implementacion real de <see cref="IRequirementResponder"/>: recupera la respuesta de la
    /// tabla de <c>requerimientos</c> del caso asignado, nunca la inventa. Espejo de
    /// <c>ClinicalResponder</c> (M15) con dos piezas propias de M16: la puerta de
    /// <see cref="RequirementDisclosurePolicy"/> (M15 no la tiene: responde o no responde, M16
    /// responde, desvia o no aplica) y el matiz de <see cref="PersonalityStyleBank"/> (dato, no
    /// un <c>switch</c> como <c>ClinicalResponder.ConMatiz</c>, AD6).
    /// <para>
    /// Recibe <c>Func&lt;RequirementCaseId,string&gt;</c> en vez de leer <c>Data/Requirements/</c>
    /// directamente (AD9): el nucleo queda probable con JSON en memoria y quien arme la sesion
    /// (M10/M11) decide de donde salen los bytes en cada plataforma.
    /// </para>
    /// </summary>
    public sealed class RequirementResponder : IRequirementResponder
    {
        private readonly Func<RequirementCaseId, string> _cargarJson;
        private readonly PersonalityStyleBank _banco;

        private RequirementCase _caso;
        private PersonalityId _personalidad = PersonalityId.None;

        /// <param name="cargarJson">
        /// De un <see cref="RequirementCaseId"/> al texto JSON del caso, o <c>null</c> si no
        /// existe. Puede ser <c>null</c> (se trata como "ningun caso carga"): <see cref="AssignCase"/>
        /// nunca lanza sin importar como se construyo este objeto (AD9).
        /// </param>
        /// <param name="maticesJson">
        /// Contenido de <c>Data/Requirements/matices.json</c>. Si es <c>null</c> o no valida, se
        /// usa <see cref="PersonalityStyleBank.Fallback"/> (AD10): la garantia del contrato
        /// ("con AunNoRevelado, Reply.Text nunca es vacio") no puede depender de un archivo.
        /// </param>
        public RequirementResponder(Func<RequirementCaseId, string> cargarJson, string maticesJson)
        {
            _cargarJson = cargarJson;
            _banco = PersonalityStyleBank.TryParse(maticesJson, out var banco) ? banco : PersonalityStyleBank.Fallback;
        }

        public bool IsReady => _caso != null;

        public void AssignCase(RequirementCaseId caseId, PersonalityId personality)
        {
            _personalidad = personality;
            _caso = null;

            if (caseId.IsNone || _cargarJson == null) return;

            string json;
            try { json = _cargarJson(caseId); }
            catch (Exception) { return; }

            if (json != null && RequirementCaseLoader.TryParse(json, out var caso))
                _caso = caso;
        }

        // "Core." es obligatorio: el namespace del propio modulo (NpcAi.RequirementResponse)
        // choca con el nombre del DTO (NpcAi.Core.RequirementResponse) — mismo problema y misma
        // solucion que en M15 y que Core.Receptivity en NpcAi.Receptivity (M4). El parametro
        // intent NO se usa (AD5): el turno aplica si y solo si el texto empareja.
        public Core.RequirementResponse Respond(Utterance studentUtterance, IntentResult intent, Receptivity receptivity)
        {
            if (!IsReady) return Core.RequirementResponse.NoAplica;

            var normalizado = RequirementMatcher.Normalizar(studentUtterance.Text);
            var indice = RequirementMatcher.Match(normalizado, _caso.Requerimientos);
            if (indice < 0) return Core.RequirementResponse.NoAplica;

            var requerimiento = _caso.Requerimientos[indice];
            var outcome = RequirementDisclosurePolicy.Decidir(receptivity, requerimiento.ReceptividadMinima);

            // Revelado: el prefijo de estilo antecede la respuesta INTACTA. AunNoRevelado: el
            // banco de desvio nunca recibe requerimiento.Respuesta, asi que no puede filtrarla
            // (AD6). Rotacion por indice de tabla, no por RequirementId (AD7).
            var texto = outcome == RequirementOutcome.Revelado
                ? _banco.PrefijoRevelado(_personalidad) + requerimiento.Respuesta
                : _banco.Desvio(_personalidad, indice);

            return new Core.RequirementResponse(
                outcome,
                new NpcReply(texto, requerimiento.EmotionTag, requerimiento.AnimationCue),
                new RequirementId(requerimiento.Id));
        }
    }
}
