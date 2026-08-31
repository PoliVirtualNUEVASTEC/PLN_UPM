using System;
using NpcAi.Core;

namespace NpcAi.VrInput.Fakes
{
    /// <summary>
    /// Doble determinista de M7. No necesita headset ni controles: la prueba o el
    /// banco de pruebas dispara las acciones a mano.
    /// </summary>
    public sealed class ScriptedPhysicalActionSource : IPhysicalActionSource
    {
        public event Action<PhysicalAction> OnAction;

        /// <summary><see cref="PhysicalAction.Ninguna"/> no se emite: no es un evento.</summary>
        public bool Emit(PhysicalAction action)
        {
            if (action == PhysicalAction.Ninguna) return false;
            OnAction?.Invoke(action);
            return true;
        }
    }
}
