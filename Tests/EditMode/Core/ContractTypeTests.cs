using System;
using System.Linq;
using NpcAi.Core;
using NUnit.Framework;

namespace NpcAi.Core.Tests
{
    /// <summary>
    /// Invariantes de los tipos del contrato. Si una de estas falla, el contrato cambio
    /// sin que nadie lo notara y hay que revisar Docs/CONTRACT-CHANGELOG.md.
    /// </summary>
    public class ContractTypeTests
    {
        [Test]
        public void Version_del_contrato_es_dos()
        {
            // v2: primer cambio de contrato. La atadura version <-> changelog la cubre
            // ContractVersionChangelogTests; este es el pin literal.
            Assert.AreEqual(2, Contract.Version);
        }

        [Test]
        public void Los_enums_tienen_el_valor_seguro_en_cero()
        {
            Assert.AreEqual(0, (int)Intent.Desconocida);
            Assert.AreEqual(0, (int)Tone.Neutral);
            Assert.AreEqual(0, (int)PhysicalAction.Ninguna);
            Assert.AreEqual(0, (int)Core.Receptivity.Neutral);
        }

        [Test]
        public void La_receptividad_esta_ordenada()
        {
            Assert.Less((int)Core.Receptivity.NoReceptivo, (int)Core.Receptivity.Neutral);
            Assert.Less((int)Core.Receptivity.Neutral,     (int)Core.Receptivity.Receptivo);
        }

        // --- G4: enums congelados en v1 (nombre, valor, orden, conteo) ---

        [Test]
        public void El_enum_Intent_esta_congelado_en_la_v1()
        {
            AssertEnumCongelado(typeof(Intent), new (string nombre, int valor)[]
            {
                ("Desconocida",          0),
                ("SolicitudRespetuosa",  1),
                ("SolicitudAgresiva",    2),
                ("Empatia",              3),
                ("AportaInformacion",    4),
                ("PreguntaFueraDeTema",  5),
                ("Interrupcion",         6),
            });
        }

        [Test]
        public void El_enum_Tone_esta_congelado_en_la_v1()
        {
            AssertEnumCongelado(typeof(Tone), new (string nombre, int valor)[]
            {
                ("Neutral",     0),
                ("Respetuoso",  1),
                ("Agresivo",    2),
                ("Empatico",    3),
                ("Ansioso",     4),
            });
        }

        [Test]
        public void El_enum_PhysicalAction_esta_congelado_en_la_v1()
        {
            AssertEnumCongelado(typeof(PhysicalAction), new (string nombre, int valor)[]
            {
                ("Ninguna",          0),
                ("ContactoVisual",   1),
                ("Acercarse",        2),
                ("Alejarse",         3),
                ("EntregarObjeto",   4),
                ("SenalarPantalla",  5),
                ("GestoCalma",       6),
                ("TocarPaciente",    7),
            });
        }

        [Test]
        public void El_enum_Receptivity_esta_congelado_en_la_v1()
        {
            AssertEnumCongelado(typeof(Core.Receptivity), new (string nombre, int valor)[]
            {
                ("NoReceptivo", -1),
                ("Neutral",      0),
                ("Receptivo",    1),
            });

            Assert.AreEqual(-1, (int)Core.Receptivity.NoReceptivo);
            Assert.AreEqual(1,  (int)Core.Receptivity.Receptivo);
        }

        // --- Utterance ---

        [Test]
        public void Utterance_IsEmpty_refleja_el_texto()
        {
            Assert.IsTrue(new Utterance(null,  0f, 0f).IsEmpty);
            Assert.IsTrue(new Utterance("",    0f, 0f).IsEmpty);
            Assert.IsTrue(new Utterance("   ", 0f, 0f).IsEmpty);
            Assert.IsFalse(new Utterance("necesito ayuda", 0f, 0f).IsEmpty);
        }

        [Test]
        public void Utterance_guarda_los_campos_sin_recortar_ni_validar_rangos()
        {
            // Los rangos (Confidence 0..1, DurationSeconds >= 0) son contrato del
            // productor M1, no invariante del struct: el ctor guarda lo que recibe.
            var u = new Utterance("hola", 5f, -2f);

            Assert.AreEqual("hola", u.Text);
            Assert.AreEqual(5f,     u.Confidence);
            Assert.AreEqual(-2f,    u.DurationSeconds);
        }

