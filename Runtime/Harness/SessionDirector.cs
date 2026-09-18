using System;
using System.Collections.Generic;
using System.Linq;
using NpcAi.Core;

namespace NpcAi.Harness
{
    /// <summary>
    /// M11 — raiz de composicion del escenario de emergencia. Elige, secuencia, enruta y
    /// delega entre M2, M4, M6, M9 y M15. C# puro: no es <c>MonoBehaviour</c>, no toca
    /// canales, no lee disco, no imprime. Cero <c>UnityEngine</c> (<c>noEngineReferences:
    /// true</c> en <c>NpcAi.Harness.asmdef</c>).
    /// </summary>
    public sealed class SessionDirector
    {
        private readonly IIntentClassifier _m2;
        private readonly IReceptivityEngine _m4;
        private readonly IDialogueGenerator _m6;
        private readonly IScenarioObjective _m9;
        private readonly IClinicalResponder _m15;

        /// <summary>
        /// Puente hacia la superficie aditiva de M9 (design.md, AD1): <see cref="IScenarioObjective"/>
        /// no declara <c>AssignCase</c>, y este ensamblado solo referencia <c>NpcAi.Core</c>.
        /// Lo cablea el compositor real (<c>HarnessBehaviour</c>, PR2) o la prueba.
        /// </summary>
        private readonly Action<ClinicalCaseId> _asignarCaso;

        private readonly Action<string> _declararTriaje;

        private readonly ClinicalCaseId[] _casos;
        private readonly PersonalityId[] _personalidades;
        private readonly Random _rng;

        private ClinicalCaseId _ultimoCaso = ClinicalCaseId.None;

        /// <summary><see cref="ClinicalCaseId.None"/> antes del primer <see cref="IniciarSesion()"/>.</summary>
        public ClinicalCaseId CasoActual { get; private set; } = ClinicalCaseId.None;

        /// <summary><see cref="PersonalityId.None"/> antes del primer <see cref="IniciarSesion()"/>.</summary>
        public PersonalityId PersonalidadActual { get; private set; } = PersonalityId.None;

        /// <summary><c>_m9.Progress01</c>, derivado en lectura; nunca cacheado.</summary>
        public float Progreso => _m9.Progress01;

        /// <param name="asignarCasoAlObjetivo">
        /// Puente hacia la superficie aditiva de M9 (AD1): <see cref="IScenarioObjective"/> NO
        /// declara <c>AssignCase</c>, y este ensamblado solo referencia <c>NpcAi.Core</c>.
        /// </param>
        /// <param name="semilla">Reproducibilidad: misma semilla + mismo catalogo, mismo par.</param>
        /// <exception cref="ArgumentNullException">
        /// Cualquiera de los 5 puertos o los 2 delegados es <c>null</c>.
        /// </exception>
        /// <exception cref="ArgumentException"><paramref name="casos"/> o <paramref name="personalidades"/> nulos o vacios.</exception>
        public SessionDirector(
            IIntentClassifier m2,
            IReceptivityEngine m4,
            IDialogueGenerator m6,
            IScenarioObjective m9,
            IClinicalResponder m15,
            Action<ClinicalCaseId> asignarCasoAlObjetivo,
            Action<string> declararTriajeEnObjetivo,
            IReadOnlyList<ClinicalCaseId> casos,
            IReadOnlyList<PersonalityId> personalidades,
            int semilla)
        {
            _m2 = m2 ?? throw new ArgumentNullException(nameof(m2));
            _m4 = m4 ?? throw new ArgumentNullException(nameof(m4));
            _m6 = m6 ?? throw new ArgumentNullException(nameof(m6));
            _m9 = m9 ?? throw new ArgumentNullException(nameof(m9));
            _m15 = m15 ?? throw new ArgumentNullException(nameof(m15));
            _asignarCaso = asignarCasoAlObjetivo ?? throw new ArgumentNullException(nameof(asignarCasoAlObjetivo));
            _declararTriaje = declararTriajeEnObjetivo ?? throw new ArgumentNullException(nameof(declararTriajeEnObjetivo));

            if (casos == null || casos.Count == 0)
                throw new ArgumentException("El catalogo de casos clinicos no puede ser nulo ni vacio.", nameof(casos));

            if (personalidades == null || personalidades.Count == 0)
                throw new ArgumentException("El catalogo de personalidades no puede ser nulo ni vacio.", nameof(personalidades));

            // Copia propia (AD3): si el llamador muta su lista despues de construir, la
            // reproducibilidad que exige la spec sigue valiendo.
            _casos = casos.ToArray();
            _personalidades = personalidades.ToArray();
            _rng = new Random(semilla);
        }

