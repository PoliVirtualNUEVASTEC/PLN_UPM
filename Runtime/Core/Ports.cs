using System;

namespace NpcAi.Core
{
    // ------------------------------------------------------------------ Entrada

    /// <summary>M1 — convierte voz en texto. Sin esto, el pipeline arranca en texto.</summary>
    public interface ISpeechToText
    {
        event Action<Utterance> OnUtterance;
        bool IsListening { get; }
        void StartListening();
        void StopListening();
    }

    /// <summary>M7 — acciones fisicas del usuario con los controles VR.</summary>
    public interface IPhysicalActionSource
    {
        event Action<PhysicalAction> OnAction;
    }

    // ------------------------------------------------------------------ Comprension

    /// <summary>M2 — de texto libre a intencion y tono.</summary>
    public interface IIntentClassifier
    {
        /// <summary>
        /// False mientras el modelo no este cargado. Leer esta propiedad NO DEBE lanzar
        /// en ningun estado.
        /// </summary>
        bool IsReady { get; }

        /// <summary>
        /// NO DEBE lanzar en ningun estado: ni con <see cref="IsReady"/> en <c>false</c>,
        /// ni ante entradas atipicas (<c>null</c>, vacio, solo espacios, simbolos, cadenas
        /// muy largas, numeros). Ante texto <c>null</c>, <c>""</c> o solo espacios devuelve
        /// <see cref="IntentResult.Unknown"/> con <c>Confidence == 0</c>. Para una misma
        /// entrada, <c>Intent</c> y <c>Tone</c> DEBEN ser deterministas; <c>Confidence</c> y
        /// <c>LatencyMs</c> NO estan obligados a serlo. Cuando <see cref="IsReady"/> es
        /// <c>false</c>, <c>Classify</c> DEBERIA (recomendacion, no regla dura) devolver
        /// <see cref="IntentResult.Unknown"/>.
        /// </summary>
        IntentResult Classify(string text);
    }

    /// <summary>M2 — de texto libre a un vector de embedding de oracion.</summary>
    public interface ISentenceEmbedder
    {
        /// <summary>
        /// False mientras el modelo/encoder no este cargado. Leer esta propiedad NO DEBE
        /// lanzar en ningun estado.
        /// </summary>
        bool IsReady { get; }

        /// <summary>
        /// NO DEBE lanzar en ningun estado: ni con <see cref="IsReady"/> en <c>false</c>,
        /// ni ante entradas atipicas (<c>null</c>, vacio, solo espacios, simbolos, cadenas
        /// muy largas, numeros). Con <see cref="IsReady"/> en <c>false</c> o texto
        /// <c>null</c> / vacio / solo espacios DEBE devolver
        /// <see cref="SentenceEmbedding.Empty"/>. Con <see cref="IsReady"/> en <c>true</c> y
        /// texto con contenido DEBE devolver un vector no vacio de componentes finitos (sin
        /// <c>NaN</c> ni <c>+-Inf</c>). Todo vector no vacio de una misma instancia DEBE
        /// tener la misma <see cref="SentenceEmbedding.Length"/>; esa longitud NO es una
        /// constante del contrato, depende del encoder cargado. DEBE ser determinista bit a
        /// bit para el mismo texto en la misma instancia (asimetria deliberada frente a
        /// <see cref="IIntentClassifier.Classify"/>, que solo exige determinismo en
        /// <c>Intent</c>/<c>Tone</c>): el vector completo es la senal primaria de
        /// emparejamiento de consumidores deterministas como
        /// <see cref="IClinicalResponder.Respond"/>.
        /// </summary>
        SentenceEmbedding Embed(string text);
    }

    // ------------------------------------------------------------------ Respuesta clinica

    /// <summary>
    /// M15 — responde como el paciente del caso clinico asignado, anclando la respuesta a un
    /// hecho del caso ("hace 2 meses", "alergica al Tramadol"). Paso "clinico" del patron
    /// enrutador: si el turno no es clinico devuelve <see cref="ClinicalResponse.NoAplica"/>
    /// y el llamador enruta a <see cref="IDialogueGenerator"/> (M6).
    /// </summary>
    public interface IClinicalResponder
    {
        /// <summary>
        /// <c>false</c> hasta que <see cref="AssignCase"/> vincule un
        /// <see cref="ClinicalCaseId"/> existente. Leer NO DEBE lanzar en ningun estado.
        /// </summary>
        bool IsReady { get; }

