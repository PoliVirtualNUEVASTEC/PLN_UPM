using NpcAi.Core;
using NUnit.Framework;

namespace NpcAi.Core.Tests
{
    /// <summary>Contrato de <see cref="IScenarioObjective"/>. Lo heredan M9 y M10.</summary>
    public abstract class ScenarioObjectiveContract
    {
        protected abstract IScenarioObjective CreateSubject();

        private static ReceptivityChange Mejora() =>
            new ReceptivityChange(Core.Receptivity.Neutral, Core.Receptivity.Receptivo, 2, "GESTO_EMPATICO");

        private static ReceptivityChange Empeora() =>
            new ReceptivityChange(Core.Receptivity.Neutral, Core.Receptivity.NoReceptivo, -2, "AGRESION_DIRECTA");

        [Test]
        public void Arranca_sin_progreso_y_sin_completar()
        {
            var o = CreateSubject();

            Assert.AreEqual(0f, o.Progress01);
            Assert.IsFalse(o.IsComplete);
        }

        [Test]
        public void El_progreso_siempre_esta_entre_cero_y_uno()
        {
            var o = CreateSubject();

            for (var i = 0; i < 20; i++)
            {
                o.Notify(i % 3 == 0 ? Empeora() : Mejora());
                Assert.GreaterOrEqual(o.Progress01, 0f);
                Assert.LessOrEqual(o.Progress01, 1f);
            }
        }

        [Test]
        public void Completar_implica_progreso_total()
        {
            var o = CreateSubject();

            for (var i = 0; i < 50 && !o.IsComplete; i++)
                o.Notify(Mejora());

            if (o.IsComplete)
                Assert.AreEqual(1f, o.Progress01, 0.0001f);
        }

        [Test]
        public void El_progreso_no_baja_de_cero_por_maltrato()
        {
            var o = CreateSubject();

            for (var i = 0; i < 10; i++)
                o.Notify(Empeora());

            Assert.AreEqual(0f, o.Progress01);
        }

        // --- G11: IsComplete si y solo si Progress01 == 1; reversible; Notify(default) no-op ---

        [Test]
        public void IsComplete_es_verdadero_si_y_solo_si_el_progreso_es_total()
        {
            var o = CreateSubject();

            for (var i = 0; i < 50; i++)
            {
                o.Notify(Mejora());

                var progresoTotal = System.Math.Abs(o.Progress01 - 1f) <= 1e-4f;
                Assert.AreEqual(progresoTotal, o.IsComplete,
                    $"IsComplete debe seguir a (Progress01 == 1): paso {i}, progreso {o.Progress01}");
            }
        }

        [Test]
        public void La_completitud_es_reversible()
        {
            var o = CreateSubject();

            for (var i = 0; i < 50 && !o.IsComplete; i++)
                o.Notify(Mejora());

            Assume.That(o.IsComplete, "El objetivo no llego a completarse; no se puede probar la reversion");

            o.Notify(Empeora());

            Assert.Less(o.Progress01, 1f);
            Assert.IsFalse(o.IsComplete,
                "Tras un retroceso por debajo de 1, IsComplete DEBE volver a false (no es pegajosa)");
        }

        [Test]
        public void Notify_con_el_valor_por_defecto_es_no_op()
        {
            var o = CreateSubject();

            for (var i = 0; i < 3; i++)
                o.Notify(Mejora());

            var progresoAntes = o.Progress01;
            var completoAntes = o.IsComplete;

            o.Notify(default);

            Assert.AreEqual(progresoAntes, o.Progress01);
            Assert.AreEqual(completoAntes, o.IsComplete);
        }
    }
}