        // --- PersonalityId ---

        [Test]
        public void PersonalityId_normaliza_y_compara_por_valor()
        {
            var a = new PersonalityId("Grosero");
            var b = new PersonalityId("  grosero ");

            Assert.AreEqual(a, b);
            Assert.AreEqual("grosero", a.Value);
            Assert.IsTrue(PersonalityId.None.IsNone);
            Assert.IsFalse(a.IsNone);
        }

        [Test]
        public void PersonalityId_nulo_o_solo_espacios_es_None()
        {
            Assert.IsTrue(new PersonalityId(null).IsNone);
            Assert.IsTrue(new PersonalityId("").IsNone);
            Assert.IsTrue(new PersonalityId("   ").IsNone);
            Assert.IsTrue(PersonalityId.None.IsNone);
        }

        // --- v2: ClinicalCaseId (espejo de PersonalityId) ---

        [Test]
        public void ClinicalCaseId_normaliza_el_valor_recibido()
        {
            Assert.AreEqual("caso-01", new ClinicalCaseId(" Caso-01 ").Value);
        }

        [Test]
        public void ClinicalCaseId_None_es_el_valor_por_defecto_y_hashea_a_cero()
        {
            Assert.IsTrue(ClinicalCaseId.None.IsNone);
            Assert.AreEqual(0, ClinicalCaseId.None.GetHashCode());
            Assert.IsTrue(new ClinicalCaseId("   ").IsNone);
        }

        [Test]
        public void ClinicalCaseId_compara_por_valor_normalizado_y_Ordinal()
        {
            var a = new ClinicalCaseId("caso-01");
            var b = new ClinicalCaseId(" CASO-01 ");
            var otro = new ClinicalCaseId("caso-02");

            Assert.IsTrue(a == b);
            Assert.AreEqual(a, b);
            Assert.IsTrue(a != otro);
            Assert.AreNotEqual(a, otro);
        }

        // --- v2: ClinicalResponse ---

        [Test]
        public void ClinicalResponse_NoAplica_no_esta_manejado()
        {
            Assert.IsFalse(ClinicalResponse.NoAplica.Handled);
        }

        // --- IntentResult ---

        [Test]
        public void IntentResult_Unknown_es_seguro()
        {
            var r = IntentResult.Unknown();

            Assert.AreEqual(Intent.Desconocida, r.Intent);
            Assert.AreEqual(Tone.Neutral,       r.Tone);
            Assert.AreEqual(0f,                 r.Confidence);
            Assert.AreEqual(0f,                 r.LatencyMs);
        }

        [Test]
        public void IntentResult_Unknown_preserva_la_latencia_y_mantiene_lo_seguro()
        {
            var r = IntentResult.Unknown(12.5f);

            Assert.AreEqual(12.5f,             r.LatencyMs);
            Assert.AreEqual(Intent.Desconocida, r.Intent);
            Assert.AreEqual(Tone.Neutral,       r.Tone);
            Assert.AreEqual(0f,                 r.Confidence);
        }

        // --- ReceptivityChange ---

        [Test]
        public void ReceptivityChange_reporta_direccion()
        {
            var mejora = new ReceptivityChange(Core.Receptivity.Neutral, Core.Receptivity.Receptivo, 2, "X");
            var empeora = new ReceptivityChange(Core.Receptivity.Neutral, Core.Receptivity.NoReceptivo, -2, "Y");
            var igual = new ReceptivityChange(Core.Receptivity.Neutral, Core.Receptivity.Neutral, 0, "Z");

            Assert.IsTrue(mejora.Improved);
            Assert.IsTrue(empeora.Worsened);
            Assert.IsFalse(igual.Changed);
        }

