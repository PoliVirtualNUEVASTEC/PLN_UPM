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
        /// <summary>False mientras el modelo no este cargado. Nadie debe llamar a Classify antes.</summary>
        bool IsReady { get; }

        /// <summary>
        /// Nunca lanza. Ante texto vacio, nulo o no entendido devuelve
        /// <see cref="IntentResult.Unknown"/>.
        /// </summary>
        IntentResult Classify(string text);
    }

    // ------------------------------------------------------------------ Decision

    /// <summary>M4 — maquina de estados Receptivo / Neutral / No receptivo.</summary>
    public interface IReceptivityEngine
    {
        Receptivity Current { get; }

        /// <summary>Reinicia al estado inicial que define el perfil de la personalidad.</summary>
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
        /// <summary>Nunca devuelve texto vacio.</summary>
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
        /// <summary>Progreso normalizado, siempre en [0, 1].</summary>
        float Progress01 { get; }

        /// <summary>True solo si <see cref="Progress01"/> llego a 1.</summary>
        bool IsComplete { get; }

        void Notify(ReceptivityChange change);
    }
}
