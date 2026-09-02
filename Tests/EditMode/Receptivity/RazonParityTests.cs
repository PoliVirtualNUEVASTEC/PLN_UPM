using System;
using System.Collections.Generic;
using NpcAi.Core;
using NpcAi.Receptivity.Fakes;
using NUnit.Framework;

namespace NpcAi.Receptivity.Tests
{
    /// <summary>
    /// Paso 6 de M4: el doble y el motor real hablan el MISMO vocabulario de
    /// ReasonCode. Para cada par (intencion, accion) la razon que reporta
    /// ScriptedReceptivityEngine coincide con la de ReceptivityEngine. El puntaje
    /// puede diferir (el doble es mas simple e ignora el tono); la etiqueta de
    /// diagnostico no.
    /// </summary>
    public class RazonParityTests
    {
        private static IEnumerable<TestCaseData> Combinaciones()
        {
            foreach (Intent intent in Enum.GetValues(typeof(Intent)))
            foreach (PhysicalAction action in Enum.GetValues(typeof(PhysicalAction)))
                yield return new TestCaseData(intent, action).SetName($"Razon_{intent}_{action}");
        }

        [TestCaseSource(nameof(Combinaciones))]
        public void El_doble_y_el_motor_real_reportan_la_misma_razon(Intent intent, PhysicalAction action)
        {
            var personalidad = new PersonalityId("grosero");

            var doble = new ScriptedReceptivityEngine();
            doble.Reset(personalidad);

            var real = new ReceptivityEngine();
            real.Reset(personalidad);

            var entrada = new IntentResult(intent, Tone.Neutral, 1f, 0f);

            var razonDoble = doble.Evaluate(entrada, action).ReasonCode;
            var razonReal = real.Evaluate(entrada, action).ReasonCode;

            Assert.AreEqual(razonReal, razonDoble,
                $"({intent}, {action}) debe dejar la misma razon en el doble y en el motor real");
        }
    }
}
