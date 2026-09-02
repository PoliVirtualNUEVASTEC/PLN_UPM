using System.Collections.Generic;
using NpcAi.Core;

namespace NpcAi.Receptivity
{
    /// <summary>
    /// Resuelve un <see cref="PersonalityId"/> a su <see cref="ReceptivityProfile"/>.
    /// Un id desconocido o <see cref="PersonalityId.None"/> cae en
    /// <see cref="ReceptivityProfile.Default"/>: el motor nunca se queda sin perfil.
    /// <para>
    /// El catalogo se inyecta por constructor. <see cref="Standard"/> arma el reparto
    /// de las 4 personalidades de M5 con numeros placeholder; cuando M5 publique sus
    /// datos reales, un adaptador construye este mismo catalogo desde ahi sin tocar
    /// el motor.
    /// </para>
    /// </summary>
    public sealed class ReceptivityProfileCatalog
    {
        private readonly IReadOnlyDictionary<PersonalityId, ReceptivityProfile> _perfiles;

        public ReceptivityProfileCatalog(IReadOnlyDictionary<PersonalityId, ReceptivityProfile> perfiles)
        {
            _perfiles = perfiles ?? new Dictionary<PersonalityId, ReceptivityProfile>();
        }

        /// <summary>Ids que este catalogo reconoce. El resto cae en <see cref="ReceptivityProfile.Default"/>.</summary>
        public IEnumerable<PersonalityId> Personalidades => _perfiles.Keys;

        /// <summary>
        /// Perfil de esa personalidad, o <see cref="ReceptivityProfile.Default"/> si el
        /// catalogo no la conoce. La normalizacion del id la hace <see cref="PersonalityId"/>.
        /// </summary>
        public ReceptivityProfile PerfilDe(PersonalityId personality) =>
            _perfiles.TryGetValue(personality, out var perfil) ? perfil : ReceptivityProfile.Default;

        /// <summary>
        /// Reparto por defecto: las 4 personalidades del alcance de M5
        /// (grosero, histerico, introvertido, empatico). Numeros placeholder,
        /// coherentes con la descripcion de cada una; el tuning fino es dato de M5.
        /// </summary>
        public static ReceptivityProfileCatalog Standard() =>
            new ReceptivityProfileCatalog(new Dictionary<PersonalityId, ReceptivityProfile>
            {
                [new PersonalityId("grosero")]      = PerfilGrosero(),
                [new PersonalityId("histerico")]    = PerfilHisterico(),
                [new PersonalityId("introvertido")] = PerfilIntrovertido(),
                [new PersonalityId("empatico")]     = PerfilEmpatico(),
            });

        // --- Perfiles placeholder (una personalidad, un metodo: mapean 1:1 a M5) ---

        /// <summary>Extremo hostil: castiga fuerte la agresion y la torpeza, cuesta ganarselo.</summary>
        private static ReceptivityProfile PerfilGrosero() => new ReceptivityProfile(
            umbralReceptivo:    4,
            umbralNoReceptivo: -2,
            limitePuntaje:      5,
            puntajeInicial:    -2,
            porIntencion: new Dictionary<Intent, int>
            {
                [Intent.SolicitudRespetuosa] =  1,
                [Intent.Empatia]             =  2,
                [Intent.AportaInformacion]   =  1,
                [Intent.SolicitudAgresiva]   = -3,
                [Intent.Interrupcion]        = -2,
                [Intent.PreguntaFueraDeTema] = -2,
            },
            porTono: new Dictionary<Tone, int>
            {
                [Tone.Respetuoso] =  1,
                [Tone.Empatico]   =  1,
                [Tone.Agresivo]   = -2,
                [Tone.Ansioso]    = -1,
            },
            porAccion: new Dictionary<PhysicalAction, int>
            {
                [PhysicalAction.ContactoVisual] =  1,
                [PhysicalAction.GestoCalma]     =  1,
                [PhysicalAction.EntregarObjeto] =  1,
                [PhysicalAction.Alejarse]       = -1,
                [PhysicalAction.TocarPaciente]  = -2,
            });

