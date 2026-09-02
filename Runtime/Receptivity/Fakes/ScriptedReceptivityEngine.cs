using NpcAi.Core;

namespace NpcAi.Receptivity.Fakes
{
    /// <summary>
    /// Doble determinista de M4. Puntaje simple, umbrales fijos, sin perfiles de
    /// personalidad: el motor real (parametrizado por M5) reemplaza esto sin que
    /// nadie mas se entere. C# puro, sin una sola referencia a UnityEngine.
    /// </summary>
    public sealed class ScriptedReceptivityEngine : IReceptivityEngine
    {
        private const int UmbralReceptivo   =  2;
        private const int UmbralNoReceptivo = -2;

        private int _score;

        public Core.Receptivity Current { get; private set; } = Core.Receptivity.Neutral;

        public PersonalityId Personality { get; private set; } = PersonalityId.None;

        public void Reset(PersonalityId personality)
        {
            Personality = personality;
            _score      = 0;
            Current     = Core.Receptivity.Neutral;
        }

        public ReceptivityChange Evaluate(IntentResult intent, PhysicalAction action)
        {
            var from = Current;

            var delta  = DeltaPorIntencion(intent.Intent);
            delta     += DeltaPorAccion(action);
            _score     = Clamp(_score + delta, -4, 4);

            Current = _score >= UmbralReceptivo   ? Core.Receptivity.Receptivo
                    : _score <= UmbralNoReceptivo ? Core.Receptivity.NoReceptivo
                                                  : Core.Receptivity.Neutral;

            return new ReceptivityChange(from, Current, _score, RazonDe(intent.Intent, action, delta));
        }

        private static int DeltaPorIntencion(Intent intent) => intent switch
        {
            Intent.SolicitudRespetuosa =>  1,
            Intent.Empatia             =>  2,
            Intent.AportaInformacion   =>  1,
            Intent.SolicitudAgresiva   => -2,
            Intent.Interrupcion        => -1,
            Intent.PreguntaFueraDeTema => -1,
            _                          =>  0,
        };

        private static int DeltaPorAccion(PhysicalAction action) => action switch
        {
            PhysicalAction.ContactoVisual => 1,
            PhysicalAction.GestoCalma     => 1,
            PhysicalAction.Alejarse       => -1,
            _                             => 0,
        };

        /// <summary>
        /// Vocabulario de diagnostico. Espejo exacto de
        /// <see cref="NpcAi.Receptivity.ReceptivityEngine"/>: el doble puede mover el
        /// puntaje distinto (es mas simple e ignora el tono), pero la razon que
        /// reporta para una (intencion, accion) dada es la misma que la del motor
        /// real. La paridad la fija RazonParityTests.
        /// </summary>
        private static string RazonDe(Intent intent, PhysicalAction action, int delta)
        {
            switch (intent)
            {
                case Intent.SolicitudAgresiva:   return "AGRESION_DIRECTA";
                case Intent.Interrupcion:        return "INTERRUPCION";
                case Intent.PreguntaFueraDeTema: return "FUERA_DE_TEMA";
                case Intent.Empatia:             return "GESTO_EMPATICO";
                case Intent.SolicitudRespetuosa: return "PETICION_RESPETUOSA";
                case Intent.AportaInformacion:   return "APORTA_INFORMACION";
            }

            switch (action)
            {
                case PhysicalAction.GestoCalma:      return "GESTO_CALMA";
                case PhysicalAction.Alejarse:        return "DISTANCIAMIENTO";
                case PhysicalAction.Acercarse:       return "ACERCAMIENTO";
                case PhysicalAction.ContactoVisual:  return "CONTACTO_VISUAL";
                case PhysicalAction.TocarPaciente:   return "CONTACTO_FISICO";
                case PhysicalAction.EntregarObjeto:  return "ENTREGA_OBJETO";
                case PhysicalAction.SenalarPantalla: return "SENALA_PANTALLA";
            }

            return delta == 0 ? "SIN_CAMBIO_RELEVANTE" : "AJUSTE_MENOR";
        }

        private static int Clamp(int value, int min, int max) =>
            value < min ? min : value > max ? max : value;
    }
}
