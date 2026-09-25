using System.Text;
using NpcAi.Core.Channels;
using NpcAi.Harness.Unity;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Core = NpcAi.Core;

namespace NpcAi.Samples.Harness
{
    /// <summary>
    /// Consola de transcripcion en pantalla, de presentacion: un encabezado fijo (siempre visible,
    /// nunca se desplaza) con el caso clinico, la personalidad y la receptividad actuales, y debajo
    /// un historial con scroll de los turnos hablados y los avisos de los controles de escena
    /// (inicio de sesion, empezar/dejar de hablar, fin de sesion), para demostrar el pipeline sin
    /// tener que revisar la bitacora de M13. Construye su propia UI en <see cref="Awake"/> (Canvas,
    /// panel, ScrollRect con scrollbar vertical), igual que <c>ConsolaDeVoz.cs</c>: no hace falta
    /// armar nada a mano en el editor, solo cablear los tres slots del Inspector. Se suscribe a los
    /// mismos canales que ya usan HarnessBehaviour (M11) y SessionLogBehaviour (M13); un canal
    /// admite varios suscriptores a la vez, asi que no interfiere con ninguno de los dos. Vive en
    /// Samples~/, no en Runtime/Harness/: es puramente de demostracion, no participa en la logica
    /// del banco de pruebas.
    /// <para>
    /// Los avisos de "empezar a hablar" / "dejar de hablar" / "finalizar sesion" no tienen un
    /// canal propio: se disparan agregando una SEGUNDA entrada en el On Click() de cada boton
    /// de escena, ademas de la que ya llama al metodo real (StartListening/StopListening/
    /// FinalizarSesion). El aviso de "iniciar sesion" lee <see cref="HarnessBehaviour.EtiquetaActiva"/>
    /// DESPUES de que la llamada real ya corrio, asi que en ese boton la entrada de este script
    /// debe ir SEGUNDA en la lista (despues de IniciarSesion()).
    /// </para>
    /// </summary>
    public sealed class ConsolaDeTranscripcion : MonoBehaviour
    {
        private const string SinSesion = "(sin sesion)";

        // Umbral para decidir si el usuario "ya estaba mirando el final": si el scroll esta a
        // menos de este margen del final, una linea nueva lo vuelve a llevar al final; si el
        // usuario se subio a mirar el historial, una linea nueva NO le mueve la vista (para poder
        // leer los primeros mensajes sin que la consola lo empuje de vuelta abajo a cada turno).
        private const float UmbralAutoScroll = 0.02f;

        [SerializeField] private UtteranceChannel _canalDeUtterance;
        [SerializeField] private NpcReplyChannel _canalDeRespuesta;
        [SerializeField] private HarnessBehaviour _arnes;
        [SerializeField] private int _maximoDeLineas = 500; // limite de seguridad, no una ventana chica: con scroll se puede ver desde el primer turno

        private readonly StringBuilder _lineas = new StringBuilder();
        private int _cantidadDeLineas;

        private ScrollRect _scroll;
        private TextMeshProUGUI _encabezado;
        private TextMeshProUGUI _cuerpo;

        // Ultimos valores pintados en el encabezado: Update solo re-pinta cuando alguno cambia.
        private bool _encabezadoPintado;
        private Core.ClinicalCaseId _casoMostrado;
        private Core.PersonalityId _personalidadMostrada;
        private Core.Receptivity _receptividadMostrada;

        private void Awake() => ConstruirUi();

        private void OnEnable()
        {
            if (_canalDeUtterance != null) _canalDeUtterance.Subscribe(OnUtterance);
            if (_canalDeRespuesta != null) _canalDeRespuesta.Subscribe(OnNpcReply);
        }

        private void OnDisable()
        {
            if (_canalDeUtterance != null) _canalDeUtterance.Unsubscribe(OnUtterance);
            if (_canalDeRespuesta != null) _canalDeRespuesta.Unsubscribe(OnNpcReply);
        }

        /// <summary>
        /// El encabezado se re-pinta aqui y no solo al llegar una linea: la receptividad de M4
        /// tambien cambia por acciones fisicas, que no producen respuesta ni pasan por estos
        /// canales, y el caso/personalidad cambian al iniciar sesion. Sin asignaciones cuando
        /// nada cambio (compara contra lo ultimo pintado).
        /// </summary>
        private void Update()
        {
            if (_arnes == null) return;

            if (_encabezadoPintado
                && _arnes.CasoActual == _casoMostrado
                && _arnes.PersonalidadActual == _personalidadMostrada
                && _arnes.ReceptividadActual == _receptividadMostrada)
                return;

            RefrescarEncabezado();
        }

