using System.Collections.Generic;
using System.Linq;
using NpcAi.Core;
using NpcAi.VrInput;
using NUnit.Framework;

namespace NpcAi.VrInput.Tests
{
    /// <summary>
    /// Detector 2 (design.md): <see cref="PhysicalAction.Acercarse"/>/<see cref="PhysicalAction.Alejarse"/>
    /// por banda de histeresis. La primera muestra siembra sin emitir (AD8); oscilar dentro de
    /// la banda no debe producir una rafaga de eventos alternados.
    /// </summary>
    public class HisteresisDeDistanciaTests
    {
        private static VrInputSettings Config() => new VrInputSettings
        {
            MetrosParaAcercarse = 1.2f,
            MetrosParaAlejarse = 2.0f,
        };

        /// <summary>
        /// El objetivo esta sobre +Z, pero el frente de cabeza mira hacia +X: el angulo de
        /// mirada queda fijo en 90 grados (fuera de cualquier cono razonable), asi que el
        /// Detector 1 nunca acumula ni emite <see cref="PhysicalAction.ContactoVisual"/> y estas
        /// pruebas aislan el Detector 2 (distancia) sin contaminacion cruzada.
        /// </summary>
        private static SpatialSample MuestraADistancia(float distancia, float deltaSegundos)
        {
            return new SpatialSample(
                posicionDeCabeza: new Vec3(0f, 0f, 0f),
                frenteDeCabeza: new Vec3(1f, 0f, 0f),
                posicionDelObjetivo: new Vec3(0f, 0f, distancia),
                hayObjetivo: true,
                pulsoDeContacto: false,
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
        public void Primera_muestra_siembra_sin_emitir()
        {
            var sujeto = CrearSujeto(out var recibidas);

            sujeto.ProcesarMuestra(MuestraADistancia(5f, 0.1f)); // > punto medio (1.6): siembra Lejos

            Assert.IsEmpty(recibidas);
            Assert.AreEqual(EstadoDeDistancia.Lejos, sujeto.DistanciaActual);
        }

        [Test]
        public void Cruzar_el_umbral_de_entrada_levanta_Acercarse_una_vez()
        {
            var sujeto = CrearSujeto(out var recibidas);

            sujeto.ProcesarMuestra(MuestraADistancia(5f, 0.1f));   // siembra Lejos, sin emitir
            sujeto.ProcesarMuestra(MuestraADistancia(1.0f, 0.1f)); // cruza el umbral de entrada (<= 1.2)
            sujeto.ProcesarMuestra(MuestraADistancia(0.8f, 0.1f)); // sigue cerca: no repite

            Assert.AreEqual(new[] { PhysicalAction.Acercarse }, recibidas.ToArray());
        }

        [Test]
        public void Cruzar_el_umbral_de_salida_levanta_Alejarse()
        {
            var sujeto = CrearSujeto(out var recibidas);

            sujeto.ProcesarMuestra(MuestraADistancia(1.0f, 0.1f)); // siembra Cerca (<= punto medio 1.6)
            sujeto.ProcesarMuestra(MuestraADistancia(1.5f, 0.1f)); // dentro de la banda: no hay transicion
            sujeto.ProcesarMuestra(MuestraADistancia(2.5f, 0.1f)); // cruza el umbral de salida (>= 2.0)

            Assert.AreEqual(new[] { PhysicalAction.Alejarse }, recibidas.ToArray());
        }

        [Test]
        public void Oscilar_sobre_el_umbral_de_entrada_no_produce_una_rafaga_de_eventos()
        {
            var sujeto = CrearSujeto(out var recibidas);

            sujeto.ProcesarMuestra(MuestraADistancia(5f, 0.1f)); // siembra Lejos

            for (var i = 0; i < 8; i++)
            {
                // oscila alrededor del umbral de entrada (1.2), siempre dentro de la banda
                var distancia = i % 2 == 0 ? 1.1f : 1.3f;
                sujeto.ProcesarMuestra(MuestraADistancia(distancia, 0.1f));
            }

            Assert.AreEqual(1, recibidas.Count);
            Assert.AreEqual(PhysicalAction.Acercarse, recibidas[0]);
        }

        [Test]
        public void Perder_el_objetivo_no_cuenta_como_Alejarse()
        {
            var sujeto = CrearSujeto(out var recibidas);

            sujeto.ProcesarMuestra(MuestraADistancia(1.0f, 0.1f)); // siembra Cerca
            sujeto.ProcesarMuestra(new SpatialSample(
                new Vec3(0f, 0f, 0f), new Vec3(1f, 0f, 0f), new Vec3(0f, 0f, 0f),
                hayObjetivo: false, pulsoDeContacto: false, deltaSegundos: 0.1f)); // se pierde el objetivo

            Assert.IsEmpty(recibidas);
            Assert.AreEqual(EstadoDeDistancia.Cerca, sujeto.DistanciaActual);
        }
    }
}