        /// <summary>
        /// Vincula el caso y la personalidad (estado de sesion, no de turno; mismo patron que
        /// <see cref="IReceptivityEngine.Reset"/>). Con el mismo par DEBE ser determinista e
        /// idempotente. Con un <see cref="ClinicalCaseId"/> desconocido NO DEBE lanzar y DEBE
        /// dejar <see cref="IsReady"/> en <c>false</c>.
        /// </summary>
        void AssignCase(ClinicalCaseId caseId, PersonalityId personality);

        /// <summary>
        /// NO DEBE lanzar en ningun estado (sin <see cref="AssignCase"/> previo, con
        /// <paramref name="nurseUtterance"/> vacio o <c>default</c>, con
        /// <paramref name="intent"/> <c>default</c>, con solo simbolos o cadenas muy largas).
        /// Con <see cref="IsReady"/> en <c>false</c> DEBE devolver
        /// <see cref="ClinicalResponse.NoAplica"/>. Cuando <c>Handled == true</c>,
        /// <c>Reply.Text</c> NO DEBE ser vacio ni solo espacios y <c>Reply.EmotionTag</c> /
        /// <c>Reply.AnimationCue</c> NO DEBEN ser <c>null</c>. DEBE ser determinista en
        /// <c>Handled</c> y en <c>Reply.Text</c> para la misma tupla
        /// <c>(ClinicalCaseId, PersonalityId, Utterance, IntentResult)</c> (tags y latencia
        /// NO obligados). Asimetria deliberada frente a
        /// <see cref="IDialogueGenerator.Generate"/>, que NO es determinista.
        /// </summary>
        ClinicalResponse Respond(Utterance nurseUtterance, IntentResult intent);
    }

    // ------------------------------------------------------------------ Requerimientos de sala de juntas

    /// <summary>
    /// M16 — responde como el NPC-cliente de sala de juntas sobre un requerimiento de negocio
    /// del caso asignado, gateada por <see cref="Receptivity"/>. Paso "requerimientos" del
    /// patron enrutador (mismo patron que <see cref="IClinicalResponder"/>): si el turno no es
    /// de requerimientos devuelve <see cref="RequirementResponse.NoAplica"/> y el llamador
    /// enruta a <see cref="IDialogueGenerator"/> (M6).
    /// </summary>
    public interface IRequirementResponder
    {
        /// <summary>
        /// <c>false</c> hasta que <see cref="AssignCase"/> vincule un
        /// <see cref="RequirementCaseId"/> existente. Leer NO DEBE lanzar en ningun estado.
        /// </summary>
        bool IsReady { get; }

        /// <summary>
        /// Vincula el caso y la personalidad (estado de sesion, no de turno; mismo patron que
        /// <see cref="IClinicalResponder.AssignCase"/>). Con el mismo par DEBE ser determinista
        /// e idempotente. Con un <see cref="RequirementCaseId"/> desconocido NO DEBE lanzar y
        /// DEBE dejar <see cref="IsReady"/> en <c>false</c>.
        /// </summary>
        void AssignCase(RequirementCaseId caseId, PersonalityId personality);

