using System.Collections.Generic;
using NpcAi.Presentation.Config;
using UnityEngine;

namespace NpcAi.Presentation
{
    /// <summary>
    /// <see cref="IAnimationDriver"/> real: traduce <c>EmotionTag</c> y <c>AnimationCue</c> a
    /// parámetros de un <c>Animator</c> usando el mapa de <see cref="PresentationSettings"/>.
    /// Un tag/cue fuera del mapa se ignora; sin <c>Animator</c> o sin controller, todo es no-op.
    /// El <c>Animator</c> y su <c>AnimatorController</c> son del proyecto anfitrión.
    /// </summary>
    internal sealed class AnimatorDriver : IAnimationDriver
    {
        private readonly Animator _animator;
        private readonly IReadOnlyDictionary<string, ParametroDeAnimacion> _mapa;

        public AnimatorDriver(Animator animator, IReadOnlyDictionary<string, ParametroDeAnimacion> mapa)
        {
            _animator = animator;
            _mapa     = mapa ?? new Dictionary<string, ParametroDeAnimacion>();
        }

        public void Aplicar(string emotionTag, string animationCue)
        {
            AplicarUno(emotionTag);
            AplicarUno(animationCue);
        }

        private void AplicarUno(string clave)
        {
            if (_animator == null || _animator.runtimeAnimatorController == null || string.IsNullOrEmpty(clave))
                return;

            if (!_mapa.TryGetValue(clave, out var p) || string.IsNullOrEmpty(p.Nombre))
                return;

            switch (p.Tipo)
            {
                case TipoDeParametroDeAnimacion.Trigger:
                    _animator.SetTrigger(p.Nombre);
                    break;
                case TipoDeParametroDeAnimacion.Bool:
                    _animator.SetBool(p.Nombre, true);
                    break;
            }
        }
    }
}
