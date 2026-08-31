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
    }
}
