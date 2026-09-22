using UnityEngine;

namespace NpcAi.Scenarios.Boardroom.Unity
{
    /// <summary>
    /// Pesos del objetivo de sala de juntas, editables en el Inspector. Es SOLO dato:
    /// <see cref="ToSettings"/> proyecta al POCO puro y no toma una sola decision
    /// (espejo exacto de <c>ReceptivityProfileAsset.ToProfile</c>, M4). Aqui vive todo el
    /// contacto de la configuracion de M10 con <c>UnityEngine</c>; el objetivo, el checklist y
    /// los settings siguen siendo C# puro. Sin <c>OnValidate</c>: el clamp real vive en el
    /// constructor de <see cref="BoardroomObjectiveSettings"/> (AD13) — <c>[Range]</c>/<c>[Min]</c>
    /// solo acotan la experiencia del Inspector.
    /// </summary>
    [CreateAssetMenu(
        fileName = "BoardroomObjective",
        menuName = "NpcAi/Escenarios/Objetivo de sala de juntas")]
    public sealed class BoardroomObjectiveSettingsAsset : ScriptableObject
    {
        [Tooltip("Peso de la via de trato. El complemento es el levantamiento.")]
        [Range(0f, 1f)]
        public float pesoDeTrato = BoardroomObjectiveSettings.PesoDeTratoPorDefecto;

        [Tooltip("Peso de la cobertura DENTRO del levantamiento. El complemento es el cierre.")]
        [Range(0f, 1f)]
        public float pesoDeCobertura = BoardroomObjectiveSettings.PesoDeCoberturaPorDefecto;

        [Tooltip("Empeoramientos seguidos que vacian el credito de trato.")]
        [Min(1)]
        public int pasosDeTrato = BoardroomObjectiveSettings.PasosDeTratoPorDefecto;

        /// <summary>Proyeccion al tipo puro. Sin logica: copia.</summary>
        public BoardroomObjectiveSettings ToSettings() =>
            new BoardroomObjectiveSettings(pesoDeTrato, pesoDeCobertura, pasosDeTrato);
    }
}
