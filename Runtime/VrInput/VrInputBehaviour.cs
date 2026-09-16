using NpcAi.Core;
using NpcAi.Core.Channels;
using UnityEngine;

namespace NpcAi.VrInput
{
    /// <summary>
    /// Raiz de composicion <see cref="MonoBehaviour"/> de M7 (envoltura, Fase 3): en
    /// <see cref="Awake"/> arma el nucleo <see cref="SpatialPhysicalActionSource"/> con un
    /// <see cref="ISpatialSampler"/> real (<see cref="UnitySpatialSampler"/>); en
    /// <see cref="OnEnable"/>/<see cref="OnDisable"/> se suscribe y desuscribe del
    /// <c>OnAction</c> del nucleo interno (AD9 -- NUNCA de <see cref="PhysicalActionChannel"/>,
    /// que solo recibe <c>Raise</c>: suscribirse a su propio canal de salida seria un lazo);
    /// en <see cref="Update"/> bombea el nucleo cada frame sin bomba de hilo principal (AD6:
    /// M7 no tiene ningun hilo de fondo, todo corre sincronico). El rig XR y su cableado son
    /// del proyecto anfitrion (M11): este componente solo los maneja.
    /// </summary>
    public sealed class VrInputBehaviour : MonoBehaviour
    {
        // internal (con [SerializeField]): Unity los serializa y muestra en el Inspector, y las
        // pruebas (ensamblado amigo) los cablean sin reflexion.
        [SerializeField] internal VrInputSettingsAsset _configuracion;
        [SerializeField] internal PhysicalActionChannel _canal;

        [SerializeField] private Camera _camaraDelHmd;
        [SerializeField] private Transform _objetivo;

        [SerializeField] internal TouchZoneRelay _zonaDeContacto;

        private SpatialPhysicalActionSource _sujeto;
        private ISpatialSampler _muestreador;
        private ISpatialSampler _muestreadorDePrueba;

        private void Awake()
        {
            var cfg = _configuracion != null ? _configuracion.ToSettings() : new VrInputSettings();
            _muestreador = _muestreadorDePrueba
                           ?? new UnitySpatialSampler(_camaraDelHmd, _objetivo, _zonaDeContacto);
            _sujeto = new SpatialPhysicalActionSource(cfg, _muestreador);
        }

        private void OnEnable()
        {
            _sujeto.OnAction += PublicarEnCanal; // AD9: el nucleo interno, nunca el canal
            _muestreador.Iniciar();
        }

        private void OnDisable()
        {
            _sujeto.OnAction -= PublicarEnCanal;
            _muestreador.Detener();
        }

        private void Update() => _sujeto?.Bombear(); // AD6: sin bomba de hilo principal

        private void PublicarEnCanal(PhysicalAction a)
        {
            if (_canal != null) _canal.Raise(a);
        }

        /// <summary>
        /// Fuerza <see cref="Awake"/> + <see cref="OnEnable"/> para pruebas EditMode (patron de
        /// M8, <c>NpcPresenterBehaviour.CablearParaPrueba</c>):
        /// <c>GameObject.SetActive(true)</c> sobre un objeto recien creado no dispara estos
        /// callbacks de forma confiable dentro de un metodo de prueba sincrono. Con un
        /// <paramref name="muestreador"/> guionado (<c>ScriptedSpatialSampler</c>) la prueba
        /// evita tocar <c>Camera</c>/<c>Transform</c> reales; sin argumento, se arma el
        /// <see cref="UnitySpatialSampler"/> real con los campos serializados (nulos si no se
        /// asignaron) para probar el no-op silencioso de AD10.
        /// </summary>
        internal void CablearParaPrueba(ISpatialSampler muestreador = null)
        {
            _muestreadorDePrueba = muestreador;
            Awake();
            OnEnable();
        }

        /// <summary>Fuerza <see cref="OnDisable"/> para pruebas (ver <see cref="CablearParaPrueba"/>).</summary>
        internal void DescablearParaPrueba() => OnDisable();

        /// <summary>Fuerza <see cref="Update"/> para pruebas (ver <see cref="CablearParaPrueba"/>).</summary>
        internal void BombearParaPrueba() => Update();
    }
}
