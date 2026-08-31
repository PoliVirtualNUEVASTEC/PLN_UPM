using NpcAi.Core;
using NUnit.Framework;

namespace NpcAi.Core.Tests
{
    /// <summary>Contrato de <see cref="IPhysicalActionSource"/>.</summary>
    public abstract class PhysicalActionSourceContract
    {
        protected abstract IPhysicalActionSource CreateSubject();

        /// <summary>Provoca una accion en la implementacion concreta.</summary>
        protected abstract bool EmitTestAction(IPhysicalActionSource subject, PhysicalAction action);

        [Test]
        public void Entrega_la_accion_a_quien_esta_suscrito()
        {
            var s = CreateSubject();
            var recibida = PhysicalAction.Ninguna;
            s.OnAction += a => recibida = a;

            EmitTestAction(s, PhysicalAction.GestoCalma);

            Assert.AreEqual(PhysicalAction.GestoCalma, recibida);
        }

        [Test]
        public void No_entrega_nada_despues_de_desuscribirse()
        {
            var s = CreateSubject();
            var recibidas = 0;
            void Handler(PhysicalAction _) => recibidas++;

            s.OnAction += Handler;
            s.OnAction -= Handler;
            EmitTestAction(s, PhysicalAction.ContactoVisual);

            Assert.AreEqual(0, recibidas);
        }

        [Test]
        public void Ninguna_no_es_un_evento()
        {
            var s = CreateSubject();
            var recibidas = 0;
            s.OnAction += _ => recibidas++;

            EmitTestAction(s, PhysicalAction.Ninguna);

            Assert.AreEqual(0, recibidas);
        }

        [Test]
        public void Emitir_sin_suscriptores_no_lanza()
        {
            var s = CreateSubject();
            Assert.DoesNotThrow(() => EmitTestAction(s, PhysicalAction.Acercarse));
        }
    }
}
