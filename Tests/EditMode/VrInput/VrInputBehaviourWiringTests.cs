using System.Collections.Generic;
using NpcAi.Core;
using NpcAi.Core.Channels;
using NpcAi.VrInput;
using NpcAi.VrInput.Fakes;
using NUnit.Framework;
using UnityEngine;

namespace NpcAi.VrInput.Tests
{
    /// <summary>
    /// Cableado de <see cref="VrInputBehaviour"/> (tasks.md 3.4; precedente
    /// <c>NpcPresenterBehaviourTests</c> de M8 y <c>OfflineSpeechToTextWiringTests</c> de M1):
    /// <c>OnEnable</c> suscribe el <c>OnAction</c> del nucleo interno (AD9 -- nunca
    /// <see cref="PhysicalActionChannel"/>, que solo recibe <c>Raise</c>) y <c>OnDisable</c>
    /// desuscribe sin fuga; sin <c>Camera</c> ni <c>Transform</c> objetivo asignados es un
    /// no-op silencioso (AD10). Usa <c>CablearParaPrueba</c>/<c>DescablearParaPrueba</c>/
    /// <c>BombearParaPrueba</c> en vez de <c>GameObject.SetActive</c>: activar un GameObject
    /// recien creado no dispara <c>Awake</c>/<c>OnEnable</c> de forma confiable dentro de un
    /// metodo de prueba EditMode sincrono. <see cref="ScriptedSpatialSampler"/> (doble de PR1)
    /// guiona la trayectoria sin tocar <c>Camera</c>/<c>Transform</c> reales.
    /// </summary>
    public class VrInputBehaviourWiringTests
    {
        [Test]
        public void OnEnable_suscribe_y_OnDisable_desuscribe_sin_fugas()
        {
            var canal = ScriptableObject.CreateInstance<PhysicalActionChannel>();
            var muestreador = new ScriptedSpatialSampler();
            muestreador.Encolar(MuestraDeContacto(0.1f)); // primer pulso: siempre emite
            muestreador.Encolar(MuestraDeContacto(2f));   // segundo pulso: fuera del enfriamiento por defecto

            var go = new GameObject("m7-behaviour-test");
            var b = go.AddComponent<VrInputBehaviour>();
            b._canal = canal;

            var recibidas = new List<PhysicalAction>();
            canal.Subscribe(recibidas.Add);

            b.CablearParaPrueba(muestreador); // Awake + OnEnable -> suscribe al nucleo interno
            b.BombearParaPrueba();            // consume el primer pulso -> TocarPaciente

            Assert.AreEqual(1, recibidas.Count, "tras OnEnable el behaviour publica en el canal lo que levanta el nucleo interno");
            Assert.AreEqual(PhysicalAction.TocarPaciente, recibidas[0]);

            b.DescablearParaPrueba(); // OnDisable -> desuscribe del nucleo interno
            b.BombearParaPrueba();    // consume el segundo pulso, ya desuscrito

            Assert.AreEqual(1, recibidas.Count, "tras OnDisable ya no publica: sin fuga");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(canal);
        }

        [Test]
        public void Sin_camara_u_objetivo_asignados_no_lanza()
        {
            var canal = ScriptableObject.CreateInstance<PhysicalActionChannel>();
            var recibidas = new List<PhysicalAction>();
            canal.Subscribe(recibidas.Add);

            var go = new GameObject("m7-behaviour-sin-camara");
            var b = go.AddComponent<VrInputBehaviour>();
            b._canal = canal;

            // Sin argumento: arma el UnitySpatialSampler real con _camaraDelHmd/_objetivo
            // nulos (nunca asignados en este GameObject de prueba) -- AD10.
            Assert.DoesNotThrow(() => b.CablearParaPrueba());
            Assert.DoesNotThrow(() => b.BombearParaPrueba());

            Assert.AreEqual(0, recibidas.Count, "sin camara ni objetivo, LeerMuestra devuelve false y el frame es no-op silencioso (AD10)");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(canal);
        }

        private static SpatialSample MuestraDeContacto(float deltaSegundos)
            => new SpatialSample(
                posicionDeCabeza: default,
                frenteDeCabeza: default,
                posicionDelObjetivo: default,
                hayObjetivo: false, // aisla los detectores de mirada/distancia de este de contacto
                pulsoDeContacto: true,
                deltaSegundos: deltaSegundos);
    }
}
