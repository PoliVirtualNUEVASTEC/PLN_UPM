using UnityEngine;

namespace NpcAi.VrInput
{
    /// <summary>
    /// <c>ScriptableObject</c> de configuracion por escenario (tasks.md 2.3; tabla
    /// "Configuracion" de design.md). Un asset por escenario bajo <c>Data/VrInput/</c>, nunca un
    /// singleton global (regla dura 7: lo variable es dato, no codigo). <see cref="OnValidate"/>
    /// acota los campos numericos al rango de la tabla apenas se editan en el Inspector o se
    /// llama a mano en pruebas, incluidos los dos rangos cruzados que evitan una banda de
    /// histeresis invertida o de ancho cero (design.md, tabla "Configuracion").
    /// </summary>
    [CreateAssetMenu(fileName = "VrInputSettings", menuName = "NpcAi/VrInput/Vr Input Settings", order = 0)]
    public sealed class VrInputSettingsAsset : ScriptableObject
    {
        public float GradosDelConoDeMirada = 20f;
        public float GradosDeLiberacionDeMirada = 30f;
        public float SegundosDePermanenciaDeMirada = 0.6f;
        public float MetrosParaAcercarse = 1.2f;
        public float MetrosParaAlejarse = 2.0f;
        public float SegundosDeEnfriamientoDeContacto = 1.0f;

        /// <summary>
        /// Acota los campos numericos al rango de la tabla "Configuracion" de design.md.
        /// <c>internal</c> (no <c>private</c>): Unity la invoca por reflexion sin importar la
        /// accesibilidad, y <c>VrInputSettingsAssetTests</c> (ensamblado amigo) la llama
        /// directamente para probarla sin pasar por el Inspector. El orden importa: el cono de
        /// mirada y la distancia de acercarse se acotan primero porque los dos <c>Max</c>
        /// cruzados dependen de su valor ya acotado.
        /// </summary>
        internal void OnValidate()
        {
            GradosDelConoDeMirada = Mathf.Clamp(GradosDelConoDeMirada, 1f, 90f);
            GradosDeLiberacionDeMirada = Mathf.Clamp(GradosDeLiberacionDeMirada, 1f, 120f);
            GradosDeLiberacionDeMirada = Mathf.Max(GradosDeLiberacionDeMirada, GradosDelConoDeMirada);

            SegundosDePermanenciaDeMirada = Mathf.Clamp(SegundosDePermanenciaDeMirada, 0f, 5f);

            MetrosParaAcercarse = Mathf.Clamp(MetrosParaAcercarse, 0.1f, 10f);
            MetrosParaAlejarse = Mathf.Clamp(MetrosParaAlejarse, 0.2f, 20f);
            MetrosParaAlejarse = Mathf.Max(MetrosParaAlejarse, MetrosParaAcercarse + 0.1f);

            SegundosDeEnfriamientoDeContacto = Mathf.Clamp(SegundosDeEnfriamientoDeContacto, 0f, 10f);
        }

        /// <summary>
        /// Snapshot POCO que consume el nucleo (<see cref="SpatialPhysicalActionSource"/>).
        /// </summary>
        internal VrInputSettings ToSettings() => new VrInputSettings
        {
            GradosDelConoDeMirada = GradosDelConoDeMirada,
            GradosDeLiberacionDeMirada = GradosDeLiberacionDeMirada,
            SegundosDePermanenciaDeMirada = SegundosDePermanenciaDeMirada,
            MetrosParaAcercarse = MetrosParaAcercarse,
            MetrosParaAlejarse = MetrosParaAlejarse,
            SegundosDeEnfriamientoDeContacto = SegundosDeEnfriamientoDeContacto
        };
    }
}
