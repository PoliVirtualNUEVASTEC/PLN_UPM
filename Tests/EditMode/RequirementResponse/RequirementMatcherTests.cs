using System.Collections.Generic;
using NpcAi.Core;
using NUnit.Framework;

namespace NpcAi.RequirementResponse.Tests
{
    /// <summary>
    /// Unidad de <see cref="RequirementMatcher"/>: normalizacion, coincidencia esperada,
    /// no-coincidencia con un turno social y desempate por menor indice (AD11 de
    /// <c>design.md</c>). <c>Match</c> no recibe <c>Intent</c>: que el emparejamiento no
    /// dependa de el (AD5) se prueba donde existe un <c>Respond</c> que podria romperlo
    /// (<c>ScriptedRequirementResponderTests</c>).
    /// </summary>
    public class RequirementMatcherTests
    {
        // Dominio del caso 01 (torneo de futbol). Cada requerimiento lleva una palabra de
        // contenido propia, asi ninguna frase social alcanza a cubrir un ejemplo completo.
        private static List<Requerimiento> TablaDePrueba() => new List<Requerimiento>
        {
            new Requerimiento("equipos", Receptivity.NoReceptivo,
                new[] { "como identifican equipo", "que datos manejan del equipo" },
                "Cada equipo se registra con su nombre.", null, null),
            new Requerimiento("arbitros", Receptivity.Neutral,
                new[] { "como asignan arbitros", "cuantos partidos pita un arbitro" },
                "Cada arbitro tiene codigo de licencia.", null, null),
        };

        private static int Emparejar(string frase, IReadOnlyList<Requerimiento> tabla) =>
            RequirementMatcher.Match(RequirementMatcher.Normalizar(frase), tabla);

        [Test]
        public void Normalizar_quita_mayusculas_tildes_y_signos()
        {
            Assert.AreEqual("cual es el presupuesto del proyecto",
                RequirementMatcher.Normalizar("¿Cuál es el PRESUPUESTO del proyecto?"));
        }

        [Test]
        public void Normalizar_colapsa_espacios_y_cadena_vacia_o_nula_da_vacio()
        {
            Assert.AreEqual("hola mundo", RequirementMatcher.Normalizar("  Hola   mundo  "));
            Assert.AreEqual(string.Empty, RequirementMatcher.Normalizar(""));
            Assert.AreEqual(string.Empty, RequirementMatcher.Normalizar(null));
        }

        [Test]
        public void Match_encuentra_el_requerimiento_esperado()
        {
            Assert.AreEqual(0, Emparejar("¿Cómo identifican hoy a cada equipo?", TablaDePrueba()));
            Assert.AreEqual(1, Emparejar("¿Cómo asignan los árbitros?", TablaDePrueba()));
        }

        [Test]
        public void Match_no_encuentra_nada_en_un_turno_social()
        {
            Assert.AreEqual(-1, Emparejar("Buenos días, ¿cómo está usted?", TablaDePrueba()));
            Assert.AreEqual(-1, Emparejar("¿Qué clima hace hoy?", TablaDePrueba()));
        }

        [Test]
        public void Match_exige_cubrir_todas_las_palabras_de_un_ejemplo()
        {
            Assert.AreEqual(-1, Emparejar("¿Cómo identifican?", TablaDePrueba()),
                "Falta \"equipo\": un ejemplo cubierto a medias NO empareja");
            Assert.AreEqual(0, Emparejar("¿Cómo identifican al equipo?", TablaDePrueba()));
        }

        [Test]
        public void Match_sin_texto_o_sin_tabla_devuelve_menos_uno()
        {
            const string frase = "como identifican equipo"; // empareja con TablaDePrueba()[0]

            Assert.AreEqual(0, RequirementMatcher.Match(frase, TablaDePrueba()),
                "Control: con texto y tabla SI empareja");
            Assert.AreEqual(-1, RequirementMatcher.Match("", TablaDePrueba()));
            Assert.AreEqual(-1, RequirementMatcher.Match(null, TablaDePrueba()));
            Assert.AreEqual(-1, RequirementMatcher.Match(frase, null));
            Assert.AreEqual(-1, RequirementMatcher.Match(frase, new List<Requerimiento>()));
        }

        [Test]
        public void Match_salta_un_requerimiento_nulo_y_sigue_con_el_siguiente()
        {
            var tabla = new List<Requerimiento> { null, TablaDePrueba()[0] };

            Assert.AreEqual(1, RequirementMatcher.Match("como identifican equipo", tabla));
        }

        [Test]
        public void En_empate_de_cobertura_gana_el_requerimiento_de_menor_indice()
        {
            // "primero" y "segundo" califican para la misma pregunta: gana el indice 1, no el 2
            // (y tampoco el 0, que no califica).
            var tabla = new List<Requerimiento>
            {
                new Requerimiento("otro", Receptivity.Neutral, new[] { "calendario" }, "Respuesta O.", null, null),
                new Requerimiento("primero", Receptivity.Neutral, new[] { "presupuesto" }, "Respuesta A.", null, null),
                new Requerimiento("segundo", Receptivity.Receptivo, new[] { "presupuesto" }, "Respuesta B.", null, null),
            };

            Assert.AreEqual(1, Emparejar("¿Cuál es el presupuesto?", tabla));
        }
    }
}
