using System.Collections.Generic;
using NpcAi.Core;

namespace NpcAi.Harness.Tests
{
    /// <summary>
    /// Los cinco espias locales de esta suite (design.md, AD11): graban exactamente lo que
    /// la tabla "Testing Strategy" exige observar y que ningun doble compartido
    /// (<c>Scripted*</c>/<c>Recording*</c>) puede exponer hoy. Mismo precedente que
    /// <c>ClasificadorNoListo</c> en <c>IntentClassifierContract</c> (decision D3): viven
    /// DENTRO de <c>Tests/EditMode/Harness/</c>, no en <c>Runtime/&lt;Modulo&gt;/Fakes/</c>.
    /// No duplican comportamiento de ningun modulo real: solo graban.
    /// </summary>
    internal sealed class EspiaClasificador : IIntentClassifier
    {
        public int Invocaciones { get; private set; }

        public string UltimoTexto { get; private set; }

        /// <summary>Resultado fijo que <see cref="Classify"/> devuelve; programable por la prueba.</summary>
        public IntentResult Resultado { get; set; } = IntentResult.Unknown();

        public bool IsReady => true;

        public IntentResult Classify(string text)
        {
            Invocaciones++;
            UltimoTexto = text;
            return Resultado;
        }
    }

    internal sealed class EspiaReceptividad : IReceptivityEngine
    {
        /// <summary>Un registro por cada <see cref="Evaluate"/>, en orden de llamada.</summary>
        public List<(IntentResult Intent, PhysicalAction Action)> Evaluaciones { get; } =
            new List<(IntentResult, PhysicalAction)>();

        /// <summary>Una entrada por cada <see cref="Reset"/>, en orden de llamada.</summary>
        public List<PersonalityId> Resets { get; } = new List<PersonalityId>();

        /// <summary>
        /// Programable por la prueba. <see cref="Reset"/> lo devuelve a
        /// <see cref="Receptivity.Neutral"/>, asi que una prueba que necesite otro valor debe
        /// asignarlo DESPUES de iniciar la sesion.
        /// </summary>
        public Receptivity Current { get; set; } = Receptivity.Neutral;

        /// <summary>
        /// Cuando no es <c>null</c>, <see cref="Evaluate"/> devuelve exactamente este cambio en
        /// lugar del que calcula el espia; permite programar uno distinguible de
        /// <c>default(ReceptivityChange)</c> y verificar que M11 lo reenvia sin alterarlo.
        /// </summary>
        public ReceptivityChange? CambioProgramado { get; set; }

        public void Reset(PersonalityId personality)
        {
            Resets.Add(personality);
            Current = Receptivity.Neutral;
        }

        /// <summary>
        /// Mueve <see cref="Current"/> a <see cref="Receptivity.Receptivo"/> ante cualquier
        /// accion distinta de <see cref="PhysicalAction.Ninguna"/>, para que el escenario
        /// "la accion mueve receptividad" sea satisfacible (AD11: <c>ScriptedReceptivityEngine</c>
        /// no sirve aqui porque su delta para <see cref="PhysicalAction.Acercarse"/> es cero).
        /// </summary>
        public ReceptivityChange Evaluate(IntentResult intent, PhysicalAction action)
        {
            Evaluaciones.Add((intent, action));

            var from = Current;
            if (action != PhysicalAction.Ninguna)
                Current = Receptivity.Receptivo;

            return CambioProgramado ?? new ReceptivityChange(from, Current, 0, "ESPIA");
        }
    }

    internal sealed class EspiaDialogo : IDialogueGenerator
    {
        public int Invocaciones { get; private set; }

        /// <summary>Un registro por cada <see cref="Generate"/>, en orden de llamada.</summary>
        public List<(PersonalityId Personality, Receptivity Receptivity, IntentResult Intent)> Llamadas { get; } =
            new List<(PersonalityId, Receptivity, IntentResult)>();

        /// <summary>Respuesta marcada e identificable; programable por la prueba.</summary>
        public NpcReply Respuesta { get; set; } =
            new NpcReply("[espia-dialogo] respuesta social", "neutral", "idle");

        public NpcReply Generate(PersonalityId personality, Receptivity receptivity, IntentResult intent)
        {
            Invocaciones++;
            Llamadas.Add((personality, receptivity, intent));
            return Respuesta;
        }
    }

    internal sealed class EspiaObjetivo : IScenarioObjective
    {
        /// <summary>Un registro por cada <see cref="Notify"/>, en orden de llamada.</summary>
        public List<ReceptivityChange> Notificaciones { get; } = new List<ReceptivityChange>();

        public ClinicalCaseId UltimoCaso { get; private set; } = ClinicalCaseId.None;

        public int CasosAsignados { get; private set; }

        public int BanderasRegistradas { get; private set; }

        /// <summary>Programable por la prueba; no se recalcula a partir de <see cref="Notify"/>.</summary>
        public float Progress01 { get; set; }

        public bool IsComplete => false;

        public void Notify(ReceptivityChange change) => Notificaciones.Add(change);

        /// <summary>
        /// Superficie aditiva de M9 (AD1): se cablea como el delegado
        /// <c>asignarCasoAlObjetivo</c> del constructor de <see cref="SessionDirector"/>.
        /// </summary>
        public void AssignCase(ClinicalCaseId caseId)
        {
            UltimoCaso = caseId;
            CasosAsignados++;
        }

        /// <summary>
        /// Superficie aditiva de M9 que la spec exige que <see cref="SessionDirector"/> nunca
        /// invoque en esta entrega (Requirement "Fronteras que SessionDirector no cruza").
        /// <c>ScriptedScenarioObjective</c> no sirve para esta aserion: es <c>sealed</c> y no
        /// declara <c>RegisterRedFlag</c>.
        /// </summary>
        public void RegisterRedFlag(int index) => BanderasRegistradas++;
    }

    internal sealed class EspiaClinico : IClinicalResponder
    {
        public ClinicalCaseId UltimoCaso { get; private set; } = ClinicalCaseId.None;

        public PersonalityId UltimaPersonalidad { get; private set; } = PersonalityId.None;

        public int Asignaciones { get; private set; }

        public int Invocaciones { get; private set; }

        /// <summary>El <see cref="IntentResult"/> de la ultima llamada a <see cref="Respond"/>.</summary>
        public IntentResult UltimoIntent { get; private set; }

        public bool IsReady => true;

        // "Core." es obligatorio aqui: el namespace del modulo NpcAi.ClinicalResponse choca
        // con el nombre del DTO NpcAi.Core.ClinicalResponse (mismo problema y misma solucion
        // que ScriptedClinicalResponder y que Core.Receptivity en NpcAi.Receptivity).
        /// <summary>Respuesta programable; por defecto "no aplica" (turno social).</summary>
        public Core.ClinicalResponse Respuesta { get; set; } = Core.ClinicalResponse.NoAplica;

        public void AssignCase(ClinicalCaseId caseId, PersonalityId personality)
        {
            UltimoCaso = caseId;
            UltimaPersonalidad = personality;
            Asignaciones++;
        }

        public Core.ClinicalResponse Respond(Utterance nurseUtterance, IntentResult intent)
        {
            Invocaciones++;
            UltimoIntent = intent;
            return Respuesta;
        }
    }
}