        /// <summary>
        /// NO DEBE lanzar en ningun estado (sin <see cref="AssignCase"/> previo, con
        /// <paramref name="studentUtterance"/> vacio o <c>default</c>, con
        /// <paramref name="intent"/> <c>default</c>, con cualquier valor de
        /// <paramref name="receptivity"/>, con solo simbolos o cadenas muy largas). Con
        /// <see cref="IsReady"/> en <c>false</c> DEBE devolver
        /// <see cref="RequirementResponse.NoAplica"/>. Invariante transversal:
        /// <c>Outcome != RequirementOutcome.NoAplica</c> exige <c>RequirementId.IsNone == false</c>;
        /// con <see cref="RequirementOutcome.NoAplica"/>, <c>RequirementId</c> DEBE ser
        /// <see cref="Core.RequirementId.None"/> y <c>Reply</c> NO tiene garantias. Con
        /// <see cref="RequirementOutcome.AunNoRevelado"/> y <see cref="RequirementOutcome.Revelado"/>,
        /// <c>Reply.Text</c> NO DEBE ser vacio ni solo espacios y los tags NO DEBEN ser
        /// <c>null</c>. El contenido del requerimiento solo DEBE aparecer en <c>Reply.Text</c>
        /// cuando <c>Outcome == RequirementOutcome.Revelado</c>. DEBE ser determinista en
        /// <c>Outcome</c>, <c>Reply.Text</c> y <c>RequirementId</c> para la misma tupla
        /// <c>(RequirementCaseId, PersonalityId, Utterance, IntentResult, Receptivity)</c>.
        /// </summary>
        RequirementResponse Respond(Utterance studentUtterance, IntentResult intent, Receptivity receptivity);
    }

    // ------------------------------------------------------------------ Decision

    /// <summary>M4 — maquina de estados Receptivo / Neutral / No receptivo.</summary>
    public interface IReceptivityEngine
    {
        /// <summary>
        /// Estado actual. DEBE ser <see cref="Receptivity.Neutral"/> hasta que se llame
        /// <see cref="Reset"/> por primera vez.
        /// </summary>
        Receptivity Current { get; }

        /// <summary>
        /// Reinicia al estado inicial que define el perfil de la personalidad. Con la misma
        /// <see cref="PersonalityId"/> DEBE ser determinista e idempotente: llamarlo varias
        /// veces seguidas deja el mismo <see cref="Current"/>.
        /// </summary>
        void Reset(PersonalityId personality);

        /// <summary>
        /// Evalua una intencion y una accion fisica y devuelve la transicion resultante.
        /// <c>result.From</c> es el estado previo y <c>result.To</c> queda igual a <see cref="Current"/>.
        /// </summary>
        ReceptivityChange Evaluate(IntentResult intent, PhysicalAction action);
    }

    /// <summary>M6 — de (personalidad, estado, intencion) a una respuesta redactada.</summary>
    public interface IDialogueGenerator
    {
        /// <summary>
        /// Nunca devuelve <c>Text</c> vacio para ningun valor de <see cref="Receptivity"/>;
        /// <c>EmotionTag</c> y <c>AnimationCue</c> nunca son <c>null</c>. Funciona con
        /// <see cref="PersonalityId.None"/> sin lanzar. <c>Receptivo</c> y <c>NoReceptivo</c>
        /// producen <c>Text</c> distinto. NO se exige determinismo: dos llamadas con la misma
        /// entrada PUEDEN devolver texto distinto (asimetria deliberada frente a
        /// <see cref="IIntentClassifier.Classify"/>, que si es determinista en Intent/Tone).
        /// </summary>
        NpcReply Generate(PersonalityId personality, Receptivity receptivity, IntentResult intent);
    }

    // ------------------------------------------------------------------ Salida

    /// <summary>M8 — voz sintetizada y animacion del NPC.</summary>
    public interface INpcPresenter
    {
        void Play(NpcReply reply);
    }

    // ------------------------------------------------------------------ Escenario

    /// <summary>M9 / M10 — progreso del objetivo del escenario (triaje, requerimientos).</summary>
    public interface IScenarioObjective
    {
        /// <summary>
        /// Progreso normalizado, siempre en <c>[0, 1]</c> tras cualquier secuencia de
        /// <see cref="Notify"/>; NO DEBE bajar de <c>0</c>.
        /// </summary>
        float Progress01 { get; }

        /// <summary>
        /// DEBE ser <c>true</c> si y solo si <see cref="Progress01"/> es <c>1</c>
        /// (tolerancia <c>1e-4</c>). La completitud es REVERSIBLE y NO pegajosa: si un
        /// <see cref="Notify"/> posterior baja el progreso, <c>IsComplete</c> PUEDE volver
        /// a <c>false</c>.
        /// </summary>
        bool IsComplete { get; }

        /// <summary><see cref="Notify"/> con <c>default(ReceptivityChange)</c> DEBE ser no-op.</summary>
        void Notify(ReceptivityChange change);
    }
}
