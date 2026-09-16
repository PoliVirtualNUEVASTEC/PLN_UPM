namespace NpcAi.VrInput
{
    /// <summary>
    /// Snapshot POCO de umbrales que consume el nucleo (AD1/AD6): nunca lee <c>UnityEngine</c>.
    /// Trasladada aqui en la Fase 2 (tasks.md 2.2) desde
    /// <c>Runtime/VrInput/SpatialPhysicalActionSource.cs</c>, donde la Fase 1 la definio
    /// temporalmente para poder compilar contra la firma exacta de <c>design.md</c> antes de que
    /// existiera <c>Runtime/VrInput/Config/</c> (ver Deviations de <c>apply-progress.md</c>, PR1).
    /// Mismo namespace <c>NpcAi.VrInput</c> que el resto del modulo (no
    /// <c>NpcAi.VrInput.Config</c>): asi <see cref="SpatialPhysicalActionSource"/> y, en la Fase 3,
    /// <c>VrInputBehaviour</c> la consumen sin un <c>using</c> adicional.
    /// <see cref="VrInputSettingsAsset.ToSettings"/> es la unica fabrica de instancias reales; en
    /// pruebas se construye directamente con inicializadores de objeto.
    /// </summary>
    internal sealed class VrInputSettings
    {
        public float GradosDelConoDeMirada { get; set; } = 20f;
        public float GradosDeLiberacionDeMirada { get; set; } = 30f;
        public float SegundosDePermanenciaDeMirada { get; set; } = 0.6f;
        public float MetrosParaAcercarse { get; set; } = 1.2f;
        public float MetrosParaAlejarse { get; set; } = 2.0f;
        public float SegundosDeEnfriamientoDeContacto { get; set; } = 1.0f;
    }
}
