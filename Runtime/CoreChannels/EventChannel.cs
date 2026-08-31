using System;
using UnityEngine;

namespace NpcAi.Core.Channels
{
    /// <summary>
    /// Canal de evento como asset. Un modulo publica en el canal y otro se suscribe
    /// desde el Inspector, sin que ninguno de los dos conozca el tipo del otro.
    /// Es el unico mecanismo de conexion permitido entre modulos.
    /// </summary>
    public abstract class EventChannel<T> : ScriptableObject
    {
        private Action<T> _listeners;

        public void Raise(T payload) => _listeners?.Invoke(payload);

        public void Subscribe(Action<T> listener)
        {
            if (listener != null) _listeners += listener;
        }

        public void Unsubscribe(Action<T> listener)
        {
            if (listener != null) _listeners -= listener;
        }

        /// <summary>Los assets sobreviven a los cambios de escena; limpiar evita fugas.</summary>
        private void OnDisable() => _listeners = null;
    }
}