        // Utterance recibida pero todavia no impresa: se imprime junto con la clasificacion de
        // M2 recien en OnNpcReply, porque HarnessBehaviour.UltimoIntentClasificado no queda
        // actualizado hasta que SessionDirector.ProcesarTurno termina -- y eso ocurre DESPUES de
        // este OnUtterance (HarnessBehaviour tiene [DefaultExecutionOrder(100)], asi que su
        // OnEnable, y por lo tanto su Subscribe, corre despues del de esta consola; los
        // suscriptores de un EventChannel se llaman en orden de suscripcion). Todo el turno es
        // sincrono (un solo hilo, una sola llamada anidada: Raise(utterance) -> ProcesarTurno ->
        // Raise(reply)), asi que no hay riesgo de mezclar el pendiente de un turno con el de otro.
        private Core.Utterance _utterancePendiente;
        private bool _hayUtterancePendiente;

        private void OnUtterance(Core.Utterance u)
        {
            if (u.IsEmpty) return;
            _utterancePendiente = u;
            _hayUtterancePendiente = true;
        }

        private void OnNpcReply(Core.NpcReply r)
        {
            if (_hayUtterancePendiente)
            {
                AgregarLinea("Enfermero: " + _utterancePendiente.Text);
                AgregarLineaDeClasificacion();
                _hayUtterancePendiente = false;
            }

            if (!r.IsEmpty) AgregarLinea("Paciente: " + r.Text);
            AgregarLineaDeOrigenYEstado(r);
        }

        /// <summary>
        /// Linea de diagnostico de M2, pegada a lo que dijo el enfermero (no al paciente): que
        /// clasifico M2 en ESE turno (Intent/Tono/Confianza), mas la confianza de transcripcion
        /// de M1 (<see cref="Core.Utterance.Confidence"/>) para el mismo turno -- son dos
        /// numeros distintos, ambos llamados "Confianza" en el contrato, y facil confundirlos:
        /// el de M1 mide que tan segura esta la transcripcion del audio (con
        /// <c>DebugForzarUtterance</c> siempre 1, porque no hay audio de por medio); el de M2 mide
        /// que tan segura esta la clasificacion del BERT de M2 sobre la intencion (puede ser bajo
        /// aunque la transcripcion sea perfecta, si la frase es ambigua para el modelo).
        /// </summary>
        private void AgregarLineaDeClasificacion()
        {
            if (_arnes == null) return;

            var intent = _arnes.UltimoIntentClasificado;
            AgregarLinea(string.Format(
                "   M2 -> Intent: {0} | Tono: {1} | Confianza-M2: {2:0.00} | Confianza-transcripcion(M1): {3:0.00}",
                intent.Intent, intent.Tone, intent.Confidence, _utterancePendiente.Confidence));
        }

        /// <summary>
        /// Linea de diagnostico pegada a la respuesta del paciente: de donde vino (M15 clinico,
        /// anclado al caso asignado, o M6 generico de respaldo) y como quedo el NPC tras el turno
        /// (receptividad de M4 y emocion de la respuesta). No participa en el pipeline real: lee
        /// propiedades de solo lectura de <see cref="HarnessBehaviour"/>. Sin <see cref="_arnes"/>
        /// asignado, no agrega nada.
        /// </summary>
        private void AgregarLineaDeOrigenYEstado(Core.NpcReply respuesta)
        {
            if (_arnes == null) return;

            var origen = _arnes.UltimoTurnoFueClinico ? "M15 clinico" : "M6 generico";
            AgregarLinea(string.Format(
                "   [{0}] Receptividad: {1} | Emocion: {2}",
                origen, _arnes.ReceptividadActual, respuesta.EmotionTag));
        }

        /// <summary>
        /// Cablear como segunda entrada del On Click() del boton "Iniciar sesion", DESPUES de
        /// la llamada real a HarnessBehaviour.IniciarSesion(): a esta altura la etiqueta ya
        /// incluye el caso elegido (formato {prefijo}-{caso}-{marca de tiempo}).
        /// </summary>
        public void NotificarInicioSesion()
        {
            var etiqueta = _arnes != null ? _arnes.EtiquetaActiva : "";
            AgregarLinea(string.IsNullOrEmpty(etiqueta)
                ? "--- Sesion iniciada ---"
                : "--- Sesion iniciada: " + etiqueta + " ---");
        }

        /// <summary>Cablear como segunda entrada del On Click() del boton "Empezar a hablar".</summary>
        public void NotificarEmpezarAHablar() => AgregarLinea("--- Escuchando... ---");

        /// <summary>Cablear como segunda entrada del On Click() del boton "Dejar de hablar".</summary>
        public void NotificarDejarDeHablar() => AgregarLinea("--- Fin de la escucha ---");

