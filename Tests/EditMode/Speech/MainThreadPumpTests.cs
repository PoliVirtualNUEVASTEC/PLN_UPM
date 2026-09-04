using System.Collections.Generic;
using NpcAi.Core;
using NpcAi.Speech.Threading;
using NUnit.Framework;

namespace NpcAi.Speech.Tests
{
    /// <summary>
    /// design.md, Decision 3: <see cref="ImmediateMainThreadPump"/> es la implementacion
    /// por defecto y hace sincronica la emision (la que usan las pruebas y el hosting en
    /// C# puro); <see cref="QueuedMainThreadPump"/> es la real, encolada hasta
    /// <c>Drenar()</c> (tasks.md 2.9).
    /// </summary>
    public class MainThreadPumpTests
    {
        [Test]
        public void ImmediateMainThreadPump_ejecuta_de_inmediato_al_publicar()
        {
            var pump = new ImmediateMainThreadPump();
            var generacionRecibida = -1;
            string textoRecibido = null;

            pump.Post(3, new Utterance("hola", 0.9f, 1f), (g, u) =>
            {
                generacionRecibida = g;
                textoRecibido = u.Text;
            });

            Assert.AreEqual(3, generacionRecibida);
            Assert.AreEqual("hola", textoRecibido);
        }

        [Test]
        public void ImmediateMainThreadPump_Drenar_no_hace_nada()
        {
            var pump = new ImmediateMainThreadPump();
            var veces = 0;

            pump.Post(1, new Utterance("x", 1f, 1f), (_, __) => veces++);
            pump.Drenar();
            pump.Drenar();

            Assert.AreEqual(1, veces);
        }

        [Test]
        public void QueuedMainThreadPump_no_ejecuta_hasta_Drenar()
        {
            var pump = new QueuedMainThreadPump();
            var ejecutado = false;

            pump.Post(1, new Utterance("x", 1f, 1f), (_, __) => ejecutado = true);

            Assert.IsFalse(ejecutado);
        }

        [Test]
        public void QueuedMainThreadPump_Drenar_ejecuta_en_orden_y_vacia_la_cola()
        {
            var pump = new QueuedMainThreadPump();
            var orden = new List<int>();

            pump.Post(1, new Utterance("a", 1f, 1f), (g, _) => orden.Add(g));
            pump.Post(2, new Utterance("b", 1f, 1f), (g, _) => orden.Add(g));
            pump.Post(3, new Utterance("c", 1f, 1f), (g, _) => orden.Add(g));

            pump.Drenar();

            CollectionAssert.AreEqual(new[] { 1, 2, 3 }, orden);

            orden.Clear();
            pump.Drenar(); // cola vacia: el segundo drenado no repite nada

            CollectionAssert.IsEmpty(orden);
        }

        [Test]
        public void Post_ignora_un_callback_de_emision_nulo()
        {
            var inmediata = new ImmediateMainThreadPump();
            var encolada  = new QueuedMainThreadPump();

            Assert.DoesNotThrow(() => inmediata.Post(1, new Utterance("x", 1f, 1f), null));
            Assert.DoesNotThrow(() => encolada.Post(1, new Utterance("x", 1f, 1f), null));
            Assert.DoesNotThrow(() => encolada.Drenar());
        }
    }
}