        [Test]
        public void ReceptivityChange_normaliza_ReasonCode_nulo_a_cadena_vacia()
        {
            var cambio = new ReceptivityChange(Core.Receptivity.Neutral, Core.Receptivity.Neutral, 0, null);

            Assert.AreEqual(string.Empty, cambio.ReasonCode);
            Assert.IsNotNull(cambio.ReasonCode);
        }

        // --- NpcReply ---

        [Test]
        public void NpcReply_normaliza_los_tres_string_nulos_a_cadena_vacia()
        {
            var reply = new NpcReply(null, null, null);

            Assert.AreEqual(string.Empty, reply.Text);
            Assert.AreEqual(string.Empty, reply.EmotionTag);
            Assert.AreEqual(string.Empty, reply.AnimationCue);
            Assert.IsTrue(reply.IsEmpty);
        }

        // --- G12: politica de igualdad de los DTO (diferida a v2) ---

        [Test]
        public void Solo_PersonalityId_implementa_IEquatable_en_la_v1()
        {
            Assert.IsFalse(ImplementaIEquatable(typeof(Utterance)),         "Utterance no debe implementar IEquatable<T> en la v1");
            Assert.IsFalse(ImplementaIEquatable(typeof(IntentResult)),      "IntentResult no debe implementar IEquatable<T> en la v1");
            Assert.IsFalse(ImplementaIEquatable(typeof(ReceptivityChange)), "ReceptivityChange no debe implementar IEquatable<T> en la v1");
            Assert.IsFalse(ImplementaIEquatable(typeof(NpcReply)),          "NpcReply no debe implementar IEquatable<T> en la v1");
            Assert.IsTrue(ImplementaIEquatable(typeof(PersonalityId)),      "PersonalityId si tiene igualdad completa y explicita");
        }

        [Test]
        public void Los_DTO_usan_igualdad_estructural_por_defecto()
        {
            var a = new NpcReply("hola", "molesto", "cruzar_brazos");
            var b = new NpcReply("hola", "molesto", "cruzar_brazos");
            var c = new NpcReply("otra", "molesto", "cruzar_brazos");

            Assert.AreEqual(a, b);
            Assert.AreNotEqual(a, c);
        }

        // --- Helpers ---

        /// <summary>
        /// Afirma que un enum expone exactamente el conjunto de miembros esperado: mismo
        /// nombre, mismo valor entero y mismo conteo. Fuente unica para G4.
        /// El orden NO se compara contra la posicion en el codigo fuente: Enum.GetNames
        /// ordena por magnitud SIN signo (Receptivity.NoReceptivo = -1 queda al final,
        /// no al principio), y ese orden es un detalle de la reflexion, no del contrato.
        /// Lo que el contrato congela es el mapeo nombre -> valor y la cardinalidad.
        /// </summary>
        private static void AssertEnumCongelado(Type tipo, (string nombre, int valor)[] esperado)
        {
            var reales = Enum.GetNames(tipo)
                .Zip(Enum.GetValues(tipo).Cast<object>().Select(Convert.ToInt32),
                     (nombre, valor) => (nombre, valor))
                .OrderBy(p => p.nombre, StringComparer.Ordinal)
                .ToArray();

            var esperadoOrdenado = esperado
                .OrderBy(p => p.nombre, StringComparer.Ordinal)
                .ToArray();

            Assert.AreEqual(esperadoOrdenado.Length, reales.Length, $"{tipo.Name}: numero de miembros");

            for (var i = 0; i < esperadoOrdenado.Length; i++)
            {
                Assert.AreEqual(esperadoOrdenado[i].nombre, reales[i].nombre, $"{tipo.Name}: falta o sobra un miembro (se esperaba {esperadoOrdenado[i].nombre})");
                Assert.AreEqual(esperadoOrdenado[i].valor,  reales[i].valor,  $"{tipo.Name}: valor de {esperadoOrdenado[i].nombre}");
            }
        }

        /// <summary>
        /// True si el tipo declara <see cref="IEquatable{T}"/> de forma explicita. Los value
        /// types reciben Equals estructural por defecto sin declarar esta interfaz.
        /// </summary>
        private static bool ImplementaIEquatable(Type tipo) =>
            tipo.GetInterfaces().Any(i =>
                i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEquatable<>));
    }
}