        /// <summary>Cablear como segunda entrada del On Click() del boton "Finalizar sesion".</summary>
        public void NotificarFinSesion() => AgregarLinea("--- Sesion finalizada ---");

        // --- Historial con scroll ---

        private void AgregarLinea(string linea)
        {
            _lineas.AppendLine(linea);
            _cantidadDeLineas++;

            if (_cantidadDeLineas > _maximoDeLineas)
            {
                var contenido = _lineas.ToString();
                var indice = contenido.IndexOf('\n');
                if (indice >= 0)
                {
                    _lineas.Clear();
                    _lineas.Append(contenido.Substring(indice + 1));
                }
                _cantidadDeLineas = _maximoDeLineas;
            }

            ActualizarCuerpo();
        }

        /// <summary>
        /// Pinta el cuerpo y mantiene la vista abajo (ultimos mensajes) SOLO si el usuario ya
        /// estaba ahi; si se subio a leer el historial, lo deja donde esta -- asi el scroll
        /// realmente sirve para ver los primeros mensajes sin que cada turno nuevo lo interrumpa.
        /// </summary>
        private void ActualizarCuerpo()
        {
            if (_cuerpo == null) return;

            var estabaAlFinal = _scroll == null || _scroll.verticalNormalizedPosition <= UmbralAutoScroll;
            _cuerpo.text = _lineas.ToString();

            if (_scroll == null) return;

            Canvas.ForceUpdateCanvases();
            if (estabaAlFinal) _scroll.verticalNormalizedPosition = 0f; // 0 = el final del contenido (ultimo mensaje)
        }

        private void RefrescarEncabezado()
        {
            if (_arnes == null || _encabezado == null) return;

            _casoMostrado = _arnes.CasoActual;
            _personalidadMostrada = _arnes.PersonalidadActual;
            _receptividadMostrada = _arnes.ReceptividadActual;
            _encabezadoPintado = true;

            _encabezado.text = string.Format(
                "CASO: {0}   |   PERSONALIDAD: {1}   |   RECEPTIVIDAD: {2}",
                _casoMostrado.IsNone ? SinSesion : _casoMostrado.Value,
                _personalidadMostrada.IsNone ? SinSesion : _personalidadMostrada.Value,
                _receptividadMostrada);
        }

        // --- Construccion de la UI en runtime, sin nada que armar a mano en el editor ---

        private void ConstruirUi()
        {
            var canvasGo = new GameObject("ConsolaDeTranscripcion_Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);
            canvasGo.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);

            // Panel de fondo: tamano fijo, anclado abajo a la izquierda del canvas (igual que
            // ConsolaDeVoz.cs, mas grande porque este si tiene historial largo con scroll).
            var panelGo = new GameObject("Panel", typeof(RectTransform), typeof(Image));
            var panelRect = (RectTransform)panelGo.transform;
            panelRect.SetParent(canvasGo.transform, false);
            panelRect.anchorMin = panelRect.anchorMax = new Vector2(0f, 0f);
            panelRect.offsetMin = new Vector2(20f, 20f);
            panelRect.offsetMax = new Vector2(20f + 820f, 20f + 620f);
            panelGo.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.65f);

            _encabezado = CrearTexto(panelRect, "Encabezado",
                ancla: (new Vector2(0f, 1f), new Vector2(1f, 1f)),
                offsetMin: new Vector2(12f, -52f), offsetMax: new Vector2(-12f, -12f),
                tamano: 22, color: new Color(1f, 0.85f, 0.4f), alineacion: TextAlignmentOptions.TopLeft);
            _encabezado.text = "CASO: " + SinSesion + "   |   PERSONALIDAD: " + SinSesion + "   |   RECEPTIVIDAD: Neutral"; // placeholder: RefrescarEncabezado lo reemplaza en el primer Update
            _encabezado.textWrappingMode = TextWrappingModes.NoWrap;

            // Divisor visual entre el encabezado (fijo) y el historial (con scroll).
            var divisorGo = new GameObject("Divisor", typeof(RectTransform), typeof(Image));
            var divisorRect = (RectTransform)divisorGo.transform;
            divisorRect.SetParent(panelRect, false);
            divisorRect.anchorMin = new Vector2(0f, 1f);
            divisorRect.anchorMax = new Vector2(1f, 1f);
            divisorRect.offsetMin = new Vector2(12f, -56f);
            divisorRect.offsetMax = new Vector2(-12f, -54f);
            divisorGo.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.25f);

