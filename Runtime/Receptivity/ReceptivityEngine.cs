using NpcAi.Core;

namespace NpcAi.Receptivity
{
    /// <summary>
    /// Motor real de M4: maquina de estados Receptivo / Neutral / NoReceptivo
    /// parametrizada por los perfiles de personalidad. C# puro, sin una sola
    /// referencia a UnityEngine; la tabla de transiciones se prueba en milisegundos.
    /// <para>
    /// El puntaje acumulado es la unica fuente de verdad: el estado siempre es
    /// funcion del puntaje contra los umbrales del perfil.
    /// </para>
    /// </summary>
    public sealed class ReceptivityEngine : IReceptivityEngine
    {
        private readonly ReceptivityProfileCatalog _catalogo;

        private ReceptivityProfile _perfil = ReceptivityProfile.Default;
        private int _puntaje;

        /// <summary>Usa el reparto de <see cref="ReceptivityProfileCatalog.Standard"/>.</summary>
        public ReceptivityEngine() : this(ReceptivityProfileCatalog.Standard()) { }

        public ReceptivityEngine(ReceptivityProfileCatalog catalogo)
        {
            _catalogo = catalogo ?? ReceptivityProfileCatalog.Standard();
        }

        /// <summary><see cref="NpcAi.Core.Receptivity.Neutral"/> hasta el primer <see cref="Reset"/>.</summary>
        public Core.Receptivity Current { get; private set; } = Core.Receptivity.Neutral;

        public void Reset(PersonalityId personality)
        {
            _perfil  = _catalogo.PerfilDe(personality);
            _puntaje = Saturar(_perfil.PuntajeInicial);
            Current  = EstadoPara(_puntaje);
        }

        public ReceptivityChange Evaluate(IntentResult intent, PhysicalAction action)
        {
            var desde = Current;

            var delta = Blindar(intent.Intent, DeltaBruto(intent, action));

            _puntaje = Saturar(_puntaje + delta);
            Current  = EstadoPara(_puntaje);

            return new ReceptivityChange(desde, Current, _puntaje, Razon(intent.Intent, action, delta));
        }

        // ------------------------------------------------------------------ interna

        private int DeltaBruto(IntentResult intent, PhysicalAction action) =>
              _perfil.DeltaPorIntencion(intent.Intent)
            + _perfil.DeltaPorTono(intent.Tone)
            + _perfil.DeltaPorAccion(action);

        /// <summary>
        /// Blindaje del contrato: la agresion nunca suma y la empatia nunca resta,
        /// pase lo que pase en los numeros del perfil.
        /// </summary>
        private static int Blindar(Intent intent, int delta)
        {
            if (intent == Intent.SolicitudAgresiva && delta > 0) return 0;
            if (intent == Intent.Empatia          && delta < 0) return 0;
            return delta;
        }

        private int Saturar(int puntaje)
        {
            var limite = _perfil.LimitePuntaje;
            if (limite < 0) limite = -limite;

            return puntaje >  limite ?  limite
                 : puntaje < -limite ? -limite
                 :                       puntaje;
        }

        private Core.Receptivity EstadoPara(int puntaje) =>
              puntaje >= _perfil.UmbralReceptivo   ? Core.Receptivity.Receptivo
            : puntaje <= _perfil.UmbralNoReceptivo ? Core.Receptivity.NoReceptivo
            :                                        Core.Receptivity.Neutral;

        private static string Razon(Intent intent, PhysicalAction action, int delta)
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
    }
}
