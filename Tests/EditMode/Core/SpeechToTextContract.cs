using NpcAi.Core;
using NUnit.Framework;

namespace NpcAi.Core.Tests
{
    /// <summary>
    /// Contrato de <see cref="ISpeechToText"/>. Tanto el doble de M1 como la
    /// implementacion real (Whisper/Sentis o Vosk) heredan de aqui y deben pasar
    /// exactamente estas pruebas. Es el mecanismo que impide que el doble mienta.
    /// </summary>
    public abstract class SpeechToTextContract
    {
        protected abstract ISpeechToText CreateSubject();

        /// <summary>Provoca una emision en la implementacion concreta. Devuelve false si no emitio.</summary>
        protected abstract bool EmitTestUtterance(ISpeechToText subject, Utterance utterance);

        [Test]
        public void Arranca_sin_escuchar()
        {
            Assert.IsFalse(CreateSubject().IsListening);
        }

        [Test]
        public void Start_y_Stop_cambian_el_estado()
        {
            var s = CreateSubject();

            s.StartListening();
            Assert.IsTrue(s.IsListening);

            s.StopListening();
            Assert.IsFalse(s.IsListening);
        }

        [Test]
        public void No_emite_nada_despues_de_StopListening()
        {
            var s = CreateSubject();
            var recibidas = 0;
            s.OnUtterance += _ => recibidas++;

            s.StartListening();
            s.StopListening();
            EmitTestUtterance(s, new Utterance("hola", 1f, 1f));

            Assert.AreEqual(0, recibidas);
        }

        [Test]
        public void Emite_lo_que_escucha_mientras_esta_escuchando()
        {
            var s = CreateSubject();
            Utterance recibida = default;
            s.OnUtterance += u => recibida = u;

            s.StartListening();
            EmitTestUtterance(s, new Utterance("necesito ayuda", 0.8f, 1.2f));

            Assert.AreEqual("necesito ayuda", recibida.Text);
        }

        [Test]
        public void Parar_dos_veces_no_lanza()
        {
            var s = CreateSubject();
            s.StartListening();
            s.StopListening();
            Assert.DoesNotThrow(() => s.StopListening());
        }
    }
}
