using System.Collections.Generic;

namespace NpcAi.VrInput.Fakes
{
    /// <summary>
    /// Doble determinista de <see cref="ISpatialSampler"/>: la prueba guiona una trayectoria
    /// como una cola de <see cref="SpatialSample"/> y decide el delta de cada una (AD3).
    /// <see cref="LeerMuestra"/> desencola en el mismo orden en que se encolo.
    /// </summary>
    internal sealed class ScriptedSpatialSampler : ISpatialSampler
    {
        private readonly Queue<SpatialSample> _muestras = new Queue<SpatialSample>();

        public bool EstaActivo { get; private set; }

        /// <summary>Agrega una muestra al final de la trayectoria guionada.</summary>
        public void Encolar(SpatialSample muestra) => _muestras.Enqueue(muestra);

        public void Iniciar() => EstaActivo = true;

        public bool LeerMuestra(out SpatialSample muestra)
        {
            if (_muestras.Count == 0)
            {
                muestra = default;
                return false;
            }

            muestra = _muestras.Dequeue();
            return true;
        }

        public void Detener() => EstaActivo = false;
    }
}
