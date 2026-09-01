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
