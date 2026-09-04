namespace NpcAi.Speech.Segmentation
{
    /// <summary>
    /// Selecciona la estrategia de disparo por dato, no por codigo (requisito "Estrategia
    /// de disparo configurable por escenario"). design.md ubica este enum junto al futuro
    /// snapshot <c>Runtime/Speech/Config/SpeechSettings.cs</c> (PR3); vive aqui en PR2
    /// porque <see cref="SegmentationFactory"/> lo necesita ya y PR2 no puede depender de
    /// <c>Data/Speech/</c> ni de un ScriptableObject (fuera de su alcance, ver tasks.md
    /// Phase 3). PR3 debe reutilizar este mismo tipo desde <c>SpeechSettings.Estrategia</c>
    /// en lugar de declarar uno nuevo con el mismo nombre.
    /// <c>public</c> (no <c>internal</c>): lo necesita <c>SegmentationFactoryTests</c>, una
    /// clase de prueba publica, como tipo de parametro (CS0051 no permite que un metodo
    /// publico exponga un tipo menos accesible, ni siquiera con <c>InternalsVisibleTo</c>);
    /// ademas PR3 lo va a exponer en el Inspector desde <c>SpeechSettingsAsset</c>.
    /// </summary>
    public enum TriggerStrategy
    {
        PulsarParaHablar,
        ActividadDeVoz
    }

    /// <summary>Que hacer con el bloque de muestras evaluado (design.md, Decision 2).</summary>
    internal enum SegmentDecision
    {
        Continuar,
        CerrarFrase,
        Descartar
    }

    /// <summary>
    /// Decide donde termina la frase dentro de la ventana de escucha. Vive en M1, no en el
    /// motor de reconocimiento (Decision 2 de design.md): un futuro motor sin endpointer
    /// propio no rompe esta costura, y la logica de corte queda probable en EditMode sin
    /// modelo ni nativo.
    /// </summary>
    internal interface ISegmentationStrategy
    {
        /// <summary>Reinicia el estado interno al abrir una frase nueva.</summary>
        void AbrirVentana();

        /// <summary>
        /// Evalua un bloque de muestras. <paramref name="segundosEnFrase"/> es el tiempo
        /// acumulado total dentro de la ventana actual, no la duracion de este bloque.
        /// </summary>
        SegmentDecision Evaluar(float[] muestras, int cantidad, double segundosEnFrase);

        /// <summary>Cierra la frase actual; se llama tras un <see cref="SegmentDecision.CerrarFrase"/>.</summary>
        void CerrarVentana();
    }
}
