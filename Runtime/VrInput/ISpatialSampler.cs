namespace NpcAi.VrInput
{
    /// <summary>
    /// Costura interna de hardware de M7 (AD2/AD3) — espejo de <c>IAudioCapture</c> (M1). La
    /// implementacion real (<c>UnitySpatialSampler</c>, Fase 3) lee Camera/Transform/Time/
    /// Collider; el doble de prueba (<see cref="Fakes.ScriptedSpatialSampler"/>) guiona una
    /// trayectoria sintetica. Nunca un puerto de <c>NpcAi.Core</c>: es interna al modulo
    /// (regla dura 3).
    /// </summary>
    internal interface ISpatialSampler
    {
        bool EstaActivo { get; }

        void Iniciar();

        /// <summary>Una muestra por tick. <c>false</c> = nada que muestrear (sin camara, sin objetivo).</summary>
        bool LeerMuestra(out SpatialSample muestra);

        void Detener();
    }
}
