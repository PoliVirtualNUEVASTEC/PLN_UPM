using UnityEngine;

namespace NpcAi.VrInput
{
    /// <summary>
    /// <see cref="MonoBehaviour"/> colocado sobre el collider del paciente en la escena
    /// anfitriona (M11). AD7: el pulso de contacto se latchea aqui, en
    /// <see cref="OnTriggerEnter"/> (push), y <see cref="UnitySpatialSampler"/> lo jala con
    /// <see cref="ConsumirPulso"/> (pull: lee y limpia). Esta direccion unica evita que el
    /// relay conozca a <see cref="VrInputBehaviour"/> y garantiza a lo sumo un pulso por
    /// tick aunque entren varios colliders en el mismo frame.
    /// </summary>
    public sealed class TouchZoneRelay : MonoBehaviour
    {
        [Tooltip("Capas que cuentan como contacto valido con el paciente. Definidas por el proyecto anfitrion (M11).")]
        [SerializeField]
        private LayerMask _capasDeContacto;

        private bool _pendiente;

        private void OnTriggerEnter(Collider otro)
        {
            if (((1 << otro.gameObject.layer) & _capasDeContacto.value) == 0) return;
            _pendiente = true;
        }

        /// <summary>Lee y limpia el latch (AD7): a lo sumo un pulso por lectura.</summary>
        internal bool ConsumirPulso()
        {
            var pulso = _pendiente;
            _pendiente = false;
            return pulso;
        }
    }
}
