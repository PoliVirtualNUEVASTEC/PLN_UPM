using System;
using NpcAi.Core;

namespace NpcAi.ClinicalResponse
{
    /// <summary>
    /// Implementacion real de <see cref="IClinicalResponder"/>: recupera la respuesta de la
    /// tabla de <c>hechos</c> del caso asignado, nunca la inventa. Recibe un
    /// <c>Func&lt;ClinicalCaseId,string&gt;</c> en vez de leer <c>Data/Cases/</c> directamente
    /// (AD3 de <c>design.md</c>) para quedar probable con JSON en memoria y dejar que quien
    /// arme la sesion (M11) decida de donde salen los bytes en cada plataforma.
    /// </summary>
    public sealed class ClinicalResponder : IClinicalResponder
    {
        private readonly Func<ClinicalCaseId, string> _cargarJson;

        private ClinicalCase _caso;
        private PersonalityId _personalidad = PersonalityId.None;

        /// <param name="cargarJson">
        /// De un <see cref="ClinicalCaseId"/> al texto JSON del caso, o <c>null</c> si no
        /// existe. Puede ser <c>null</c> (se trata como "ningun caso carga"): <see cref="AssignCase"/>
        /// nunca lanza sin importar como se construyo este objeto.
        /// </param>
        public ClinicalResponder(Func<ClinicalCaseId, string> cargarJson)
        {
            _cargarJson = cargarJson;
        }

        public bool IsReady => _caso != null;

        public void AssignCase(ClinicalCaseId caseId, PersonalityId personality)
        {
            _personalidad = personality;
            _caso = null;

            if (caseId.IsNone || _cargarJson == null) return;

            string json;
            try { json = _cargarJson(caseId); }
            catch (Exception) { return; }

            if (json != null && ClinicalCaseLoader.TryParse(json, out var caso))
                _caso = caso;
        }

        // "Core." es obligatorio: el namespace del modulo (NpcAi.ClinicalResponse) choca con
        // el nombre del DTO (NpcAi.Core.ClinicalResponse) — mismo problema que Core.Receptivity
        // en NpcAi.Receptivity (M4).
        public Core.ClinicalResponse Respond(Utterance nurseUtterance, IntentResult intent)
        {
            if (!IsReady) return Core.ClinicalResponse.NoAplica;

            var normalizado = ClinicalFactMatcher.Normalizar(nurseUtterance.Text);
            var indice = ClinicalFactMatcher.Match(normalizado, _caso.Hechos);
            if (indice < 0) return Core.ClinicalResponse.NoAplica;

            var hecho = _caso.Hechos[indice];
            var texto = ConMatiz(_personalidad, hecho.Respuesta);

            return new Core.ClinicalResponse(true,
                new NpcReply(texto, EmotionTagPara(hecho.Campo), AnimationCuePara(hecho.Campo)));
        }

        /// <summary>
        /// AD2/AD5 de <c>design.md</c>: prefijo fijo por personalidad, nunca una reescritura —
        /// <paramref name="respuesta"/> siempre queda intacta como subcadena del resultado.
        /// <c>PersonalityId.None</c> e "introvertido" no agregan matiz (decision de
        /// <c>proposal.md</c>); un id de personalidad desconocido tampoco, por el mismo motivo
        /// que un <c>PersonalityId</c> desconocido cae en el perfil por defecto en M4/M5.
        /// </summary>
        private static string ConMatiz(PersonalityId personalidad, string respuesta) =>
            personalidad.Value switch
            {
                "grosero" => "Ya le dije, " + respuesta,
                "empatico" => "Claro, doctora. " + respuesta,
                "histerico" => "¡Ay, doctora! " + respuesta,
                _ => respuesta,
            };

        /// <summary>AD6: tabla fija por <c>campo</c>, nunca derivada del texto de la respuesta.</summary>
        private static string EmotionTagPara(string campo) => campo switch
        {
            "dolor" => "dolor",
            "mareo" => "mareo",
            "alergias" => "alerta",
            _ => "neutral",
        };

        private static string AnimationCuePara(string campo) => campo switch
        {
            "dolor" => "gesto_dolor",
            "mareo" => "gesto_mareo",
            _ => "idle",
        };
    }
}
