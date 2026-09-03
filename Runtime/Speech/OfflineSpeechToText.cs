using System;
using NpcAi.Core;
using NpcAi.Speech.Config;
using NpcAi.Speech.Segmentation;
using NpcAi.Speech.Threading;

namespace NpcAi.Speech
{
    /// <summary>
    /// Adaptador real de <see cref="ISpeechToText"/>. Nucleo: ventana de escucha, contador de
    /// generacion, guarda de emision tardia (Decision 5 de design.md) y clamps de
    /// <see cref="Utterance"/> (G14) — todo desde PR1, siempre disponible aunque el sujeto no
    /// este cableado a un motor real. El constructor sin parametros (usado por la gemela de
    /// contrato y las pruebas de guarda) deja las costuras <c>internal</c>
    /// (<see cref="EmitirParaPrueba"/>, <see cref="ProcesarResultadoDePrueba"/>) como el unico
    /// camino de emision, exactamente como en PR1/PR2. El constructor cableado (PR3, tasks.md
    /// 3.8) agrega segmentacion + motor + bomba: <see cref="StartListening"/> abre la
    /// estrategia de segmentacion y reinicia el motor; <see cref="AlimentarBloqueDeAudio"/> es
    /// la costura por la que entra cada bloque de muestras (produccion: el futuro
    /// <c>SpeechToTextBehaviour</c> de PR4 leyendo <c>IAudioCapture</c> en <c>Update()</c>;
    /// pruebas: llamada directa; ver Deviation en apply-progress); <see cref="StopListening"/>
    /// cierra el segmento abierto y emite DENTRO de si misma, antes de bajar la bandera
    /// (Decision 4 de design.md).
    /// </summary>
    public sealed class OfflineSpeechToText : ISpeechToText
    {
        private readonly SpeechSettings _config;
        private readonly IRecognitionEngine _motor;
        private readonly IMainThreadPump _bomba;

        private ISegmentationStrategy _estrategia;
        private double _segundosEnFrase;
        private int _generacion;

        public event Action<Utterance> OnUtterance;
        public bool IsListening { get; private set; }

        /// <summary>
        /// Construye un sujeto sin cablear: solo el nucleo de PR1 (ventana, generacion,
        /// guardas, clamps). Es lo que usan la gemela de contrato y las pruebas de guarda —
        /// <see cref="StartListening"/>/<see cref="StopListening"/> solo mueven estado.
        /// </summary>
        public OfflineSpeechToText()
        {
        }

        /// <summary>
        /// Construye un sujeto cableado (tasks.md 3.8) con la bomba por defecto
        /// (<see cref="ImmediateMainThreadPump"/>, sincronica — Decision 3 de design.md: "la
        /// bomba por defecto ... es la que usan las pruebas").
        /// </summary>
        internal OfflineSpeechToText(SpeechSettings config, IRecognitionEngine motor)
            : this(config, motor, new ImmediateMainThreadPump())
        {
        }

        /// <summary>
        /// Construye un sujeto cableado (tasks.md 3.8): segmentacion + motor + bomba.
        /// <c>internal</c> porque <see cref="IRecognitionEngine"/> y <see cref="SpeechSettings"/>
        /// son costuras del modulo (regla dura 3: ningun otro modulo las referencia); la
        /// escena anfitriona solo ve <see cref="ISpeechToText"/> a traves del futuro
        /// <c>SpeechToTextBehaviour</c> (PR4).
        /// </summary>
        internal OfflineSpeechToText(SpeechSettings config, IRecognitionEngine motor, IMainThreadPump bomba)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _motor  = motor  ?? throw new ArgumentNullException(nameof(motor));
            _bomba  = bomba  ?? throw new ArgumentNullException(nameof(bomba));
        }

        public void StartListening()
        {
            if (IsListening) return; // transicion no efectiva: no abre generacion nueva
            IsListening = true;
            _generacion++;
            AbrirSegmentacionSiHayMotor();
        }

        public void StopListening()
        {
            if (!IsListening) return; // transicion no efectiva
            CerrarSegmentoAbiertoYEmitir();
            IsListening = false;
            _generacion++;
            _estrategia = null;
        }