            ConstruirScroll(panelRect);
            AsegurarEventSystem();
        }

        private void ConstruirScroll(RectTransform panelRect)
        {
            var scrollGo = new GameObject("ScrollView", typeof(RectTransform), typeof(ScrollRect));
            var scrollRect = (RectTransform)scrollGo.transform;
            scrollRect.SetParent(panelRect, false);
            scrollRect.anchorMin = Vector2.zero;
            scrollRect.anchorMax = Vector2.one;
            scrollRect.offsetMin = new Vector2(12f, 12f);
            scrollRect.offsetMax = new Vector2(-12f, -60f);

            var viewportGo = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D));
            var viewportRect = (RectTransform)viewportGo.transform;
            viewportRect.SetParent(scrollRect, false);
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = Vector2.zero;
            viewportRect.offsetMax = new Vector2(-20f, 0f); // deja el ancho de la scrollbar a la derecha

            // Contenido: ancho fijo por el stretch horizontal, alto libre (ContentSizeFitter lo
            // agranda con cada linea nueva). Ancla arriba: los turnos nuevos se agregan abajo,
            // como una consola/chat normal.
            var contentGo = new GameObject("Content", typeof(RectTransform),
                typeof(TextMeshProUGUI), typeof(ContentSizeFitter));
            var contentRect = (RectTransform)contentGo.transform;
            contentRect.SetParent(viewportRect, false);
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = new Vector2(0f, 0f);

            var fitter = contentGo.GetComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            _cuerpo = contentGo.GetComponent<TextMeshProUGUI>();
            _cuerpo.fontSize = 20;
            _cuerpo.color = Color.white;
            _cuerpo.alignment = TextAlignmentOptions.TopLeft;
            _cuerpo.textWrappingMode = TextWrappingModes.Normal;
            _cuerpo.text = "(esperando turnos...)";

            var scrollbar = ConstruirScrollbarVertical(scrollRect);

            _scroll = scrollGo.GetComponent<ScrollRect>();
            _scroll.viewport = viewportRect;
            _scroll.content = contentRect;
            _scroll.horizontal = false;
            _scroll.vertical = true;
            _scroll.movementType = ScrollRect.MovementType.Clamped;
            _scroll.verticalScrollbar = scrollbar;
            _scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;
            _scroll.verticalScrollbarSpacing = 4f;
        }

        /// <summary>
        /// Scrollbar vertical minima: fondo + manija, sin la envoltura "Sliding Area" del prefab
        /// de Unity (no es necesaria: el componente <see cref="Scrollbar"/> calcula el tamano y la
        /// posicion de la manija solo, a partir de <see cref="Scrollbar.handleRect"/>).
        /// </summary>
        private static Scrollbar ConstruirScrollbarVertical(RectTransform padre)
        {
            var barraGo = new GameObject("Scrollbar", typeof(RectTransform), typeof(Image), typeof(Scrollbar));
            var barraRect = (RectTransform)barraGo.transform;
            barraRect.SetParent(padre, false);
            barraRect.anchorMin = new Vector2(1f, 0f);
            barraRect.anchorMax = new Vector2(1f, 1f);
            barraRect.pivot = new Vector2(1f, 0.5f);
            barraRect.offsetMin = new Vector2(-18f, 0f);
            barraRect.offsetMax = Vector2.zero;
            barraGo.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.08f);

            var manijaGo = new GameObject("Handle", typeof(RectTransform), typeof(Image));
            var manijaRect = (RectTransform)manijaGo.transform;
            manijaRect.SetParent(barraRect, false);
            var manijaImagen = manijaGo.GetComponent<Image>();
            manijaImagen.color = new Color(1f, 1f, 1f, 0.5f);

            var scrollbar = barraGo.GetComponent<Scrollbar>();
            scrollbar.direction = Scrollbar.Direction.BottomToTop;
            scrollbar.handleRect = manijaRect;
            scrollbar.targetGraphic = manijaImagen;

            return scrollbar;
        }

        private static TextMeshProUGUI CrearTexto(
            RectTransform padre, string nombre,
            (Vector2 min, Vector2 max) ancla, Vector2 offsetMin, Vector2 offsetMax,
            float tamano, Color color, TextAlignmentOptions alineacion)
        {
            var go = new GameObject(nombre, typeof(RectTransform), typeof(TextMeshProUGUI));
            var rect = (RectTransform)go.transform;
            rect.SetParent(padre, false);
            rect.anchorMin = ancla.min;
            rect.anchorMax = ancla.max;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;

            var texto = go.GetComponent<TextMeshProUGUI>();
            texto.fontSize = tamano;
            texto.color = color;
            texto.alignment = alineacion;
            return texto;
        }

        private static void AsegurarEventSystem()
        {
            if (FindFirstObjectByType<EventSystem>() != null) return;

            var go = new GameObject("EventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM
            go.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
            go.AddComponent<StandaloneInputModule>();
#endif
        }
    }
}
