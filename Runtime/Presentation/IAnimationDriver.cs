namespace NpcAi.Presentation
{
    /// <summary>
    /// Costura INTERNA de M8: traduce los cues de un <c>NpcReply</c>
    /// (<c>EmotionTag</c>, <c>AnimationCue</c>) a parametros de un <c>Animator</c>.
    /// Nunca un puerto de <c>NpcAi.Core</c> (regla dura 3). La implementacion real
    /// (<c>AnimatorDriver</c> sobre un <c>Animator</c> inyectado por la escena) llega
    /// en el PR2; el mapa cue -> parametro es dato de la config, no codigo.
    /// </summary>
    internal interface IAnimationDriver
    {
        /// <summary>
        /// Un <paramref name="emotionTag"/> o <paramref name="animationCue"/> fuera del mapa
        /// se ignora sin lanzar. Sin <c>Animator</c>, la implementacion real es no-op.
        /// Acepta <c>null</c> en cualquiera de los dos.
        /// </summary>
        void Aplicar(string emotionTag, string animationCue);
    }
}
