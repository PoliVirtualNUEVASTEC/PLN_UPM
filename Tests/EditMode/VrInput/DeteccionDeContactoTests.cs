using System.Collections.Generic;
using System.Linq;
using NpcAi.Core;
using NpcAi.VrInput;
using NUnit.Framework;

namespace NpcAi.VrInput.Tests
{
    /// <summary>
    /// Detector 3 (design.md): <see cref="PhysicalAction.TocarPaciente"/> a partir del pulso de
    /// contacto entregado por <c>SpatialSample.PulsoDeContacto</c>, con enfriamiento (antirrebote)
    /// para que un solape continuo no inunde de eventos.
    /// </summary>
    public class DeteccionDeContactoTests
    {
        private static VrInputSettings Config() => new VrInputSettings
        {
            SegundosDeEnfriamientoDeContacto = 1.0f,
        };

        private static SpatialSample MuestraDeContacto(bool pulso, float deltaSegundos)
        {
            return new SpatialSample(
                posicionDeCabeza: new Vec3(0f, 0f, 0f),
                frenteDeCabeza: new Vec3(0f, 0f, 1f),
                posicionDelObjetivo: new Vec3(0f, 0f, 0f),
                hayObjetivo: false,
                pulsoDeContacto: pulso,
                deltaSegundos: deltaSegundos);
        }

        private static SpatialPhysicalActionSource CrearSujeto(out List<PhysicalAction> recibidas)
        {
            var sujeto = new SpatialPhysicalActionSource(Config(), null);
            var lista = new List<PhysicalAction>();
            sujeto.OnAction += lista.Add;
            recibidas = lista;
            return sujeto;
        }

        [Test]
        public void Primer_pulso_levanta_TocarPaciente()
        {
            var sujeto = CrearSujeto(out var recibidas);

            sujeto.ProcesarMuestra(MuestraDeContacto(true, 0.1f));

            Assert.AreEqual(new[] { PhysicalAction.TocarPaciente }, recibidas.ToArray());
        }

        [Test]
        public void Contacto_continuo_dentro_del_enfriamiento_no_repite_el_evento()
        {
            var sujeto = CrearSujeto(out var recibidas);

            // solape continuo: pulso activo en cada muestra, deltas que suman menos que 1.0s
            for (var i = 0; i < 5; i++)
                sujeto.ProcesarMuestra(MuestraDeContacto(true, 0.1f));

            Assert.AreEqual(1, recibidas.Count(a => a == PhysicalAction.TocarPaciente));
        }

        [Test]
        public void Pulso_pasado_el_enfriamiento_vuelve_a_levantar_TocarPaciente()
        {
            var sujeto = CrearSujeto(out var recibidas);

            sujeto.ProcesarMuestra(MuestraDeContacto(true, 0.1f)); // primer pulso: emite

            for (var i = 0; i < 11; i++)
                sujeto.ProcesarMuestra(MuestraDeContacto(false, 0.1f)); // deja vencer el enfriamiento (1.0s)

            sujeto.ProcesarMuestra(MuestraDeContacto(true, 0.1f)); // segundo pulso: enfriamiento vencido

            Assert.AreEqual(2, recibidas.Count(a => a == PhysicalAction.TocarPaciente));
        }

        [Test]
        public void Sin_pulso_no_emite_TocarPaciente()
        {
            var sujeto = CrearSujeto(out var recibidas);

            for (var i = 0; i < 5; i++)
                sujeto.ProcesarMuestra(MuestraDeContacto(false, 0.1f));

            Assert.IsEmpty(recibidas);
        }
    }
}
