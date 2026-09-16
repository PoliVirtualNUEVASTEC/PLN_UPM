using System;
using System.Collections.Generic;
using System.Linq;
using NpcAi.Core;
using NpcAi.VrInput;
using NUnit.Framework;

namespace NpcAi.VrInput.Tests
{
    /// <summary>
    /// Detector 1 (design.md): <see cref="PhysicalAction.ContactoVisual"/> por permanencia de
    /// mirada de cabeza, con histeresis de liberacion (cono de entrada mas angosto que el cono
    /// de salida). Corre <c>ProcesarMuestra</c> directo, sin muestreador (tabla "Testing
    /// Strategy" de design.md).
    /// </summary>
    public class DeteccionDeMiradaTests
    {
        private static VrInputSettings Config() => new VrInputSettings
        {
            GradosDelConoDeMirada = 20f,
            GradosDeLiberacionDeMirada = 30f,
            SegundosDePermanenciaDeMirada = 0.5f,
        };

        /// <summary>Muestra con el objetivo a un angulo dado, respecto de un frente fijo en +Z.</summary>
        private static SpatialSample MuestraConAngulo(float anguloGrados, float deltaSegundos)
        {
            var radianes = anguloGrados * (float)(Math.PI / 180.0);
            var objetivo = new Vec3((float)Math.Sin(radianes) * 5f, 0f, (float)Math.Cos(radianes) * 5f);
            return new SpatialSample(
                posicionDeCabeza: new Vec3(0f, 0f, 0f),
                frenteDeCabeza: new Vec3(0f, 0f, 1f),
                posicionDelObjetivo: objetivo,
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
        public void Mirada_sostenida_en_el_cono_levanta_ContactoVisual_una_vez()
        {
            var sujeto = CrearSujeto(out var recibidas);

            // dentro del cono (10 < 20 grados); 5 muestras de 0.1s acumulan 0.5s >= permanencia
            for (var i = 0; i < 6; i++)
                sujeto.ProcesarMuestra(MuestraConAngulo(10f, 0.1f));

            Assert.AreEqual(1, recibidas.Count(a => a == PhysicalAction.ContactoVisual));
        }

        [Test]
        public void Vistazo_fugaz_no_levanta_ContactoVisual()
        {
            var sujeto = CrearSujeto(out var recibidas);

            // entra al cono solo 0.2s (< 0.5s de permanencia) y luego sale
            sujeto.ProcesarMuestra(MuestraConAngulo(10f, 0.1f));
            sujeto.ProcesarMuestra(MuestraConAngulo(10f, 0.1f));
            sujeto.ProcesarMuestra(MuestraConAngulo(45f, 0.1f));

            Assert.IsFalse(recibidas.Contains(PhysicalAction.ContactoVisual));
        }

        [Test]
        public void Mirada_continua_no_repite_ContactoVisual_hasta_liberar_el_cono()
        {
            var sujeto = CrearSujeto(out var recibidas);

            for (var i = 0; i < 6; i++)
                sujeto.ProcesarMuestra(MuestraConAngulo(10f, 0.1f)); // levanta una vez a los 0.5s

            for (var i = 0; i < 10; i++)
                sujeto.ProcesarMuestra(MuestraConAngulo(10f, 0.1f)); // sigue dentro del cono: no repite

            Assert.AreEqual(1, recibidas.Count(a => a == PhysicalAction.ContactoVisual));

            // sale del cono ancho de liberacion (35 > 30) y vuelve a entrar: emite otra vez
            sujeto.ProcesarMuestra(MuestraConAngulo(35f, 0.1f));
            for (var i = 0; i < 6; i++)
                sujeto.ProcesarMuestra(MuestraConAngulo(10f, 0.1f));

            Assert.AreEqual(2, recibidas.Count(a => a == PhysicalAction.ContactoVisual));
        }

        [Test]
        public void Perder_el_objetivo_reinicia_la_permanencia()
        {
            var sujeto = CrearSujeto(out var recibidas);

            sujeto.ProcesarMuestra(MuestraConAngulo(10f, 0.4f)); // acumula 0.4s, no llega a 0.5s
            sujeto.ProcesarMuestra(new SpatialSample(
                new Vec3(0f, 0f, 0f), new Vec3(0f, 0f, 1f), new Vec3(0f, 0f, 0f),
                hayObjetivo: false, pulsoDeContacto: false, deltaSegundos: 0.1f)); // se pierde el objetivo

            for (var i = 0; i < 4; i++)
                sujeto.ProcesarMuestra(MuestraConAngulo(10f, 0.1f)); // solo 0.4s tras recuperar objetivo

            Assert.IsFalse(recibidas.Contains(PhysicalAction.ContactoVisual));
        }
    }
}
