using System;
using System.Globalization;
using NpcAi.Core;
using NpcAi.Core.Channels;
using UnityEngine;

namespace NpcAi.Harness.Unity
{
    /// <summary>
    /// M11 -- cascara de escena del banco de pruebas: traduce canal -> argumento de metodo y
    /// valor de retorno -> canal. No construye nada (regla dura 3): recibe el
    /// <see cref="SessionDirector"/> ya armado y la costura de M13 (abrir y cerrar la sesion de
    /// bitacora) desde la raiz de composicion del anfitrion, via <see cref="Inyectar"/>. Se
    /// suscribe a <see cref="UtteranceChannel"/> y <see cref="PhysicalActionChannel"/> con
    /// simetria estricta <see cref="OnEnable"/>/<see cref="OnDisable"/> (el canal no deduplica)
    /// y solo hace <c>Raise</c> sobre <see cref="NpcReplyChannel"/> (AD9: suscribirse al propio
    /// canal de salida seria un lazo).
    /// <para>
    /// Orden de escucha (AD2): la cascara hace <c>Raise</c> de <see cref="NpcReplyChannel"/>
    /// DENTRO de su handler de <see cref="UtteranceChannel"/>, y M13 escucha ambos canales. Si la
    /// cascara quedara antes que M13 en el canal de utterance, cada turno se grabaria invertido
    /// (fila Npc antes que la fila Usuario). <c>[DefaultExecutionOrder(100)]</c> hace que su
    /// <see cref="OnEnable"/> corra despues de los componentes de orden por defecto y su oyente
    /// quede ultimo. Solo manda cuando los dos componentes se habilitan en el mismo pase de carga;
    /// el efecto no es observable en EditMode y lo cubre la compuerta humana de escena.
    /// </para>
    /// </summary>
    [DefaultExecutionOrder(100)]
    public sealed class HarnessBehaviour : MonoBehaviour
    {
        // internal (con [SerializeField]): Unity los serializa y muestra en el Inspector, y las
        // pruebas (ensamblado amigo, ver Properties/AssemblyInfo.cs) los cablean sin reflexion.
        [SerializeField] internal UtteranceChannel _canalDeUtterance;       // entrada: solo Subscribe
        [SerializeField] internal PhysicalActionChannel _canalDeAccion;     // entrada: solo Subscribe
        [SerializeField] internal NpcReplyChannel _canalDeRespuesta;        // salida: solo Raise (AD9)
        [SerializeField] internal string _prefijoDeEtiqueta = "banco";      // AD7
        [SerializeField] internal bool _arrancarSolo;                       // AD8: por defecto falso

        private SessionDirector _director;
        private Action<string> _abrirBitacora;
        private Action _cerrarBitacora;
        private Func<DateTime> _reloj = RelojDelSistema;                    // AD7: solo lo sustituye la costura de prueba

        /// <summary>
        /// Etiqueta de la sesion de bitacora que esta cascara abrio; vacia antes del primer
        /// <see cref="IniciarSesion"/> y despues de cerrarla con <see cref="FinalizarSesion"/>.
        /// </summary>
        public string EtiquetaActiva { get; private set; } = "";

        /// <summary>
        /// Diagnostico, solo para calibrar M2/M6/M15 durante el desarrollo (p. ej. desde una
        /// consola de presentacion): reenvia <see cref="SessionDirector.UltimoIntentClasificado"/>.
        /// No lo usa ninguna logica de enrutado de esta cascara. <see cref="IntentResult.Unknown"/>
        /// sin director inyectado.
        /// </summary>
        public IntentResult UltimoIntentClasificado =>
            _director != null ? _director.UltimoIntentClasificado : IntentResult.Unknown();

        /// <summary>
        /// Diagnostico, solo para calibrar M2/M6/M15 durante el desarrollo: reenvia
        /// <see cref="SessionDirector.UltimoTurnoFueClinico"/>. No lo usa ninguna logica de
        /// enrutado de esta cascara. <c>false</c> sin director inyectado.
        /// </summary>
        public bool UltimoTurnoFueClinico => _director != null && _director.UltimoTurnoFueClinico;

        /// <summary>
        /// Diagnostico: caso clinico asignado a la sesion en curso, para mostrarlo de forma
        /// permanente en una consola de presentacion. Reenvia
        /// <see cref="SessionDirector.CasoActual"/>; <see cref="ClinicalCaseId.None"/> sin director
        /// inyectado o antes del primer <see cref="IniciarSesion"/>.
        /// </summary>
        public ClinicalCaseId CasoActual => _director != null ? _director.CasoActual : ClinicalCaseId.None;

        /// <summary>
        /// Diagnostico: personalidad asignada a la sesion en curso. Reenvia
        /// <see cref="SessionDirector.PersonalidadActual"/>; <see cref="PersonalityId.None"/> sin
        /// director inyectado o antes del primer <see cref="IniciarSesion"/>.
        /// </summary>
        public PersonalityId PersonalidadActual => _director != null ? _director.PersonalidadActual : PersonalityId.None;

