using UnityEngine;

namespace NpcAi.Core.Channels
{
    [CreateAssetMenu(fileName = "UtteranceChannel", menuName = "NPC AI/Canales/Utterance")]
    public sealed class UtteranceChannel : EventChannel<Utterance> { }

    [CreateAssetMenu(fileName = "IntentResultChannel", menuName = "NPC AI/Canales/IntentResult")]
    public sealed class IntentResultChannel : EventChannel<IntentResult> { }

    [CreateAssetMenu(fileName = "PhysicalActionChannel", menuName = "NPC AI/Canales/PhysicalAction")]
    public sealed class PhysicalActionChannel : EventChannel<PhysicalAction> { }

    [CreateAssetMenu(fileName = "ReceptivityChangeChannel", menuName = "NPC AI/Canales/ReceptivityChange")]
    public sealed class ReceptivityChangeChannel : EventChannel<ReceptivityChange> { }

    [CreateAssetMenu(fileName = "NpcReplyChannel", menuName = "NPC AI/Canales/NpcReply")]
    public sealed class NpcReplyChannel : EventChannel<NpcReply> { }
}
