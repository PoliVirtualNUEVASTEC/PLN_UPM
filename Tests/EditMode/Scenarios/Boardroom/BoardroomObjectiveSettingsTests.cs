using NUnit.Framework;

namespace NpcAi.Scenarios.Boardroom.Tests
{
    /// <summary>
    /// tasks.md 1.1: fija <see cref="BoardroomObjectiveSettings"/> — los complementos exactos
    /// (AD3), el clamp/renormalización de <see cref="BoardroomObjectiveSettings.Mezclar"/> (AD7),
    /// el mínimo de <see cref="BoardroomObjectiveSettings.PasosDeTrato"/> y el clamp de pesos
    /// fuera de rango en el constructor. RED: escrito antes de que
    /// <c>BoardroomObjectiveSettings</c> exista; no compila hasta la tarea 1.2 (GREEN).
    /// Precedente: <c>VrInputSettingsAssetTests</c> (M7), <c>TriageProgresoTests</c> (M9).
    /// </summary>
    public class BoardroomObjectiveSettingsTests
    {
        private const float Tolerancia = 1e-4f;

        [Test]
        public void PesoDeLevantamiento_es_el_complemento_exacto_de_PesoDeTrato()
        {
            var settings = new BoardroomObjectiveSettings(pesoDeTrato: 0.2f);

            Assert.AreEqual(0.8f, settings.PesoDeLevantamiento, Tolerancia);
        }

        [Test]
        public void PesoDeCierre_es_el_complemento_exacto_de_PesoDeCobertura()
        {
            var settings = new BoardroomObjectiveSettings(pesoDeCobertura: 0.75f);

            Assert.AreEqual(0.25f, settings.PesoDeCierre, Tolerancia);
        }

        [Test]
        public void Mezclar_con_las_tres_vias_en_uno_da_techo_exacto_de_uno()
        {
            // Con los defaults (0.2 / 0.75) y con pesos arbitrarios: el techo es 1f por
            // construccion (AD3), no por casualidad de los valores por defecto.
            var porDefecto = new BoardroomObjectiveSettings();
            var arbitraria = new BoardroomObjectiveSettings(pesoDeTrato: 0.6f, pesoDeCobertura: 0.1f);

            Assert.AreEqual(1f, porDefecto.Mezclar(hayCaso: true, cobertura: 1f, cierre: 1f, trato: 1f), Tolerancia);
            Assert.AreEqual(1f, arbitraria.Mezclar(hayCaso: true, cobertura: 1f, cierre: 1f, trato: 1f), Tolerancia);
        }

        [Test]
        public void Mezclar_aplana_a_la_formula_confirmada_con_los_pesos_reales()
        {
            // Data/Scenarios/Boardroom.asset: pesoDeTrato 0.2, pesoDeCobertura 0.75
            // => Progress01 = 0.20*t + 0.60*c + 0.20*k (design.md, "Aritmetica del progreso").
            var settings = new BoardroomObjectiveSettings(pesoDeTrato: 0.2f, pesoDeCobertura: 0.75f);

            Assert.AreEqual(0.20f, settings.Mezclar(true, cobertura: 0f, cierre: 0f, trato: 1f), Tolerancia);
            Assert.AreEqual(0.60f, settings.Mezclar(true, cobertura: 1f, cierre: 0f, trato: 0f), Tolerancia);
            Assert.AreEqual(0.20f, settings.Mezclar(true, cobertura: 0f, cierre: 1f, trato: 0f), Tolerancia);
            Assert.AreEqual(0.80f, settings.Mezclar(true, cobertura: 1f, cierre: 0f, trato: 1f), Tolerancia);
            Assert.AreEqual(0.96f, settings.Mezclar(true, cobertura: 1f, cierre: 1f, trato: 0.8f), Tolerancia);
        }

        [Test]
        public void Mezclar_sin_caso_renormaliza_al_trato_e_ignora_cobertura_y_cierre()
        {
            var settings = new BoardroomObjectiveSettings();

            Assert.AreEqual(0.6f, settings.Mezclar(hayCaso: false, cobertura: 1f, cierre: 1f, trato: 0.6f), Tolerancia);
            Assert.AreEqual(0f, settings.Mezclar(hayCaso: false, cobertura: 1f, cierre: 1f, trato: 0f), Tolerancia);
            Assert.AreEqual(1f, settings.Mezclar(hayCaso: false, cobertura: 0f, cierre: 0f, trato: 1f), Tolerancia);
        }

        [Test]
        public void Mezclar_clampea_el_resultado_con_entradas_fuera_de_rango()
        {
            var settings = new BoardroomObjectiveSettings();

            // Sin caso: trato negativo o mayor a 1 se clampea a [0,1].
            Assert.AreEqual(0f, settings.Mezclar(hayCaso: false, cobertura: 0f, cierre: 0f, trato: -1f), Tolerancia);
            Assert.AreEqual(1f, settings.Mezclar(hayCaso: false, cobertura: 0f, cierre: 0f, trato: 2f), Tolerancia);

            // Con caso: una cobertura fuera de [0,1] no debe romper el clamp final.
            Assert.AreEqual(1f, settings.Mezclar(hayCaso: true, cobertura: 2f, cierre: 1f, trato: 1f), Tolerancia);
            Assert.AreEqual(0f, settings.Mezclar(hayCaso: true, cobertura: -1f, cierre: -1f, trato: -1f), Tolerancia);
        }

        [Test]
        public void PasosDeTrato_tiene_minimo_1()
        {
            var cero = new BoardroomObjectiveSettings(pasosDeTrato: 0);
            var negativo = new BoardroomObjectiveSettings(pasosDeTrato: -5);

            Assert.AreEqual(1, cero.PasosDeTrato);
            Assert.AreEqual(1, negativo.PasosDeTrato);
        }

        [Test]
        public void PasosDeTrato_por_defecto_es_5()
        {
            var settings = new BoardroomObjectiveSettings();

            Assert.AreEqual(5, settings.PasosDeTrato);
        }

        [Test]
        public void Constructor_clampea_pesos_fuera_de_rango_a_0_1()
        {
            var bajoCero = new BoardroomObjectiveSettings(pesoDeTrato: -1f, pesoDeCobertura: -1f);
            var sobreUno = new BoardroomObjectiveSettings(pesoDeTrato: 2f, pesoDeCobertura: 2f);

            Assert.AreEqual(0f, bajoCero.PesoDeTrato, Tolerancia);
            Assert.AreEqual(0f, bajoCero.PesoDeCobertura, Tolerancia);
            Assert.AreEqual(1f, sobreUno.PesoDeTrato, Tolerancia);
            Assert.AreEqual(1f, sobreUno.PesoDeCobertura, Tolerancia);
        }

        [Test]
        public void Constructor_sin_parametros_reproduce_los_defaults_documentados()
        {
            var settings = new BoardroomObjectiveSettings();

            Assert.AreEqual(BoardroomObjectiveSettings.PesoDeTratoPorDefecto, settings.PesoDeTrato, Tolerancia);
            Assert.AreEqual(BoardroomObjectiveSettings.PesoDeCoberturaPorDefecto, settings.PesoDeCobertura, Tolerancia);
            Assert.AreEqual(BoardroomObjectiveSettings.PasosDeTratoPorDefecto, settings.PasosDeTrato);
        }
    }
}
