using System.Collections.Generic;
using NpcAi.Presentation.Threading;
using NUnit.Framework;

namespace NpcAi.Presentation.Tests
{
    /// <summary>Unidad de la bomba al hilo principal de M8 (copia del patrón de M1).</summary>
    public class MainThreadPumpTests
    {
        [Test]
        public void ImmediateMainThreadPump_ejecuta_al_publicar()
        {
            var corrio = false;
            new ImmediateMainThreadPump().Post(() => corrio = true);
            Assert.IsTrue(corrio);
        }

        [Test]
        public void ImmediateMainThreadPump_Drenar_no_lanza()
        {
            Assert.DoesNotThrow(() => new ImmediateMainThreadPump().Drenar());
        }

        [Test]
        public void QueuedMainThreadPump_no_ejecuta_hasta_Drenar()
        {
            var pump = new QueuedMainThreadPump();
            var corrio = false;

            pump.Post(() => corrio = true);
            Assert.IsFalse(corrio);

            pump.Drenar();
            Assert.IsTrue(corrio);
        }

        [Test]
        public void QueuedMainThreadPump_Drenar_ejecuta_en_orden_y_vacia_la_cola()
        {
            var pump = new QueuedMainThreadPump();
            var orden = new List<int>();

            pump.Post(() => orden.Add(1));
            pump.Post(() => orden.Add(2));
            pump.Post(() => orden.Add(3));

            pump.Drenar();
            Assert.AreEqual(new[] { 1, 2, 3 }, orden);

            pump.Drenar(); // cola vacía: no repite
            Assert.AreEqual(3, orden.Count);
        }

        [Test]
        public void Post_ignora_una_accion_nula()
        {
            Assert.DoesNotThrow(() => new ImmediateMainThreadPump().Post(null));

            var q = new QueuedMainThreadPump();
            Assert.DoesNotThrow(() =>
            {
                q.Post(null);
                q.Drenar();
            });
        }
    }
}
