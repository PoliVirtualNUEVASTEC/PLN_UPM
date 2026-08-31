using NpcAi.Core;

namespace NpcAi.Dialogue.Fakes
{
    /// <summary>
    /// Doble determinista de M6. Plantillas fijas por estado de receptividad.
    /// El generador real agrega personalidad y variacion (cadenas de Markov);
    /// este solo garantiza que nunca devuelve texto vacio.
    /// </summary>
    public sealed class ScriptedDialogueGenerator : IDialogueGenerator
    {
        public NpcReply Generate(PersonalityId personality, Core.Receptivity receptivity, IntentResult intent)
        {
            var quien = personality.IsNone ? "el NPC" : personality.Value;

            return receptivity switch
            {
                Core.Receptivity.Receptivo =>
                    new NpcReply($"[{quien}] Claro, digame en que le ayudo.", "receptivo", "asentir"),

                Core.Receptivity.NoReceptivo =>
                    new NpcReply($"[{quien}] No tengo nada mas que hablar con usted.", "molesto", "cruzar_brazos"),

                _ =>
                    new NpcReply($"[{quien}] Lo escucho.", "neutral", "idle"),
            };
        }
    }
}