        /// <summary>
        /// Costura por la que entra cada bloque de muestras capturado (tasks.md 3.8, Decision
        /// 3 de design.md). No hace nada si el sujeto no esta cableado
        /// (<c>_motor == null</c>) o no esta escuchando. Segun la decision de la estrategia
        /// para este bloque: <c>Continuar</c> alimenta el motor; <c>CerrarFrase</c> finaliza,
        /// emite via <see cref="_bomba"/> y reabre la ventana (solo alcanzable en
        /// <see cref="TriggerStrategy.ActividadDeVoz"/>, ver design.md diagrama B);
        /// <c>Descartar</c> no hace nada (silencio antes de acumular voz minima).
        /// </summary>
        internal void AlimentarBloqueDeAudio(float[] muestras, int cantidad)
        {
            if (_motor == null || !IsListening || _estrategia == null) return;

            _segundosEnFrase += DuracionEnSegundos(cantidad);

            switch (_estrategia.Evaluar(muestras, cantidad, _segundosEnFrase))
            {
                case SegmentDecision.Continuar:
                    _motor.Alimentar(muestras, cantidad);
                    break;

                case SegmentDecision.CerrarFrase:
                    CerrarSegmentoPorSilencioYReabrir();
                    break;
            }
        }

        private void AbrirSegmentacionSiHayMotor()
        {
            if (_motor == null) return; // sujeto sin cablear (PR1/PR2): solo estado

            _estrategia = SegmentationFactory.Crear(
                _config.Estrategia,
                _config.UmbralDeEnergia,
                _config.MsMinimosDeVoz,
                _config.MsDeSilencioParaCortar,
                _config.MaxSegundosPorFrase);
            _estrategia.AbrirVentana();
            _motor.Reiniciar();
            _segundosEnFrase = 0d;
        }

        /// <summary>
        /// Decision 4 de design.md: cerrar segmento -> Finalizar() -> EmitirSiVigente(...) con
        /// <see cref="IsListening"/> AUN <c>true</c> -> recien despues de este metodo
        /// <see cref="StopListening"/> baja la bandera y sube la generacion. Es la unica
        /// ordenacion que respeta a la vez "la ventana ES la frase" y "el contrato prohibe
        /// emitir despues de StopListening".
        /// </summary>
        private void CerrarSegmentoAbiertoYEmitir()
        {
            if (_motor == null || _estrategia == null) return; // sujeto sin cablear

            _estrategia.CerrarVentana();
            var resultado = _motor.Finalizar();
            var utterance = ConstruirUtteranceAcotada(resultado.Texto, resultado.ConfianzaCruda, DuracionEnSegundos(resultado.MuestrasAlimentadas));
            EmitirSiVigente(_generacion, utterance);
        }

        /// <summary>
        /// Cierre automatico por silencio dentro de una ventana <see cref="TriggerStrategy.ActividadDeVoz"/>
        /// (design.md, diagrama B): a diferencia de <see cref="CerrarSegmentoAbiertoYEmitir"/>,
        /// la ventana de escucha sigue abierta, asi que la emision pasa por
        /// <see cref="_bomba"/> (no directo) y la segmentacion se reabre para la frase
        /// siguiente sin tocar generacion ni <see cref="IsListening"/>.
        /// </summary>
        private void CerrarSegmentoPorSilencioYReabrir()
        {
            var resultado = _motor.Finalizar();
            var utterance = ConstruirUtteranceAcotada(resultado.Texto, resultado.ConfianzaCruda, DuracionEnSegundos(resultado.MuestrasAlimentadas));
            var generacionDeLaFrase = _generacion;

            _bomba.Post(generacionDeLaFrase, utterance, (gen, frase) => EmitirSiVigente(gen, frase));

            _estrategia.AbrirVentana();
            _motor.Reiniciar();
            _segundosEnFrase = 0d;
        }

        private float DuracionEnSegundos(int cantidadDeMuestras)
            => _config != null && _config.TasaDeMuestreo > 0 ? (float)cantidadDeMuestras / _config.TasaDeMuestreo : 0f;

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
