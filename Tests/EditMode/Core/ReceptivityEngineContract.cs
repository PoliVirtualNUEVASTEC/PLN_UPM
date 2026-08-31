using NpcAi.Core;
using NUnit.Framework;

namespace NpcAi.Core.Tests
{
    /// <summary>
    /// Contrato de <see cref="IReceptivityEngine"/>. El doble de M4 y el motor real
    /// parametrizado por los perfiles de M5 heredan de aqui.
    /// </summary>
    public abstract class ReceptivityEngineContract
    {
        protected abstract IReceptivityEngine CreateSubject();

        private static readonly PersonalityId Cualquiera = new PersonalityId("grosero");

        private static IntentResult Intencion(Intent intent, Tone tone = Tone.Neutral)
            => new IntentResult(intent, tone, 0.9f, 0f);

        [Test]
        public void Reset_es_determinista_para_la_misma_personalidad()
        {
            var a = CreateSubject(); a.Reset(Cualquiera);
            var b = CreateSubject(); b.Reset(Cualquiera);

            Assert.AreEqual(a.Current, b.Current,
                "Dos motores reiniciados con la misma personalidad deben arrancar igual");
        }

        [Test]
        public void La_transicion_reporta_el_estado_previo_y_el_nuevo()
        {
            var e = CreateSubject();
            e.Reset(Cualquiera);

            var previo = e.Current;
            var cambio = e.Evaluate(Intencion(Intent.Empatia, Tone.Empatico), PhysicalAction.ContactoVisual);

            Assert.AreEqual(previo, cambio.From, "From debe ser el estado antes de evaluar");
            Assert.AreEqual(e.Current, cambio.To, "To debe coincidir con Current despues de evaluar");
        }

        [Test]
        public void Toda_transicion_trae_una_razon()
        {
            var e = CreateSubject();
            e.Reset(Cualquiera);

            var cambio = e.Evaluate(Intencion(Intent.SolicitudAgresiva, Tone.Agresivo), PhysicalAction.Ninguna);

            Assert.IsFalse(string.IsNullOrWhiteSpace(cambio.ReasonCode));
        }

        [Test]
        public void La_agresion_sostenida_nunca_mejora_la_receptividad()
        {
            var e = CreateSubject();
            e.Reset(Cualquiera);
            var inicial = e.Current;

            for (var i = 0; i < 5; i++)
                e.Evaluate(Intencion(Intent.SolicitudAgresiva, Tone.Agresivo), PhysicalAction.Ninguna);

            Assert.LessOrEqual((int)e.Current, (int)inicial);
        }

        [Test]
        public void La_empatia_sostenida_nunca_empeora_la_receptividad()
        {
            var e = CreateSubject();
            e.Reset(Cualquiera);
            var inicial = e.Current;

            for (var i = 0; i < 5; i++)
                e.Evaluate(Intencion(Intent.Empatia, Tone.Empatico), PhysicalAction.ContactoVisual);

            Assert.GreaterOrEqual((int)e.Current, (int)inicial);
        }

        [Test]
        public void Reset_vuelve_al_mismo_estado_despues_de_maltratar_al_NPC()
        {
            var e = CreateSubject();
            e.Reset(Cualquiera);
            var inicial = e.Current;

            for (var i = 0; i < 5; i++)
                e.Evaluate(Intencion(Intent.SolicitudAgresiva, Tone.Agresivo), PhysicalAction.Alejarse);

            e.Reset(Cualquiera);
            Assert.AreEqual(inicial, e.Current);
        }

        [Test]
        public void Una_intencion_desconocida_no_mueve_el_estado()
        {
            var e = CreateSubject();
            e.Reset(Cualquiera);
            var antes = e.Current;

            var cambio = e.Evaluate(IntentResult.Unknown(), PhysicalAction.Ninguna);

            Assert.AreEqual(antes, e.Current);
            Assert.IsFalse(cambio.Changed);
        }
    }
}
