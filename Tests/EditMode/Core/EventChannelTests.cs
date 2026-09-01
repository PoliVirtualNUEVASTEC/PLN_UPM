using NpcAi.Core;
using NpcAi.Core.Channels;
using NUnit.Framework;
using UnityEngine;

namespace NpcAi.Core.Tests
{
    /// <summary>
    /// G3 del contrato v1: semantica de <see cref="EventChannel{T}"/> en
    /// <c>NpcAi.Core.Channels</c> y de los 5 canales concretos del nucleo. Los canales
    /// son <see cref="ScriptableObject"/>, asi que se instancian con
    /// <see cref="ScriptableObject.CreateInstance{T}()"/> y se destruyen con
    /// <see cref="Object.DestroyImmediate(Object)"/>. Para <c>OnDisable</c> se usa
    /// <see cref="Object.DestroyImmediate(Object)"/> y luego se afirma que un
    /// <c>Raise</c> posterior no invoca al handler viejo (decision D6 del diseno).
    /// </summary>
    public class EventChannelTests
    {
        private UtteranceChannel _canal;

        [SetUp]
        public void CrearCanal()
        {
            _canal = ScriptableObject.CreateInstance<UtteranceChannel>();
        }

        [TearDown]
        public void DestruirCanal()
        {
            if (_canal != null)
                Object.DestroyImmediate(_canal);
            _canal = null;
        }

        [Test]
        public void Raise_sin_suscriptores_no_lanza()
        {
            Assert.DoesNotThrow(() => _canal.Raise(new Utterance("nadie escucha", 1f, 1f)));
        }

        [Test]
        public void Raise_entrega_el_mismo_payload_a_cada_suscriptor()
        {
            Utterance recibidaA = default;
            Utterance recibidaB = default;
            System.Action<Utterance> a = u => recibidaA = u;
            System.Action<Utterance> b = u => recibidaB = u;
            _canal.Subscribe(a);
            _canal.Subscribe(b);

            _canal.Raise(new Utterance("necesito ayuda", 0.8f, 1.2f));

            Assert.AreEqual("necesito ayuda", recibidaA.Text);
            Assert.AreEqual("necesito ayuda", recibidaB.Text);
        }

        [Test]
        public void Unsubscribe_corta_la_entrega_a_ese_handler()
        {
            var recibidas = 0;
            System.Action<Utterance> h = _ => recibidas++;

            _canal.Subscribe(h);
            _canal.Raise(new Utterance("uno", 1f, 1f));
            _canal.Unsubscribe(h);
            _canal.Raise(new Utterance("dos", 1f, 1f));

            Assert.AreEqual(1, recibidas);
        }

        [Test]
        public void Subscribe_y_Unsubscribe_con_null_no_lanzan_ni_alteran_la_lista()
        {
            var recibidas = 0;
            System.Action<Utterance> h = _ => recibidas++;
            _canal.Subscribe(h);

            Assert.DoesNotThrow(() =>
            {
                _canal.Subscribe(null);
                _canal.Unsubscribe(null);
            });

            _canal.Raise(new Utterance("sigo suscrito", 1f, 1f));
            Assert.AreEqual(1, recibidas);
        }

        [Test]
        public void OnDisable_limpia_los_suscriptores_y_un_Raise_posterior_no_invoca_al_handler_viejo()
        {
            var canal = ScriptableObject.CreateInstance<UtteranceChannel>();
            var recibidas = 0;
            System.Action<Utterance> h = _ => recibidas++;
            canal.Subscribe(h);

            // DestroyImmediate dispara OnDisable de forma sincrona: limpia la lista interna.
            Object.DestroyImmediate(canal);
            canal.Raise(new Utterance("despues de OnDisable", 1f, 1f));

            Assert.AreEqual(0, recibidas);
        }

        [Test]
        public void Los_cinco_canales_concretos_son_sealed_y_heredan_EventChannel_del_tipo_esperado()
        {
            AssertCanalConcreto<UtteranceChannel, Utterance>();
            AssertCanalConcreto<IntentResultChannel, IntentResult>();
            AssertCanalConcreto<PhysicalActionChannel, PhysicalAction>();
            AssertCanalConcreto<ReceptivityChangeChannel, ReceptivityChange>();
            AssertCanalConcreto<NpcReplyChannel, NpcReply>();
        }

        [Test]
        public void Un_canal_concreto_se_comporta_como_la_base()
        {
            var canal = ScriptableObject.CreateInstance<IntentResultChannel>();
            try
            {
                var recibidas = 0;
                System.Action<IntentResult> h = _ => recibidas++;

                canal.Subscribe(h);
                canal.Raise(IntentResult.Unknown());
                canal.Unsubscribe(h);
                canal.Raise(IntentResult.Unknown());

                Assert.AreEqual(1, recibidas);
            }
            finally
            {
                Object.DestroyImmediate(canal);
            }
        }

        private static void AssertCanalConcreto<TCanal, TPayload>()
        {
            var tipo = typeof(TCanal);

            Assert.IsTrue(tipo.IsSealed, $"{tipo.Name} debe ser sealed");
            Assert.AreEqual(typeof(EventChannel<TPayload>), tipo.BaseType,
                $"{tipo.Name} debe heredar de EventChannel<{typeof(TPayload).Name}>");
        }
    }
}
