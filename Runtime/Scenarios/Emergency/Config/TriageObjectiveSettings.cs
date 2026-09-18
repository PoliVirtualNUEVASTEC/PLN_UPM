namespace NpcAi.Scenarios.Emergency
{
    /// <summary>
    /// Pesos y umbrales de <see cref="TriageScenarioObjective"/>, inyectables. <c>Config/</c>
    /// es una carpeta, no un sub-namespace (mismo patron que <c>Runtime/VrInput/Config/</c>).
    /// Dos fracciones complementarias (AD4): <see cref="PesoClinico"/> y
    /// <see cref="PesoDeTriaje"/> son exactamente <c>1f - PesoDeReceptividad</c> /
    /// <c>1f - PesoDeBanderasRojas</c>, asi que el techo de la mezcla es siempre exactamente
    /// <c>1.0f</c> (ver "Aritmetica" en <c>design.md</c>) y no existe el caso patologico de
    /// pesos sueltos que sumen 0.
    /// </summary>
    public sealed class TriageObjectiveSettings
    {
        public const float PesoDeReceptividadPorDefecto = 0.3f;
        public const float PesoDeBanderasRojasPorDefecto = 0.7f;
        public const int RachaParaReceptividadPlenaPorDefecto = 5;

        /// <summary>[0,1]. El complemento es <see cref="PesoClinico"/>.</summary>
        public float PesoDeReceptividad { get; }

        /// <summary>[0,1] dentro de la mitad clinica. El complemento es <see cref="PesoDeTriaje"/>.</summary>
        public float PesoDeBanderasRojas { get; }

        /// <summary>Mejoras consecutivas necesarias para el credito receptivo pleno. Minimo 1.</summary>
        public int RachaParaReceptividadPlena { get; }

        /// <summary>AD4: complemento exacto de <see cref="PesoDeReceptividad"/>.</summary>
        public float PesoClinico => 1f - PesoDeReceptividad;

        /// <summary>AD4: complemento exacto de <see cref="PesoDeBanderasRojas"/>.</summary>
        public float PesoDeTriaje => 1f - PesoDeBanderasRojas;

        public TriageObjectiveSettings(
            float pesoDeReceptividad = PesoDeReceptividadPorDefecto,
            float pesoDeBanderasRojas = PesoDeBanderasRojasPorDefecto,
            int rachaParaReceptividadPlena = RachaParaReceptividadPlenaPorDefecto)
        {
            PesoDeReceptividad = Clamp01(pesoDeReceptividad);
            PesoDeBanderasRojas = Clamp01(pesoDeBanderasRojas);
            // Math.Max(1, ...) elimina la division por cero en Progress01.
            RachaParaReceptividadPlena = System.Math.Max(1, rachaParaReceptividadPlena);
        }

        private static float Clamp01(float x) => x < 0f ? 0f : (x > 1f ? 1f : x);
    }
}