        /// <summary>
        /// Diagnostico: estado de receptividad (M4) del NPC ahora mismo. Reenvia
        /// <see cref="SessionDirector.ReceptividadActual"/>; <see cref="Receptivity.Neutral"/> (el
        /// estado que M4 debe reportar antes de su primer <c>Reset</c>) sin director inyectado.
        /// </summary>
        public Receptivity ReceptividadActual => _director != null ? _director.ReceptividadActual : Receptivity.Neutral;

        /// <summary>
        /// Entrega el director ya armado y la costura de M13 (AD4, AD5). Ruidoso aqui, en la
        /// raiz de composicion (barato y aislado); silencioso en los handlers de canal, porque
        /// una excepcion ahi abortaria a los demas oyentes de un canal compartido. Las dos
        /// costuras pueden ser <c>null</c> (sin M13). La suscripcion a los canales no depende de
        /// esta llamada: puede ocurrir antes o despues de <see cref="OnEnable"/>.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="director"/> es <c>null</c>.</exception>
        public void Inyectar(SessionDirector director, Action<string> abrirBitacora, Action cerrarBitacora)
        {
            _director = director ?? throw new ArgumentNullException(nameof(director));
            _abrirBitacora = abrirBitacora;
            _cerrarBitacora = cerrarBitacora;
        }

        /// <summary>
        /// Una operacion, cuatro efectos (AD6, AD7): cierra la sesion abierta si la hay, pide al
        /// director elegir y arrancar (<c>m4.Reset</c>, <c>m9.AssignCase</c>,
        /// <c>m15.AssignCase</c>) y abre la sesion de M13 con la etiqueta
        /// <c>{prefijo}-{caso}-{yyyyMMdd-HHmmss}</c>. Sin director inyectado es no-op. Publico,
        /// sin parametros y sin retorno para que un control de escena lo cablee desde el
        /// Inspector (AD8).
        /// </summary>
        public void IniciarSesion()
        {
            if (_director == null) return;

            FinalizarSesion(); // AD6: "inicio, cierre, inicio"; no-op si no hay sesion abierta

            _director.IniciarSesion(); // efectos 1-3: m4.Reset, m9.AssignCase, m15.AssignCase

            // AD7: el caso solo se conoce DESPUES del arranque, asi que el orden es fijo. Los
            // segundos en la marca de tiempo evitan que dos sesiones del mismo caso dentro del
            // mismo minuto compartan etiqueta (M13 reabre la fila de una etiqueta ya conocida).
            EtiquetaActiva = _prefijoDeEtiqueta + "-" + _director.CasoActual.Value + "-"
                             + _reloj().ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture);

            if (_abrirBitacora != null) _abrirBitacora(EtiquetaActiva); // efecto 4: M13, sin ver al par
        }

        /// <summary>
        /// Camino normal de cierre (AD6): pide a M13 cerrar la sesion que esta cascara abrio, de
        /// modo que <c>ReanudarUltimaSesion</c> no la retome despues. Sin sesion abierta es
        /// no-op. No toca M4, M9 ni M15: el director no tiene operacion de cierre.
        /// </summary>
        public void FinalizarSesion()
        {
            if (string.IsNullOrEmpty(EtiquetaActiva)) return;

            if (_cerrarBitacora != null) _cerrarBitacora();
            EtiquetaActiva = "";
        }

        /// <summary>
        /// Pass-through crudo al director (AD8): sin recortar, sin normalizar (eso es de M9),
        /// sin <c>Raise</c> y sin tocar la sesion de M13. Sin director inyectado es no-op.
        /// </summary>
        public void DeclararTriaje(string categoria)
        {
            if (_director == null) return;

            _director.DeclararTriaje(categoria);
        }

        /// <summary>
        /// Suscribe los DOS canales de entrada (AD4: no depende de la inyeccion del director, asi
        /// que puede correr antes o despues de <see cref="Inyectar"/>). Guarda <c>!= null</c> y
        /// nunca el operador de acceso condicional nulo sobre un <c>UnityEngine.Object</c>: el
        /// <c>==</c> de Unity reporta como nulo un objeto ya destruido, y ese operador se salta
        /// la sobrecarga. Nunca se suscribe a <see cref="_canalDeRespuesta"/> (AD9).
        /// </summary>
        private void OnEnable()
        {
            if (_canalDeUtterance != null) _canalDeUtterance.Subscribe(AlRecibirUtterance);
            if (_canalDeAccion != null) _canalDeAccion.Subscribe(AlRecibirAccion);
        }

        /// <summary>
        /// Desuscribe los dos canales de entrada: simetria estricta con <see cref="OnEnable"/>,
        /// porque el canal no deduplica. Aqui NO se cierra la bitacora (AD6): el registrador de
        /// M13 puede estar ya en <c>null</c>, y en el Editor este metodo corre en cada recarga de
        /// dominio.
        /// </summary>
        private void OnDisable()
        {
            if (_canalDeUtterance != null) _canalDeUtterance.Unsubscribe(AlRecibirUtterance);
            if (_canalDeAccion != null) _canalDeAccion.Unsubscribe(AlRecibirAccion);
        }

