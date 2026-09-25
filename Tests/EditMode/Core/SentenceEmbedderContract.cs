using System;
using NpcAi.Core;
using NUnit.Framework;

namespace NpcAi.Core.Tests
{
    /// <summary>
    /// Contrato de <see cref="ISentenceEmbedder"/>. El doble y la implementacion real de M2
    /// heredan de aqui y pasan las mismas pruebas, sin escena de Unity ni entorno de VR. Los
    /// escenarios que exigen un vector no vacio o un sujeto listo usan <see cref="Assume"/>
    /// para no fallar contra un doble no listo (mismo patron que
    /// <see cref="RequirementResponderContract"/>).
    /// </summary>
    public abstract class SentenceEmbedderContract
    {
        protected abstract ISentenceEmbedder CreateSubject();

        // Frases compartidas por todos los escenarios. El doble y la implementacion real de
        // M2 DEBEN devolver un vector no vacio para ambas cuando estan listos; si no,
        // Assume las omite (no fallan).
        private const string FraseDePrueba = "me duele el pecho desde ayer";
        private const string FraseDeControl = "que clima hace hoy";

        [Test]
        public void Reporta_si_esta_listo_sin_lanzar()
        {
            Assert.DoesNotThrow(() => { var _ = CreateSubject().IsReady; });
        }

        [Test]
        public void Embed_no_lanza_en_ningun_estado()
        {
            var sujeto = CreateSubject();
            var noListo = new EmbebedorDeOracionesNoListo();
            var entradas = new[] { null, "", "   ", "!!!???", new string('a', 5000), "123 456" };

            foreach (var entrada in entradas)
            {
                Assert.DoesNotThrow(() => sujeto.Embed(entrada));
                Assert.DoesNotThrow(() => noListo.Embed(entrada));
            }
        }

        [Test]
        public void Sin_estar_listo_Embed_devuelve_vector_vacio()
        {
            var noListo = new EmbebedorDeOracionesNoListo();
            Assert.IsFalse(noListo.IsReady);
            Assert.IsTrue(noListo.Embed(FraseDePrueba).IsEmpty,
                "Con IsReady == false, Embed DEBE devolver SentenceEmbedding.Empty");

            var sujeto = CreateSubject();
            if (!sujeto.IsReady)
                Assert.IsTrue(sujeto.Embed(FraseDePrueba).IsEmpty,
                    "Un sujeto no listo DEBE degradar a Empty sin lanzar");
        }

        [Test]
        public void Texto_nulo_vacio_o_solo_espacios_devuelve_vector_vacio()
        {
            // Sin Assume: esta garantia vale en cualquier estado de IsReady (AD5).
            var sujeto = CreateSubject();

            Assert.IsTrue(sujeto.Embed(null).IsEmpty);
            Assert.IsTrue(sujeto.Embed("").IsEmpty);
            Assert.IsTrue(sujeto.Embed("   ").IsEmpty);
        }

        [Test]
        public void Listo_y_con_texto_devuelve_vector_no_vacio()
        {
            var sujeto = CreateSubject();
            Assume.That(sujeto.IsReady, "El sujeto no quedo listo");

            Assert.IsFalse(sujeto.Embed(FraseDePrueba).IsEmpty);
        }

        [Test]
        public void Todos_los_vectores_no_vacios_tienen_la_misma_longitud()
        {
            var sujeto = CreateSubject();
            Assume.That(sujeto.IsReady, "El sujeto no quedo listo");

            var a = sujeto.Embed(FraseDePrueba);
            var b = sujeto.Embed(FraseDeControl);
            var c = sujeto.Embed(new string('a', 5000));

            Assume.That(a.IsEmpty, Is.False, "La frase de prueba no produjo vector");
            Assume.That(b.IsEmpty, Is.False, "La frase de control no produjo vector");
            Assume.That(c.IsEmpty, Is.False, "La cadena larga no produjo vector");

            Assert.AreEqual(a.Length, b.Length, "Textos distintos DEBEN producir vectores de igual Length");
            Assert.AreEqual(a.Length, c.Length, "Textos distintos DEBEN producir vectores de igual Length");
        }

        [Test]
        public void Los_componentes_son_finitos()
        {
            var sujeto = CreateSubject();
            Assume.That(sujeto.IsReady, "El sujeto no quedo listo");

            var vector = sujeto.Embed(FraseDePrueba);
            Assume.That(vector.IsEmpty, Is.False);

            for (var i = 0; i < vector.Length; i++)
            {
                Assert.IsFalse(float.IsNaN(vector[i]), $"El componente {i} es NaN");
                Assert.IsFalse(float.IsInfinity(vector[i]), $"El componente {i} es infinito");
            }
        }