        /// <summary>Alta reactividad emocional: exige gestos de calma, la invasion fisica lo altera.</summary>
        private static ReceptivityProfile PerfilHisterico() => new ReceptivityProfile(
            umbralReceptivo:    3,
            umbralNoReceptivo: -2,
            limitePuntaje:      5,
            puntajeInicial:     0,
            porIntencion: new Dictionary<Intent, int>
            {
                [Intent.SolicitudRespetuosa] =  1,
                [Intent.Empatia]             =  2,
                [Intent.AportaInformacion]   =  1,
                [Intent.SolicitudAgresiva]   = -3,
                [Intent.Interrupcion]        = -3,
                [Intent.PreguntaFueraDeTema] = -1,
            },
            porTono: new Dictionary<Tone, int>
            {
                [Tone.Respetuoso] =  1,
                [Tone.Empatico]   =  2,
                [Tone.Agresivo]   = -3,
                [Tone.Ansioso]    = -2,
            },
            porAccion: new Dictionary<PhysicalAction, int>
            {
                [PhysicalAction.GestoCalma]     =  3,
                [PhysicalAction.ContactoVisual] =  1,
                [PhysicalAction.Acercarse]      = -2,
                [PhysicalAction.Alejarse]       = -1,
                [PhysicalAction.TocarPaciente]  = -1,
            });

        /// <summary>Baja iniciativa: responde poco, obliga al usuario a sostener la conversacion.</summary>
        private static ReceptivityProfile PerfilIntrovertido() => new ReceptivityProfile(
            umbralReceptivo:    3,
            umbralNoReceptivo: -3,
            limitePuntaje:      4,
            puntajeInicial:    -1,
            porIntencion: new Dictionary<Intent, int>
            {
                [Intent.SolicitudRespetuosa] =  1,
                [Intent.Empatia]             =  1,
                [Intent.AportaInformacion]   =  2,
                [Intent.SolicitudAgresiva]   = -2,
                [Intent.Interrupcion]        = -2,
                [Intent.PreguntaFueraDeTema] = -1,
            },
            porTono: new Dictionary<Tone, int>
            {
                [Tone.Respetuoso] =  1,
                [Tone.Empatico]   =  1,
                [Tone.Agresivo]   = -2,
                [Tone.Ansioso]    = -1,
            },
            porAccion: new Dictionary<PhysicalAction, int>
            {
                [PhysicalAction.ContactoVisual] =  1,
                [PhysicalAction.GestoCalma]     =  1,
                [PhysicalAction.Acercarse]      = -1,
                [PhysicalAction.Alejarse]       = -1,
            });

        /// <summary>Extremo cooperativo: linea base contra la cual se miden las otras tres.</summary>
        private static ReceptivityProfile PerfilEmpatico() => new ReceptivityProfile(
            umbralReceptivo:    2,
            umbralNoReceptivo: -3,
            limitePuntaje:      4,
            puntajeInicial:     1,
            porIntencion: new Dictionary<Intent, int>
            {
                [Intent.SolicitudRespetuosa] =  2,
                [Intent.Empatia]             =  2,
                [Intent.AportaInformacion]   =  2,
                [Intent.SolicitudAgresiva]   = -1,
                [Intent.Interrupcion]        = -1,
            },
            porTono: new Dictionary<Tone, int>
            {
                [Tone.Respetuoso] =  1,
                [Tone.Empatico]   =  2,
                [Tone.Agresivo]   = -1,
            },
            porAccion: new Dictionary<PhysicalAction, int>
            {
                [PhysicalAction.ContactoVisual] =  1,
                [PhysicalAction.GestoCalma]     =  1,
                [PhysicalAction.Acercarse]      =  1,
                [PhysicalAction.EntregarObjeto] =  1,
                [PhysicalAction.Alejarse]       = -1,
            });
    }
}