        /// <summary>
        /// Reenvia la <see cref="Utterance"/> al director sin alterarla y publica UN solo
        /// <c>Raise</c> con lo que devuelve <see cref="SessionDirector.ProcesarTurno"/>, sea por la
        /// via clinica o por la social. El <c>Raise</c> ocurre DENTRO del despacho del canal de
        /// utterance (AD2). Sin director inyectado retorna sin publicar (AD4: invariante "sin
        /// director, cero <c>Raise</c>"); un slot de respuesta sin asignar es un no-op, no una
        /// excepcion (AD9). "No lanza" cubre el codigo propio de la cascara, NO al director: una
        /// excepcion de <see cref="SessionDirector.ProcesarTurno"/> propaga a proposito (AD4).
        /// </summary>
        private void AlRecibirUtterance(Utterance utterance)
        {
            if (_director == null) return;

            var reply = _director.ProcesarTurno(utterance);
            if (_canalDeRespuesta != null) _canalDeRespuesta.Raise(reply);
        }

        /// <summary>
        /// Reenvia la <see cref="PhysicalAction"/> a <see cref="SessionDirector.ProcesarAccion"/>.
        /// Publica solo si el director devolvio valor: hoy siempre devuelve <c>null</c> (un gesto
        /// no habla), asi que la guarda es diseno y no es observable.
        /// </summary>
        private void AlRecibirAccion(PhysicalAction accion)
        {
            if (_director == null) return;

            var reply = _director.ProcesarAccion(accion);
            if (reply.HasValue && _canalDeRespuesta != null) _canalDeRespuesta.Raise(reply.Value);
        }

        /// <summary>
        /// Auto-arranque opcional (AD3, AD8). Nunca en <c>Awake</c> ni en <c>OnEnable</c>: ahi
        /// <c>SessionLogBehaviour</c> aun no armo su registrador y la costura de M13 seria un
        /// no-op silencioso (bitacora vacia entera). Unity garantiza que <c>Start</c> corre despues
        /// de todos los <c>Awake</c>/<c>OnEnable</c> del pase de carga, y despues de la inyeccion
        /// que hace la raiz de composicion en su propio <c>Awake</c>.
        /// </summary>
        private void Start()
        {
            if (_arrancarSolo) IniciarSesion();
        }

        /// <summary>
        /// Respaldo de cierre (AD6): corre ANTES de los <c>OnDisable</c> del apagado, asi que el
        /// registrador de M13 sigue vivo y el cierre llega a destino. Con frecuencia NO dispara en
        /// Quest/Android, ni si el proceso muere por crash o kill del SO: por eso el camino normal
        /// es <see cref="FinalizarSesion"/> cableado a un control de escena.
        /// </summary>
        private void OnApplicationQuit()
        {
            FinalizarSesion();
        }

        private static DateTime RelojDelSistema() => DateTime.Now;

        /// <summary>
        /// Fuerza <see cref="OnEnable"/> para pruebas EditMode (patron de M1, M7 y M8,
        /// <c>VrInputBehaviour.CablearParaPrueba</c>): <c>GameObject.SetActive(true)</c> sobre
        /// un objeto recien creado no dispara <c>Awake</c>/<c>OnEnable</c> de forma confiable
        /// dentro de un metodo de prueba sincrono. Asigna los campos directamente y NO pasa por
        /// <see cref="Inyectar"/> (que lanzaria con director nulo): sin argumentos deja la
        /// cascara habilitada y sin director, para probar el no-op de AD4. <paramref name="reloj"/>
        /// sustituye a <c>DateTime.Now</c> (AD7); si es <c>null</c> se usa el reloj del sistema.
        /// </summary>
        internal void CablearParaPrueba(
            SessionDirector director = null,
            Action<string> abrirBitacora = null,
            Action cerrarBitacora = null,
            Func<DateTime> reloj = null)
        {
            _director = director;
            _abrirBitacora = abrirBitacora;
            _cerrarBitacora = cerrarBitacora;
            _reloj = reloj ?? RelojDelSistema;
            OnEnable();
        }

        /// <summary>
        /// Fuerza <see cref="Start"/> para pruebas (AD3): en la escena corre DESPUES de la
        /// inyeccion y de todos los <c>OnEnable</c> del pase de carga, asi que es el unico punto
        /// donde se observa <c>_arrancarSolo</c>.
        /// </summary>
        internal void ArrancarParaPrueba() => Start();

        /// <summary>Fuerza <see cref="OnDisable"/> para pruebas (ver <see cref="CablearParaPrueba"/>).</summary>
        internal void DescablearParaPrueba() => OnDisable();

        /// <summary>Fuerza <see cref="OnApplicationQuit"/> para pruebas (AD6).</summary>
        internal void SalirParaPrueba() => OnApplicationQuit();
    }
}
