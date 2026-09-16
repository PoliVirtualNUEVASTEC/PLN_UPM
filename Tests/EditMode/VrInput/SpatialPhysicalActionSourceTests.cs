using NpcAi.Core;
using NpcAi.Core.Tests;
using NpcAi.VrInput;

namespace NpcAi.VrInput.Tests
{
    /// <summary>
    /// Gemela del contrato para la implementacion real de M7 (PR1: solo el nucleo, sin
    /// muestreador cableado). Debe pasar exactamente las mismas pruebas que
    /// <see cref="ScriptedPhysicalActionSourceTests"/>: es el mecanismo que impide que la
    /// implementacion real mienta sobre <see cref="IPhysicalActionSource"/>.
    /// </summary>
    public class SpatialPhysicalActionSourceTests : PhysicalActionSourceContract
    {
        protected override IPhysicalActionSource CreateSubject() => new SpatialPhysicalActionSource();

        protected override bool EmitTestAction(IPhysicalActionSource subject, PhysicalAction action)
            => ((SpatialPhysicalActionSource)subject).EmitirParaPrueba(action);
    }
}