        [Test]
        public void Es_determinista_bit_a_bit_para_el_mismo_texto()
        {
            var sujeto = CreateSubject();
            Assume.That(sujeto.IsReady, "El sujeto no quedo listo");

            var a = sujeto.Embed(FraseDePrueba);
            var b = sujeto.Embed(FraseDePrueba);
            var c = sujeto.Embed(FraseDePrueba);

            Assert.AreEqual(a, b);
            Assert.AreEqual(a, c);

            for (var i = 0; i < a.Length; i++)
            {
                Assert.AreEqual(
                    BitConverter.SingleToInt32Bits(a[i]),
                    BitConverter.SingleToInt32Bits(b[i]),
                    $"El componente {i} no es bit-exacto entre llamadas");
            }
        }

        [Test]
        public void Mutar_la_copia_devuelta_no_altera_llamadas_posteriores()
        {
            var sujeto = CreateSubject();
            Assume.That(sujeto.IsReady, "El sujeto no quedo listo");

            var primero = sujeto.Embed(FraseDePrueba);
            Assume.That(primero.IsEmpty, Is.False);

            var copia = primero.ToArray();
            for (var i = 0; i < copia.Length; i++)
                copia[i] = 12345f;

            var segundo = sujeto.Embed(FraseDePrueba);
            Assert.AreEqual(primero, segundo,
                "Mutar la copia de ToArray NO DEBE alterar el vector devuelto por una llamada posterior");
        }

        [Test]
        public void Textos_distintos_no_dan_el_mismo_vector()
        {
            var sujeto = CreateSubject();
            Assume.That(sujeto.IsReady, "El sujeto no quedo listo");

            var a = sujeto.Embed(FraseDePrueba);
            var b = sujeto.Embed(FraseDeControl);
            Assume.That(a.IsEmpty, Is.False);
            Assume.That(b.IsEmpty, Is.False);

            Assert.AreNotEqual(a, b, "Textos distintos NO DEBEN producir el mismo vector (no degenerado)");
        }

        /// <summary>
        /// Stub minimo NO listo, definido DENTRO de Tests/EditMode/Core/ (mismo patron que
        /// <c>RespondedorDeRequerimientosNoListo</c> en <see cref="RequirementResponderContract"/>):
        /// alcanza el camino "no listo" sin depender del doble de M2.
        /// </summary>
        private sealed class EmbebedorDeOracionesNoListo : ISentenceEmbedder
        {
            public bool IsReady => false;
            public SentenceEmbedding Embed(string text) => SentenceEmbedding.Empty;
        }

        /// <summary>
        /// Stub minimo LISTO: sin el, los escenarios que exigen un vector no vacio quedarian
        /// omitidos por <see cref="Assume"/>, no verdes. Hashea las palabras en minusculas a
        /// un numero fijo de cubetas con conteos enteros: bit-exacto, finito y distinto para
        /// frases con contenido distinto. Andamio de prueba de este cambio, no implementacion
        /// de M2 (ver <c>Runtime/Nlu/</c> para el encoder real, fuera de alcance aqui).
        /// </summary>
        private sealed class EmbebedorDeOracionesDePrueba : ISentenceEmbedder
        {
            private const int Cubetas = 8;

            public bool IsReady => true;

            public SentenceEmbedding Embed(string text)
            {
                if (string.IsNullOrWhiteSpace(text))
                    return SentenceEmbedding.Empty;

                var vector = new float[Cubetas];
                var palabras = text.ToLowerInvariant().Split(
                    new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

                foreach (var palabra in palabras)
                    vector[CubetaDe(palabra)] += 1f;

                return new SentenceEmbedding(vector);
            }

            /// <summary>Suma de codigos de caracter modulo <see cref="Cubetas"/>: trivial, determinista, sin dependencias del runtime.</summary>
            private static int CubetaDe(string palabra)
            {
                var suma = 0;
                foreach (var c in palabra)
                    suma += c;
                return suma % Cubetas;
            }
        }

        /// <summary>
        /// Subclase concreta que ejerce la base contra <see cref="EmbebedorDeOracionesDePrueba"/>.
        /// Sin ella NUnit no ejecuta ni un <c>[Test]</c> de la base en este cambio (M2 aun no
        /// expone un embedder real).
        /// </summary>
        public sealed class SentenceEmbedderContractStubTests : SentenceEmbedderContract
        {
            protected override ISentenceEmbedder CreateSubject() => new EmbebedorDeOracionesDePrueba();
        }
    }
}
