using NpcAi.Core;
using NUnit.Framework;

namespace NpcAi.Nlu.Tests
{
    [TestFixture]
    public class SemanticMatcherTests
    {
        // --- Pruebas de ToneAnalyzer ---

        [TestCase("por favor ayudeme señor", Tone.Respetuoso)]
        [TestCase("muchas gracias con permiso", Tone.Respetuoso)]
        [TestCase("callese y hagalo ya rapido", Tone.Agresivo)]
        [TestCase("incompetente apurese", Tone.Agresivo)]
        [TestCase("lo entiendo comprendo su dolor", Tone.Empatico)]
        [TestCase("calma tranquilo todo estara bien", Tone.Empatico)]
        [TestCase("rapido urgente auxilio dios mio", Tone.Ansioso)]
        [TestCase("el paciente tiene presion 120 80", Tone.Neutral)]
        [TestCase("", Tone.Neutral)]
        [TestCase(null, Tone.Neutral)]
        public void Clasifica_el_tono_correctamente(string texto, Tone tonoEsperado)
        {
            var tono = ToneAnalyzer.AnalyzeTone(texto);
            Assert.AreEqual(tonoEsperado, tono);
        }

        // --- Pruebas de SemanticMatcher (Intents) ---

        [TestCase("por favor me ayuda con la ficha medica", Intent.SolicitudRespetuosa)]
        [TestCase("podria revisar los signos vitales", Intent.SolicitudRespetuosa)]
        [TestCase("hagalo ya mismo rapido", Intent.SolicitudAgresiva)]
        [TestCase("callese la boca", Intent.SolicitudAgresiva)]
        [TestCase("entiendo perfectamente su situacion", Intent.Empatia)]
        [TestCase("tranquilo aqui estamos para ayudarlo", Intent.Empatia)]
        [TestCase("el paciente presenta fiebre y presion alta", Intent.AportaInformacion)]
        [TestCase("los datos del monitor marcan saturacion 95", Intent.AportaInformacion)]
        [TestCase("quien gano el partido de futbol ayer", Intent.PreguntaFueraDeTema)]
        [TestCase("que clima hara manana por la tarde", Intent.PreguntaFueraDeTema)]
        [TestCase("un momento espere pare", Intent.Interrupcion)]
        [TestCase("alto detengase", Intent.Interrupcion)]
        [TestCase("asdfghjkl qwerty zxcv", Intent.Desconocida)]
        [TestCase("", Intent.Desconocida)]
        [TestCase(null, Intent.Desconocida)]
        public void Clasifica_la_intencion_correctamente(string texto, Intent intentEsperado)
        {
            var (intent, confidence) = SemanticMatcher.ClassifyIntent(texto);
            Assert.AreEqual(intentEsperado, intent);
            Assert.GreaterOrEqual(confidence, 0f);
            Assert.LessOrEqual(confidence, 1f);

            if (intent == Intent.Desconocida)
            {
                Assert.AreEqual(0f, confidence);
            }
            else
            {
                Assert.Greater(confidence, 0f);
            }
        }
    }
}
