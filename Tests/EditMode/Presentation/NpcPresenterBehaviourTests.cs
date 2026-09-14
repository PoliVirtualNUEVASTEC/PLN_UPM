using NpcAi.Core;
using NpcAi.Core.Channels;
using NpcAi.Presentation.Config;
using NUnit.Framework;
using UnityEngine;

namespace NpcAi.Presentation.Tests
{
    /// <summary>
    /// Cableado de <see cref="NpcPresenterBehaviour"/>: se suscribe al <see cref="NpcReplyChannel"/>
    /// en <c>OnEnable</c> y se desuscribe en <c>OnDisable</c>, sin fugas, y tolera que falte el
    /// canal. Usa <see cref="NpcPresenterBehaviour.CablearParaPrueba"/> /
    /// <c>DescablearParaPrueba</c> en vez de <c>GameObject.SetActive</c>: activar un GameObject
    /// recién creado no dispara <c>Awake</c>/<c>OnEnable</c> de forma confiable dentro de un
    /// método de prueba EditMode síncrono.
    /// </summary>
    public class NpcPresenterBehaviourTests
    {
        [Test]
        public void OnEnable_suscribe_al_canal_y_OnDisable_lo_desuscribe()
        {
            var canal  = ScriptableObject.CreateInstance<NpcReplyChannel>();
            var config = ScriptableObject.CreateInstance<PresentationSettingsAsset>();

            var go = new GameObject("m8-behaviour-test");
            var b = go.AddComponent<NpcPresenterBehaviour>();
            b._canal = canal;
            b._configuracion = config;

            b.CablearParaPrueba(); // Awake + OnEnable -> suscribe

            canal.Raise(new NpcReply("hola", "neutral", "idle"));
            Assert.AreEqual(1, b.RepliesRecibidos, "tras OnEnable el behaviour recibe los Raise del canal");

            b.DescablearParaPrueba(); // OnDisable -> desuscribe
            canal.Raise(new NpcReply("chau", "neutral", "idle"));
            Assert.AreEqual(1, b.RepliesRecibidos, "tras OnDisable ya no recibe: sin fugas");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(canal);
            Object.DestroyImmediate(config);
        }

        [Test]
        public void Cablear_sin_canal_asignado_no_lanza()
        {
            var go = new GameObject("m8-behaviour-sin-canal");
            var b = go.AddComponent<NpcPresenterBehaviour>();

            Assert.DoesNotThrow(() => b.CablearParaPrueba());
            Assert.DoesNotThrow(() => b.DescablearParaPrueba());

            Object.DestroyImmediate(go);
        }
    }
}
