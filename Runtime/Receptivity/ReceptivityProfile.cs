using System.Collections.Generic;
using NpcAi.Core;

namespace NpcAi.Receptivity
{
    /// <summary>
    /// Parametros de receptividad de UNA personalidad. Es un contenedor de datos:
    /// no decide transiciones, solo expone los numeros que el motor de M4 suma.
    /// C# puro, sin una sola referencia a UnityEngine; los perfiles reales de M5
    /// se proyectan a este tipo sin que el motor cambie una linea.
    /// </summary>
    public sealed class ReceptivityProfile
    {
        /// <summary>Puntaje en o por encima del cual el estado es <see cref="NpcAi.Core.Receptivity.Receptivo"/>.</summary>
        public int UmbralReceptivo { get; }

        /// <summary>Puntaje en o por debajo del cual el estado es <see cref="NpcAi.Core.Receptivity.NoReceptivo"/>.</summary>
        public int UmbralNoReceptivo { get; }

        /// <summary>
        /// Cota simetrica del puntaje acumulado: el motor lo satura en
        /// <c>[-LimitePuntaje, +LimitePuntaje]</c>. Se guarda tal cual llega
        /// (contrato del productor, no invariante del contenedor).
        /// </summary>
        public int LimitePuntaje { get; }

        /// <summary>
        /// Puntaje con el que arranca esta personalidad tras <c>Reset</c>. El estado
        /// inicial NO es un dato aparte: sale de este puntaje contra los umbrales,
        /// asi que el puntaje es la unica fuente de verdad.
        /// </summary>
        public int PuntajeInicial { get; }

        private readonly IReadOnlyDictionary<Intent, int> _porIntencion;
        private readonly IReadOnlyDictionary<Tone, int> _porTono;
        private readonly IReadOnlyDictionary<PhysicalAction, int> _porAccion;

        public ReceptivityProfile(
            int umbralReceptivo,
            int umbralNoReceptivo,
            int limitePuntaje,
            int puntajeInicial,
            IReadOnlyDictionary<Intent, int> porIntencion,
            IReadOnlyDictionary<Tone, int> porTono,
            IReadOnlyDictionary<PhysicalAction, int> porAccion)
        {
            UmbralReceptivo   = umbralReceptivo;
            UmbralNoReceptivo = umbralNoReceptivo;
            LimitePuntaje     = limitePuntaje;
            PuntajeInicial    = puntajeInicial;

            // Nunca null: un accesor sin tabla devuelve 0, no lanza. Misma linea
            // que ReceptivityChange con ReasonCode o NpcReply con sus strings.
            _porIntencion = porIntencion ?? Vacio<Intent>();
            _porTono      = porTono      ?? Vacio<Tone>();
            _porAccion    = porAccion    ?? Vacio<PhysicalAction>();
        }

        /// <summary>Delta de puntaje por la intencion. <c>0</c> si la personalidad no la puntua.</summary>
        public int DeltaPorIntencion(Intent intent) =>
            _porIntencion.TryGetValue(intent, out var delta) ? delta : 0;

        /// <summary>Ajuste de puntaje por el tono. <c>0</c> si la personalidad no lo puntua.</summary>
        public int DeltaPorTono(Tone tone) =>
            _porTono.TryGetValue(tone, out var delta) ? delta : 0;

        /// <summary>Delta de puntaje por la accion fisica. <c>0</c> si la personalidad no la puntua.</summary>
        public int DeltaPorAccion(PhysicalAction action) =>
            _porAccion.TryGetValue(action, out var delta) ? delta : 0;

        private static IReadOnlyDictionary<TClave, int> Vacio<TClave>() =>
            new Dictionary<TClave, int>();

        /// <summary>
        /// Perfil neutro para <see cref="PersonalityId.None"/> y para cualquier id que el
        /// catalogo no reconozca. Numeros conservadores: la agresion resta, la empatia
        /// suma, el resto es leve. Garantiza que el motor nunca opere sin perfil.
        /// </summary>
        public static ReceptivityProfile Default { get; } = new ReceptivityProfile(
            umbralReceptivo:    2,
            umbralNoReceptivo: -2,
            limitePuntaje:      4,
            puntajeInicial:     0,
            porIntencion: new Dictionary<Intent, int>
            {
                [Intent.SolicitudRespetuosa] =  1,
                [Intent.Empatia]             =  2,
                [Intent.AportaInformacion]   =  1,
                [Intent.SolicitudAgresiva]   = -2,
                [Intent.Interrupcion]        = -1,
                [Intent.PreguntaFueraDeTema] = -1,
            },
            porTono: new Dictionary<Tone, int>
            {
                [Tone.Respetuoso] =  1,
                [Tone.Empatico]   =  1,
                [Tone.Agresivo]   = -1,
            },
            porAccion: new Dictionary<PhysicalAction, int>
            {
                [PhysicalAction.ContactoVisual] =  1,
                [PhysicalAction.Acercarse]      =  1,
                [PhysicalAction.GestoCalma]     =  1,
                [PhysicalAction.EntregarObjeto] =  1,
                [PhysicalAction.Alejarse]       = -1,
            });
    }
}
