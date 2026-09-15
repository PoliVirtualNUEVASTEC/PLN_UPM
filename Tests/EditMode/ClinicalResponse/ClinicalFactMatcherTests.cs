using System.Collections.Generic;
using NUnit.Framework;

namespace NpcAi.ClinicalResponse.Tests
{
    /// <summary>
    /// Unidad de <see cref="ClinicalFactMatcher"/>: normalizacion, coincidencia esperada,
    /// no-coincidencia con un turno social, y el criterio de empate de AD8.
    /// </summary>
    public class ClinicalFactMatcherTests
    {
        private static List<Hecho> HechosDePrueba() => new List<Hecho>
        {
            new Hecho("inicio_sintoma",
                new[] { "desde cuando", "hace cuanto", "cuando empezo" },
                "Hace unos dos meses."),
            new Hecho("alergias",
                new[] { "es alergica", "alergica a algo" },
                "Soy alergica al Tramadol."),
        };

        [Test]
        public void Normalizar_quita_mayusculas_tildes_y_signos()
        {
            Assert.AreEqual("desde cuando le empezo el dolor",
                ClinicalFactMatcher.Normalizar("¿Desde CUÁNDO le empezó el dolor?"));
        }

        [Test]
        public void Normalizar_colapsa_espacios_y_cadena_vacia_da_vacio()
        {
            Assert.AreEqual("hola mundo", ClinicalFactMatcher.Normalizar("  Hola   mundo  "));
            Assert.AreEqual(string.Empty, ClinicalFactMatcher.Normalizar(""));
            Assert.AreEqual(string.Empty, ClinicalFactMatcher.Normalizar(null));
        }

        [Test]
        public void Match_encuentra_el_campo_que_pregunta_por_el_inicio_del_sintoma()
        {
            var normalizado = ClinicalFactMatcher.Normalizar("¿Desde cuándo le empezó el dolor?");

            Assert.AreEqual(0, ClinicalFactMatcher.Match(normalizado, HechosDePrueba()));
        }

        [Test]
        public void Match_encuentra_el_campo_de_alergias_con_otra_frase_del_mismo_hecho()
        {
            var normalizado = ClinicalFactMatcher.Normalizar("¿Es alérgica a algo?");

            Assert.AreEqual(1, ClinicalFactMatcher.Match(normalizado, HechosDePrueba()));
        }

        [Test]
        public void Match_no_encuentra_nada_en_un_turno_social()
        {
            var normalizado = ClinicalFactMatcher.Normalizar("Buenos días, ¿cómo se siente?");

            Assert.AreEqual(-1, ClinicalFactMatcher.Match(normalizado, HechosDePrueba()));
        }

        [Test]
        public void Match_sin_hechos_o_sin_texto_devuelve_menos_uno()
        {
            Assert.AreEqual(-1, ClinicalFactMatcher.Match("", HechosDePrueba()));
            Assert.AreEqual(-1, ClinicalFactMatcher.Match(null, HechosDePrueba()));
            Assert.AreEqual(-1, ClinicalFactMatcher.Match("desde cuando", null));
            Assert.AreEqual(-1, ClinicalFactMatcher.Match("desde cuando", new List<Hecho>()));
        }

        [Test]
        public void En_empate_de_cobertura_gana_el_hecho_de_menor_indice()
        {
            // Ambos hechos califican para esta pregunta (comparten la palabra "dolor" con
            // un ejemplo de una sola palabra); el de indice 0 debe ganar por orden de lista.
            var hechos = new List<Hecho>
            {
                new Hecho("primero", new[] { "dolor" }, "Respuesta A"),
                new Hecho("segundo", new[] { "dolor" }, "Respuesta B"),
            };

            var normalizado = ClinicalFactMatcher.Normalizar("¿Tiene dolor?");

            Assert.AreEqual(0, ClinicalFactMatcher.Match(normalizado, hechos));
        }
    }
}
