using UnityEngine;

namespace NpcAi.VrInput
{
    /// <summary>
    /// Implementacion real de <see cref="ISpatialSampler"/> (espejo de
    /// <c>MicrophoneAudioCapture</c>, M1): lee la <c>Camera</c> del HMD, el <c>Transform</c>
    /// del objetivo y el pulso latcheado de <see cref="TouchZoneRelay"/> de la escena
    /// anfitriona y los empaqueta en un <see cref="SpatialSample"/> por tick.
    /// <para>
    /// AD10: sin camara u objetivo asignados, <see cref="LeerMuestra"/> devuelve
    /// <c>false</c> de forma completamente silenciosa -- sin <c>Debug.Log</c>, sin lanzar --
    /// y <c>SpatialPhysicalActionSource.Bombear()</c> simplemente no avanza ningun
    /// temporizador ese frame. No hay rig XR en ninguna escena todavia.
    /// </para>
    /// </summary>
    internal sealed class UnitySpatialSampler : ISpatialSampler
    {
        private readonly Camera _camaraDelHmd;
        private readonly Transform _objetivo;
        private readonly TouchZoneRelay _zonaDeContacto;

        public bool EstaActivo { get; private set; }

        public UnitySpatialSampler(Camera camaraDelHmd, Transform objetivo, TouchZoneRelay zonaDeContacto)
        {
            _camaraDelHmd = camaraDelHmd;
            _objetivo = objetivo;
            _zonaDeContacto = zonaDeContacto;
        }

        public void Iniciar() => EstaActivo = true;

        public bool LeerMuestra(out SpatialSample muestra)
        {
            if (_camaraDelHmd == null || _objetivo == null)
            {
                muestra = default;
                return false;
            }

            var cabeza = _camaraDelHmd.transform;
            muestra = new SpatialSample(
                posicionDeCabeza: AVec3(cabeza.position),
                frenteDeCabeza: AVec3(cabeza.forward),
                posicionDelObjetivo: AVec3(_objetivo.position),
                hayObjetivo: _camaraDelHmd != null && _objetivo != null,
                pulsoDeContacto: _zonaDeContacto != null && _zonaDeContacto.ConsumirPulso(),
                deltaSegundos: Time.deltaTime);
            return true;
        }

        public void Detener() => EstaActivo = false;

        private static Vec3 AVec3(Vector3 v) => new Vec3(v.x, v.y, v.z);
    }
}