        /// <summary>AD5: elige <c>(caso, personalidad)</c> con el RNG propio y arranca.</summary>
        public void IniciarSesion()
        {
            var caso = SiguienteCaso();
            var personalidad = _personalidades[_rng.Next(_personalidades.Length)];
            Arrancar(caso, personalidad);
        }

        /// <summary>AD5: explicito, no toca el RNG en absoluto.</summary>
        public void IniciarSesion(ClinicalCaseId caso, PersonalityId personalidad)
        {
            Arrancar(caso, personalidad);
        }

        /// <summary>
        /// AD7: lista filtrada, nunca re-tirada. Consume exactamente una tirada de <see cref="_rng"/>.
        /// </summary>
        private ClinicalCaseId SiguienteCaso()
        {
            if (_ultimoCaso.IsNone)
                return _casos[_rng.Next(_casos.Length)]; // primera sesion: catalogo completo

            var candidatos = _casos.Where(c => c != _ultimoCaso).ToArray();
            if (candidatos.Length == 0)
                candidatos = _casos; // catalogo de 1 caso, o todo duplicado (AD7)

            return candidatos[_rng.Next(candidatos.Length)];
        }

        /// <summary>Unico sitio con los 3 efectos del arranque (AD8: actualiza <see cref="_ultimoCaso"/> en ambos caminos).</summary>
        private void Arrancar(ClinicalCaseId caso, PersonalityId personalidad)
        {
            CasoActual = caso;
            PersonalidadActual = personalidad;
            _ultimoCaso = caso;

            _m4.Reset(personalidad);         // M4 — estado inicial del perfil
            _asignarCaso(caso);               // M9 — AssignCase(caso), via delegado (AD1)
            _m15.AssignCase(caso, personalidad); // M15 — mismo ClinicalCaseId que M9
        }

        /// <summary>
        /// Requirement "Centinelas para senales parciales" + "Enrutado clinico/social".
        /// Nunca lanza (AD3): M15/M6 garantizan <c>Text</c> no vacio por contrato de M0.
        /// </summary>
        public NpcReply ProcesarTurno(Utterance utterance)
        {
            var intent = _m2.Classify(utterance.Text); // AD9: sin mirar IsReady
            var cambio = _m4.Evaluate(intent, PhysicalAction.Ninguna); // centinela de la otra dimension
            _m9.Notify(cambio); // AD10: un solo sitio, antes de la rama

            var clin = _m15.Respond(utterance, intent);
            return clin.Handled
                ? clin.Reply // tal cual, sin alterar
                : _m6.Generate(PersonalidadActual, _m4.Current, intent);
        }

        /// <summary>
        /// Requirement "Centinelas para senales parciales" + "Silencio de la accion fisica
        /// sola". Mueve la receptividad pero nunca produce habla: hablar responde a lo
        /// dicho, no a un gesto. <c>Classify</c> NO se llama.
        /// </summary>
        public NpcReply? ProcesarAccion(PhysicalAction accion)
        {
            var cambio = _m4.Evaluate(IntentResult.Unknown(), accion); // centinela; Classify NO se llama
            _m9.Notify(cambio);
            return null;
        }

        /// <summary>
        /// Pass-through crudo hacia la superficie aditiva de M9 (AD1): normalizar la
        /// categoria es responsabilidad de M9 (su propia decision de diseno), no de M11.
        /// </summary>
        public void DeclararTriaje(string categoria)
        {
            _declararTriaje(categoria);
        }
    }
}
