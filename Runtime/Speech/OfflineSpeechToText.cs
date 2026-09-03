using System;
using NpcAi.Core;

namespace NpcAi.Speech
{
    /// <summary>
    /// Adaptador real de <see cref="ISpeechToText"/>. Este corte (PR1) solo trae el
    /// nucleo: ventana de escucha, contador de generacion, guarda de emision tardia
    /// (Decision 5 de design.md) y clamps de <see cref="Utterance"/> (G14). La captura de
    /// microfono, las estrategias de segmentacion y el motor real se cablean en
    /// PR2/PR3/PR4; hasta entonces <see cref="StartListening"/>/<see cref="StopListening"/>
    /// solo mueven estado, y las costuras <c>internal</c> ejercitan el mismo camino de
    /// emision que usara el motor real (requisito "Costura de prueba determinista sin
    /// microfono ni modelo").
    /// </summary>
    public sealed class OfflineSpeechToText : ISpeechToText
    {
        private int _generacion;

        public event Action<Utterance> OnUtterance;
        public bool IsListening { get; private set; }

        public void StartListening()
        {
            if (IsListening) return; // transicion no efectiva: no abre generacion nueva
            IsListening = true;
            _generacion++;
        }

        public void StopListening()
        {
            if (!IsListening) return; // transicion no efectiva
            IsListening = false;
            _generacion++;
        }

        /// <summary>
        /// Generacion vigente de la ventana de escucha actual (o de la ultima cerrada).
        /// Permite a la prueba capturar una generacion vieja y reintentarla.
        /// </summary>
        internal int GeneracionActual => _generacion;

        /// <summary>
        /// Espejo de <c>Emit()</c> del doble scripted: entra despues de los clamps, con la
        /// generacion vigente. La gemela de contrato la usa como <c>EmitTestUtterance</c>.
        /// </summary>
        internal bool EmitirParaPrueba(Utterance utterance) => EmitirSiVigente(_generacion, utterance);

        /// <summary>
        /// Entra en el mismo punto que entraria el callback del motor real: acota
        /// <see cref="Utterance.Confidence"/> a <c>[0,1]</c> y <see cref="Utterance.DurationSeconds"/>
        /// a <c>&gt;=0</c> (G14, "Paso de confianza sin filtrar, con rangos acotados"), y
        /// luego revalida generacion + <see cref="IsListening"/> antes de emitir.
        /// </summary>
        internal bool ProcesarResultadoDePrueba(string texto, float confianza, float duracionSegundos, int generacion)
        {
            var utterance = ConstruirUtteranceAcotada(texto, confianza, duracionSegundos);
            return EmitirSiVigente(generacion, utterance);
        }

        private static Utterance ConstruirUtteranceAcotada(string texto, float confianza, float duracionSegundos)
        {
            var confianzaAcotada = Clamp01(confianza);
            var duracionAcotada  = duracionSegundos < 0f ? 0f : duracionSegundos;
            return new Utterance(texto, confianzaAcotada, duracionAcotada);
        }

        private static float Clamp01(float valor)
        {
            if (valor < 0f) return 0f;
            if (valor > 1f) return 1f;
            return valor;
        }

        /// <summary>
        /// Guarda de emision tardia (Decision 5 de design.md): un resultado solo se
        /// emite si su generacion coincide con la vigente Y el sujeto sigue escuchando.
        /// Revalidar solo <see cref="IsListening"/> no basta: un resultado de la ventana N
        /// que llega durante la ventana N+1 encontraria <c>IsListening == true</c> y se
        /// emitiria como si fuera de la frase actual.
        /// </summary>
        private bool EmitirSiVigente(int generacion, Utterance frase)
        {
            if (generacion != _generacion) return false; // resultado de una ventana anterior
            if (!IsListening)               return false; // ya se detuvo
            OnUtterance?.Invoke(frase);
            return true;
        }
    }
}
