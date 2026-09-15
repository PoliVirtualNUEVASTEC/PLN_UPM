using System.Collections.Generic;
using NpcAi.Core;

namespace NpcAi.ClinicalResponse.Fakes
{
    /// <summary>
    /// Doble determinista de M15. Tabla de hechos embebida (sin leer <c>Data/Cases/</c>):
    /// sirve para que otros modulos (M4, M8, M11) integren contra <see cref="IClinicalResponder"/>
    /// sin depender de M14. C# puro, sin una sola referencia a UnityEngine.
    /// <para>
    /// Reconoce los mismos 3 ids que el catalogo real (<c>caso-01</c>, <c>caso-02</c>,
    /// <c>caso-03</c>) como "casos existentes" — cualquier otro id (incluido
    /// <c>"no-existe"</c>, que usa <c>ClinicalResponderContract</c>) deja <c>IsReady</c> en
    /// <c>false</c>, igual que haria la implementacion real ante un caso que no existe en disco.
    /// </para>
    /// </summary>
    public sealed class ScriptedClinicalResponder : IClinicalResponder
    {
        private static readonly HashSet<string> IdsConocidos =
            new HashSet<string> { "caso-01", "caso-02", "caso-03" };

        private static readonly Hecho[] HechosEmbebidos =
        {
            new Hecho("inicio_sintoma",
                new[] { "desde cuando", "hace cuanto", "cuando empezo" },
                "Desde hace un par de dias, doctora."),
            new Hecho("alergias",
                new[] { "es alergica", "alergica a algo", "tiene alergias" },
                "No, no soy alergica a nada."),
            new Hecho("dolor",
                new[] { "cuanto le duele", "escala de dolor", "que tanto dolor siente" },
                "El dolor esta como en un 5 de 10."),
        };

        private ClinicalCaseId _caseId = ClinicalCaseId.None;

        public bool IsReady => IdsConocidos.Contains(_caseId.Value ?? string.Empty);

        public void AssignCase(ClinicalCaseId caseId, PersonalityId personality)
        {
            _caseId = caseId;
        }

        public ClinicalResponse Respond(Utterance nurseUtterance, IntentResult intent)
        {
            if (!IsReady) return ClinicalResponse.NoAplica;

            var normalizado = ClinicalFactMatcher.Normalizar(nurseUtterance.Text);
            var indice = ClinicalFactMatcher.Match(normalizado, HechosEmbebidos);

            return indice < 0
                ? ClinicalResponse.NoAplica
                : new ClinicalResponse(true, new NpcReply(HechosEmbebidos[indice].Respuesta, "neutral", "idle"));
        }
    }
}
