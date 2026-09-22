namespace NpcAi.Scenarios.Boardroom
{
    /// <summary>
    /// Pesos y umbrales de <see cref="RequirementsScenarioObjective"/>, inyectables.
    /// <c>Config/</c> es una carpeta, no un sub-namespace (mismo patron que M9,
    /// <c>Runtime/Scenarios/Emergency/Config/TriageObjectiveSettings.cs</c>).
    /// Dos pares de fracciones complementarias (AD3): el techo de la mezcla es siempre
    /// exactamente 1.0f y no existe el caso patologico de pesos sueltos que sumen 0.
    /// Las constantes son la red de seguridad para cuando no hay asset (AD13); el valor
    /// de tuning vive en <c>Data/Scenarios/Boardroom.asset</c> (regla dura 7).
    /// </summary>
    public sealed class BoardroomObjectiveSettings
    {
        /// <summary>Decision de negocio de Jefferson (2026-09-21, pregunta 3): peso bajo.</summary>
        public const float PesoDeTratoPorDefecto = 0.2f;

        /// <summary>Dentro de la mitad de levantamiento. Cobertura manda 3-a-1 sobre cierre.</summary>
        public const float PesoDeCoberturaPorDefecto = 0.75f;

        /// <summary>Empeoramientos que hace falta acumular para vaciar el credito de trato.</summary>
        public const int PasosDeTratoPorDefecto = 5;

        /// <summary>[0,1]. El complemento es <see cref="PesoDeLevantamiento"/>.</summary>
        public float PesoDeTrato { get; }

        /// <summary>[0,1] dentro del levantamiento. El complemento es <see cref="PesoDeCierre"/>.</summary>
        public float PesoDeCobertura { get; }

        /// <summary>Pasos del libro mayor de trato. Minimo 1 (elimina la division por cero).</summary>
        public int PasosDeTrato { get; }

        /// <summary>AD3: complemento exacto de <see cref="PesoDeTrato"/>.</summary>
        public float PesoDeLevantamiento => 1f - PesoDeTrato;

        /// <summary>AD3: complemento exacto de <see cref="PesoDeCobertura"/>.</summary>
        public float PesoDeCierre => 1f - PesoDeCobertura;

        public BoardroomObjectiveSettings(
            float pesoDeTrato = PesoDeTratoPorDefecto,
            float pesoDeCobertura = PesoDeCoberturaPorDefecto,
            int pasosDeTrato = PasosDeTratoPorDefecto)
        {
            PesoDeTrato = Clamp01(pesoDeTrato);
            PesoDeCobertura = Clamp01(pesoDeCobertura);
            // Math.Max(1, ...) elimina la division por cero en la via de trato.
            PasosDeTrato = System.Math.Max(1, pasosDeTrato);
        }

        /// <summary>
        /// AD7: UNICO lugar donde viven las tres vias mezcladas. La llaman la implementacion
        /// real y el doble, asi la paridad no depende de copiar aritmetica a mano.
        /// Sin caso (<paramref name="hayCaso"/> falso) renormaliza: el trato es el 100 %
        /// (design.md, "Aritmetica del progreso"). Devuelve siempre [0,1].
        /// </summary>
        public float Mezclar(bool hayCaso, float cobertura, float cierre, float trato)
        {
            if (!hayCaso)
                return Clamp01(trato);

            float levantamiento = PesoDeCobertura * cobertura + PesoDeCierre * cierre;
            float mezcla = PesoDeTrato * trato + PesoDeLevantamiento * levantamiento;
            return Clamp01(mezcla);
        }

        private static float Clamp01(float x) => x < 0f ? 0f : (x > 1f ? 1f : x);
    }
}
