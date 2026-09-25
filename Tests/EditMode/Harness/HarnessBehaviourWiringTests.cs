using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NpcAi.Core;
using NpcAi.Core.Channels;
using NpcAi.Harness.Unity;
using NUnit.Framework;
using UnityEngine;

namespace NpcAi.Harness.Tests
{
    /// <summary>
    /// Cableado de <see cref="HarnessBehaviour"/> (M11, PR2; precedente
    /// <c>VrInputBehaviourWiringTests</c> de M7 y <c>NpcPresenterBehaviourTests</c> de M8): el
    /// sujeto es la cascara sobre un <see cref="SessionDirector"/> REAL armado con los cinco
    /// espias locales de <c>EspiasDeArnes.cs</c>, y los tres canales son instancias propias de
    /// cada prueba (<c>ScriptableObject.CreateInstance</c>), asi que ninguna prueba ve slots
    /// apuntando a assets distintos: eso es de la compuerta humana de escena.
    /// <para>
    /// Usa <c>CablearParaPrueba</c>/<c>ArrancarParaPrueba</c>/<c>DescablearParaPrueba</c>/
    /// <c>SalirParaPrueba</c> en vez de <c>GameObject.SetActive</c>: activar un GameObject recien
    /// creado no dispara <c>Awake</c>/<c>OnEnable</c> de forma confiable dentro de un metodo de
    /// prueba EditMode sincrono.
    /// </para>
    /// </summary>
    public sealed class HarnessBehaviourWiringTests
    {
        private static readonly ClinicalCaseId Caso01 = new ClinicalCaseId("caso-01");
        private static readonly PersonalityId Personalidad = new PersonalityId("grosero");

        // Todo lo que una prueba crea se registra aqui y TearDown lo destruye, incluso si la
        // prueba falla a mitad de camino.
        private readonly List<UnityEngine.Object> _creados = new List<UnityEngine.Object>();

        [TearDown]
        public void Limpiar()
        {
            foreach (var objeto in _creados)
                UnityEngine.Object.DestroyImmediate(objeto);

            _creados.Clear();
        }

        // --- Requirement: Simetria de suscripcion sobre los dos canales de entrada ---

        [Test]
        public void OnEnable_suscribe_los_dos_canales_de_entrada()
        {
            var m = Armar();

            m.Cascara.CablearParaPrueba(m.Director);

            // AD9: la cascara solo PUBLICA en el canal de respuesta, nunca se suscribe a el. Un
            // Raise ahi no debe llegar al director (un lazo haria que procesara su propia salida).
            m.Respuestas.Raise(new NpcReply("eco", "neutral", "idle"));
            Assert.AreEqual(0, m.M2.Invocaciones);
            Assert.AreEqual(0, m.M4.Evaluaciones.Count);

            m.Utterances.Raise(Dicho("hola doctora"));
            m.Acciones.Raise(PhysicalAction.Acercarse);

            // El canal de utterance llego a M2 sin alterar el texto.
            Assert.AreEqual("hola doctora", m.M2.UltimoTexto);

            // M4 evalua una vez por el turno hablado (accion Ninguna) y una por la accion fisica:
            // si solo uno de los dos canales estuviera suscrito, faltaria una evaluacion.
            Assert.AreEqual(2, m.M4.Evaluaciones.Count);
            Assert.AreEqual(PhysicalAction.Ninguna, m.M4.Evaluaciones[0].Action);
            Assert.AreEqual(PhysicalAction.Acercarse, m.M4.Evaluaciones[1].Action);
        }

        [Test]
        public void OnDisable_desuscribe_los_dos_canales_sin_fuga()
        {
            var m = Armar();
            m.Cascara.CablearParaPrueba(m.Director);

            // Control positivo: mientras esta habilitada, los dos canales SI llegan a los espias.
            m.Utterances.Raise(Dicho("uno"));
            m.Acciones.Raise(PhysicalAction.Acercarse);
            Assert.AreEqual(1, m.M2.Invocaciones, "control positivo: el canal de utterance debe llegar a M2");
            Assert.AreEqual(2, m.M4.Evaluaciones.Count, "control positivo: los dos canales deben llegar a M4");
            m.Replies.Clear(); // el contador de salida parte de cero para lo que sigue

            m.Cascara.DescablearParaPrueba();

            m.Utterances.Raise(Dicho("dos"));
            m.Acciones.Raise(PhysicalAction.Acercarse);

            // Contadores congelados: una fuga en cualquiera de los dos canales los moveria.
            Assert.AreEqual(1, m.M2.Invocaciones);
            Assert.AreEqual("uno", m.M2.UltimoTexto);
            Assert.AreEqual(2, m.M4.Evaluaciones.Count);
            Assert.AreEqual(0, m.Replies.Count);
        }

        [Test]
        public void Volver_a_habilitar_no_duplica_la_suscripcion()
        {
            var m = Armar();

            m.Cascara.CablearParaPrueba(m.Director);
            m.Cascara.DescablearParaPrueba();
            m.Cascara.CablearParaPrueba(m.Director);

            m.Utterances.Raise(Dicho());

            // El canal no deduplica: un oyente fugado en el primer ciclo daria 2, tanto en M2 como
            // en el contador de NpcReply del canal de salida.
            Assert.AreEqual(1, m.M2.Invocaciones);
            Assert.AreEqual(1, m.Replies.Count);

            m.Acciones.Raise(PhysicalAction.Acercarse);

            // 1 evaluacion del turno hablado + 1 de la accion; una fuga en el canal de accion daria 3.
            Assert.AreEqual(2, m.M4.Evaluaciones.Count);
        }

        [Test]
        public void Canal_de_entrada_sin_asignar_no_lanza()
        {
            var sinUtterance = Armar(conCanalDeUtterance: false);
            Assert.DoesNotThrow(() => sinUtterance.Cascara.CablearParaPrueba(sinUtterance.Director));
            Assert.DoesNotThrow(() => sinUtterance.Cascara.DescablearParaPrueba());

            var sinAccion = Armar(conCanalDeAccion: false);
            Assert.DoesNotThrow(() => sinAccion.Cascara.CablearParaPrueba(sinAccion.Director));
            Assert.DoesNotThrow(() => sinAccion.Cascara.DescablearParaPrueba());
        }

        // --- Requirement: Un solo Raise por turno hablado y ninguno por accion fisica sola ---

        [Test]
        public void Turno_social_produce_exactamente_un_Raise_con_la_respuesta_del_director()
        {
            var m = Armar();
            var r = new NpcReply("respuesta social marcada", "molesto", "cruzar_brazos");
            m.M15.Respuesta = Core.ClinicalResponse.NoAplica; // M15 no maneja el turno: ruta social
            m.M6.Respuesta = r;
            m.Cascara.CablearParaPrueba(m.Director);

            m.Utterances.Raise(Dicho("buenos dias"));

            Assert.AreEqual(1, m.Replies.Count);
            Assert.AreEqual(r.Text, m.Replies[0].Text);
            Assert.AreEqual(r.EmotionTag, m.Replies[0].EmotionTag);
            Assert.AreEqual(r.AnimationCue, m.Replies[0].AnimationCue);
            Assert.AreEqual("buenos dias", m.M2.UltimoTexto);
            Assert.AreEqual(1, m.M6.Invocaciones);
        }

        [Test]
        public void Turno_clinico_manejado_produce_exactamente_un_Raise()
        {
            var m = Armar();
            var c = new NpcReply("respuesta clinica marcada", "adolorido", "tocarse_el_pecho");
            m.M15.Respuesta = new Core.ClinicalResponse(true, c); // M15 maneja el turno: ruta clinica
            m.Cascara.CablearParaPrueba(m.Director);

            m.Utterances.Raise(Dicho("desde cuando le duele"));

            Assert.AreEqual(1, m.Replies.Count);
            Assert.AreEqual(c.Text, m.Replies[0].Text);
            Assert.AreEqual(c.EmotionTag, m.Replies[0].EmotionTag);
            Assert.AreEqual(c.AnimationCue, m.Replies[0].AnimationCue);
            Assert.AreEqual(0, m.M6.Invocaciones);
        }

        [Test]
        public void Accion_fisica_sola_no_publica_nada_pero_llega_al_director()
        {
            var m = Armar();
            m.Cascara.CablearParaPrueba(m.Director);

            m.Acciones.Raise(PhysicalAction.Acercarse);

            // Control positivo: la accion SI llego al director (M4 la evalua) y aun asi no hay Raise.
            Assert.AreEqual(1, m.M4.Evaluaciones.Count);
            Assert.AreEqual(PhysicalAction.Acercarse, m.M4.Evaluaciones[0].Action);
            Assert.AreEqual(0, m.Replies.Count);

            // Un gesto no habla: ni clasifica, ni responde, ni genera.
            Assert.AreEqual(0, m.M2.Invocaciones);
            Assert.AreEqual(0, m.M15.Invocaciones);
            Assert.AreEqual(0, m.M6.Invocaciones);
        }

        // --- Requirement: Diagnostico de calibracion (M2/M6/M15), solo lectura ---

        [Test]
        public void Los_diagnosticos_de_la_cascara_reenvian_los_del_director_tras_un_turno_clinico()
        {
            var m = Armar();
            var intentDeM2 = new IntentResult(Intent.SolicitudRespetuosa, Tone.Respetuoso, 0.85f, 12.5f);
            m.M2.Resultado = intentDeM2;
            m.M15.Respuesta = new Core.ClinicalResponse(
                true, new NpcReply("respuesta clinica", "adolorido", "tocarse_el_pecho"));
            m.Cascara.CablearParaPrueba(m.Director);

            m.Utterances.Raise(Dicho("desde cuando le duele"));

            Assert.IsTrue(m.Cascara.UltimoTurnoFueClinico);
            Assert.AreEqual(intentDeM2, m.Cascara.UltimoIntentClasificado);
        }

        [Test]
        public void Los_diagnosticos_de_la_cascara_son_el_valor_por_defecto_sin_director()
        {
            var m = Armar();
            m.Cascara.CablearParaPrueba(); // habilitada, SIN director

            Assert.IsFalse(m.Cascara.UltimoTurnoFueClinico);
            Assert.AreEqual(IntentResult.Unknown(), m.Cascara.UltimoIntentClasificado);
        }

        [Test]
        public void Caso_personalidad_y_receptividad_de_la_cascara_reenvian_los_del_director()
        {
            var m = Armar();
            m.Cascara.CablearParaPrueba(m.Director, _ => { }, () => { });
            m.Cascara.IniciarSesion();
            m.M4.Current = Core.Receptivity.NoReceptivo; // despues del Reset del arranque

            Assert.AreEqual(Caso01, m.Cascara.CasoActual);
            Assert.AreEqual(Personalidad, m.Cascara.PersonalidadActual);
            Assert.AreEqual(Core.Receptivity.NoReceptivo, m.Cascara.ReceptividadActual);
        }

        [Test]
        public void Caso_personalidad_y_receptividad_son_el_valor_por_defecto_sin_director()
        {
            var m = Armar();
            m.Cascara.CablearParaPrueba(); // habilitada, SIN director

            Assert.IsTrue(m.Cascara.CasoActual.IsNone);
            Assert.IsTrue(m.Cascara.PersonalidadActual.IsNone);
            Assert.AreEqual(Core.Receptivity.Neutral, m.Cascara.ReceptividadActual);
        }

        [Test]
        public void Canal_de_respuesta_sin_asignar_no_lanza()
        {
            var m = Armar(conCanalDeRespuesta: false);
            m.Cascara.CablearParaPrueba(m.Director);

            Assert.DoesNotThrow(() => m.Utterances.Raise(Dicho("hola")));

            // El director SI proceso el turno aunque no haya donde publicar la respuesta.
            Assert.AreEqual("hola", m.M2.UltimoTexto);
        }

        // --- Requirement: Frontera de inyeccion del director ---

        [Test]
        public void Sin_director_los_canales_de_entrada_no_publican()
        {
            var m = Armar();

            m.Cascara.CablearParaPrueba(); // habilitada, con los 3 canales y SIN director

            Assert.DoesNotThrow(() => m.Utterances.Raise(Dicho()));
            Assert.DoesNotThrow(() => m.Acciones.Raise(PhysicalAction.Acercarse));
            Assert.AreEqual(0, m.Replies.Count);

            // Control positivo: la cascara SI estaba suscrita (un cero tambien valdria si
            // OnEnable no hubiera suscrito nada); con un director inyectado, el mismo Raise publica.
            m.Cascara.Inyectar(m.Director, null, null);
            m.Utterances.Raise(Dicho());
            Assert.AreEqual(1, m.Replies.Count);
        }

        [Test]
        public void El_orden_entre_inyeccion_y_habilitacion_no_cambia_el_resultado()
        {
            // Primera: habilitada ANTES de inyectar el director (el orden real de la escena:
            // OnEnable corre en el pase de carga y la raiz de composicion inyecta despues).
            var primera = Armar();
            var aperturasDePrimera = new List<string>();
            var cierresDePrimera = 0;
            primera.Cascara.CablearParaPrueba();
            primera.Cascara.Inyectar(primera.Director, aperturasDePrimera.Add, () => cierresDePrimera++);

            // Segunda: el director ya esta presente al habilitar.
            var segunda = Armar();
            segunda.Cascara.CablearParaPrueba(segunda.Director);

            primera.Utterances.Raise(Dicho());
            segunda.Utterances.Raise(Dicho());

            Assert.AreEqual(1, primera.Replies.Count);
            Assert.AreEqual(1, segunda.Replies.Count);

            // Inyectar tambien guarda las dos costuras de M13 (CablearParaPrueba las asigna
            // directo, asi que solo esta ruta ejercita las asignaciones de Inyectar): abrir y
            // cerrar la sesion llega a las lambdas de la primera, una sola vez cada una.
            primera.Cascara.IniciarSesion();
            primera.Cascara.FinalizarSesion();

            Assert.AreEqual(1, aperturasDePrimera.Count);
            StringAssert.Contains("caso-01", aperturasDePrimera[0]);
            Assert.AreEqual(1, cierresDePrimera);
        }

        [Test]
        public void Inyectar_con_director_nulo_lanza_ArgumentNullException()
        {
            var m = Armar();

            var excepcion = Assert.Throws<ArgumentNullException>(() => m.Cascara.Inyectar(null, null, null));

            Assert.AreEqual("director", excepcion.ParamName);
        }

        // --- Requirement: Inicio de sesion como una sola operacion con cuatro efectos ---

        [Test]
        public void IniciarSesion_dispara_los_cuatro_efectos_juntos_con_el_mismo_caso()
        {
            var m = Armar();
            var etiquetas = new List<string>();
            m.Cascara.CablearParaPrueba(m.Director, etiquetas.Add, () => { });

            m.Cascara.IniciarSesion();

            // Efectos 1-3 (director): M4 recibe un Reset con la personalidad elegida; M9 y M15
            // reciben UNA asignacion cada uno, con el mismo caso.
            Assert.AreEqual(1, m.M4.Resets.Count);
            Assert.AreEqual(m.Director.PersonalidadActual, m.M4.Resets[0]);
            Assert.AreEqual(1, m.M9.CasosAsignados);
            Assert.AreEqual(1, m.M15.Asignaciones);
            Assert.AreEqual(m.Director.CasoActual, m.M9.UltimoCaso);
            Assert.AreEqual(m.M9.UltimoCaso, m.M15.UltimoCaso);

            // Efecto 4 (M13): una sola llamada, con una etiqueta que contiene ese caso.
            Assert.IsFalse(m.Director.CasoActual.IsNone);
            Assert.AreEqual(1, etiquetas.Count);
            StringAssert.Contains(m.Director.CasoActual.Value, etiquetas[0]);
        }

        [Test]
        public void IniciarSesion_sin_oyente_en_la_costura_de_M13_dispara_igual_los_tres_efectos_del_director()
        {
            var m = Armar();
            m.Cascara.CablearParaPrueba(m.Director); // sin abrirBitacora ni cerrarBitacora

            Assert.DoesNotThrow(() => m.Cascara.IniciarSesion());

            Assert.AreEqual(1, m.M4.Resets.Count);
            Assert.AreEqual(1, m.M9.CasosAsignados);
            Assert.AreEqual(1, m.M15.Asignaciones);
        }

        // --- Requirement: Etiqueta de la sesion de bitacora ---

        [Test]
        public void Etiqueta_lleva_el_prefijo_y_el_caso_elegido()
        {
            // Catalogo de un solo caso: el caso elegido es forzosamente caso-02.
            var m = Armar(casos: new[] { new ClinicalCaseId("caso-02") });
            m.Cascara._prefijoDeEtiqueta = "sim";
            var etiquetas = new List<string>();
            m.Cascara.CablearParaPrueba(m.Director, etiquetas.Add);

            m.Cascara.IniciarSesion();

            Assert.AreEqual(1, etiquetas.Count);
            StringAssert.StartsWith("sim", etiquetas[0]);
            StringAssert.Contains("caso-02", etiquetas[0]);

            // Forma exacta (AD7), con el prefijo por defecto y un reloj fijo:
            // {prefijo}-{caso}-{yyyyMMdd-HHmmss}.
            var fija = Armar(casos: new[] { Caso01 });
            var etiquetasFijas = new List<string>();
            fija.Cascara.CablearParaPrueba(
                fija.Director, etiquetasFijas.Add, null, () => new DateTime(2026, 9, 21, 14, 35, 12));

            fija.Cascara.IniciarSesion();

            Assert.AreEqual(1, etiquetasFijas.Count);
            Assert.AreEqual("banco-caso-01-20260921-143512", etiquetasFijas[0]);
        }

        [Test]
        public void Instantes_distintos_dan_etiquetas_distintas()
        {
            // Catalogo de un solo caso y mismo prefijo: la marca de tiempo es lo unico que las distingue.
            var m = Armar();
            var etiquetas = new List<string>();
            var ahora = new DateTime(2026, 9, 21, 14, 35, 12);
            m.Cascara.CablearParaPrueba(m.Director, etiquetas.Add, null, () => ahora);

            m.Cascara.IniciarSesion();
            ahora = ahora.AddSeconds(1);
            m.Cascara.IniciarSesion();

            Assert.AreEqual(2, etiquetas.Count);
            Assert.AreNotEqual(etiquetas[0], etiquetas[1]);
        }

        [Test]
        public void Prefijo_vacio_no_produce_etiqueta_vacia()
        {
            var m = Armar();
            m.Cascara._prefijoDeEtiqueta = "";
            var etiquetas = new List<string>();
            m.Cascara.CablearParaPrueba(m.Director, etiquetas.Add);

            m.Cascara.IniciarSesion();

            Assert.AreEqual(1, etiquetas.Count);
            Assert.IsFalse(string.IsNullOrWhiteSpace(etiquetas[0]));
            StringAssert.Contains("caso-01", etiquetas[0]);
        }

        // --- Requirement: Frontera de inyeccion del director (IniciarSesion sin director) ---

        [Test]
        public void Sin_director_IniciarSesion_no_abre_la_sesion_de_M13()
        {
            var m = Armar();
            var aperturas = 0;
            m.Cascara.CablearParaPrueba(abrirBitacora: _ => aperturas++); // habilitada, SIN director

            Assert.DoesNotThrow(() => m.Cascara.IniciarSesion());

            Assert.AreEqual(0, aperturas);
        }

        // --- Requirement: Cierre simetrico de la sesion de bitacora de M13 ---

        [Test]
        public void FinalizarSesion_dispara_la_costura_de_cierre_una_vez_sin_tocar_el_director()
        {
            var m = Armar();
            var cierres = 0;
            m.Cascara.CablearParaPrueba(m.Director, _ => { }, () => cierres++);
            m.Cascara.IniciarSesion();

            // Linea base tras el arranque: el cierre no debe moverla.
            var resets = m.M4.Resets.Count;
            var evaluaciones = m.M4.Evaluaciones.Count;
            var casosM9 = m.M9.CasosAsignados;
            var notificacionesM9 = m.M9.Notificaciones.Count;
            var asignacionesM15 = m.M15.Asignaciones;
            var invocacionesM15 = m.M15.Invocaciones;

            m.Cascara.FinalizarSesion();

            Assert.AreEqual(1, cierres);
            Assert.AreEqual(resets, m.M4.Resets.Count);
            Assert.AreEqual(evaluaciones, m.M4.Evaluaciones.Count);
            Assert.AreEqual(casosM9, m.M9.CasosAsignados);
            Assert.AreEqual(notificacionesM9, m.M9.Notificaciones.Count);
            Assert.AreEqual(asignacionesM15, m.M15.Asignaciones);
            Assert.AreEqual(invocacionesM15, m.M15.Invocaciones);
        }

        [Test]
        public void FinalizarSesion_sin_sesion_abierta_es_no_op()
        {
            var m = Armar();
            var cierres = 0;
            m.Cascara.CablearParaPrueba(m.Director, _ => { }, () => cierres++);

            m.Cascara.FinalizarSesion(); // sin sesion abierta

            Assert.AreEqual(0, cierres);

            m.Cascara.IniciarSesion();
            m.Cascara.FinalizarSesion();
            m.Cascara.FinalizarSesion(); // segundo cierre consecutivo: ya no hay sesion

            Assert.AreEqual(1, cierres);
        }

        // --- Requirement: La sesion de bitacora no queda abierta por olvido ---

        [Test]
        public void OnApplicationQuit_con_sesion_abierta_cierra_la_sesion_de_M13()
        {
            var m = Armar();
            var cierres = 0;
            m.Cascara.CablearParaPrueba(m.Director, _ => { }, () => cierres++);
            m.Cascara.IniciarSesion();

            // AD6: OnDisable NO cierra la bitacora (el registrador de M13 puede estar ya en null).
            m.Cascara.DescablearParaPrueba();
            Assert.AreEqual(0, cierres);

            m.Cascara.SalirParaPrueba();

            Assert.AreEqual(1, cierres);
        }

        [Test]
        public void IniciarSesion_con_otra_abierta_cierra_primero_la_anterior()
        {
            var m = Armar();
            var secuencia = new List<string>(); // compartida por las dos costuras: "inicio, cierre, inicio"
            var etiquetas = new List<string>();
            var ahora = new DateTime(2026, 9, 21, 14, 35, 12);
            m.Cascara.CablearParaPrueba(
                m.Director,
                etiqueta => { secuencia.Add("inicio"); etiquetas.Add(etiqueta); },
                () => secuencia.Add("cierre"),
                () => ahora);

            m.Cascara.IniciarSesion();
            ahora = ahora.AddSeconds(1);
            m.Cascara.IniciarSesion(); // sin cerrar la anterior

            CollectionAssert.AreEqual(new[] { "inicio", "cierre", "inicio" }, secuencia);
            Assert.AreEqual(2, etiquetas.Count);
            Assert.AreNotEqual(etiquetas[0], etiquetas[1]);
        }

        // --- Requirement: Punto de entrada publico y arranque automatico opcional ---

        [Test]
        public void IniciarSesion_es_publico_sin_parametros_y_sin_retorno()
        {
            // AD8: una sola IniciarSesion, sin sobrecarga (un UnityEvent del Inspector solo
            // alcanza metodos sin argumentos o con un argumento serializable).
            var metodo = typeof(HarnessBehaviour).GetMethod("IniciarSesion", Type.EmptyTypes);

            Assert.IsNotNull(metodo);
            Assert.IsTrue(metodo.IsPublic);
            Assert.AreEqual(typeof(void), metodo.ReturnType);
        }

        [Test]
        public void Con_el_flag_por_defecto_arrancar_no_inicia_sesion()
        {
            var m = Armar();
            var aperturas = 0;
            m.Cascara.CablearParaPrueba(m.Director, _ => aperturas++);

            Assert.IsFalse(m.Cascara._arrancarSolo); // AD8: apagado por defecto

            m.Cascara.ArrancarParaPrueba();

            Assert.AreEqual(0, m.M4.Resets.Count);
            Assert.AreEqual(0, m.M9.CasosAsignados);
            Assert.AreEqual(0, m.M15.Asignaciones);
            Assert.AreEqual(0, aperturas);
        }

        [Test]
        public void Con_el_flag_activo_arrancar_inicia_exactamente_una_sesion()
        {
            var m = Armar();
            var etiquetas = new List<string>();
            m.Cascara._arrancarSolo = true;
            m.Cascara.CablearParaPrueba(m.Director, etiquetas.Add);

            // AD3: habilitar NO arranca la sesion; solo lo hace Start, despues de la inyeccion.
            Assert.AreEqual(0, m.M4.Resets.Count);
            Assert.AreEqual(0, etiquetas.Count);

            m.Cascara.ArrancarParaPrueba();

            Assert.AreEqual(1, m.M4.Resets.Count);
            Assert.AreEqual(1, m.M9.CasosAsignados);
            Assert.AreEqual(1, m.M15.Asignaciones);
            Assert.AreEqual(1, etiquetas.Count);
        }

        // --- Requirement: DeclararTriaje es un pass-through publico ---

        [Test]
        public void DeclararTriaje_reenvia_la_categoria_sin_alterarla()
        {
            var m = Armar();
            m.Cascara.CablearParaPrueba(m.Director);

            m.Cascara.DeclararTriaje("  Rojo ");
            m.Cascara.DeclararTriaje("AMARILLO");

            // Crudo: sin recortar y sin cambiar mayusculas (normalizar es de M9, no de M11).
            Assert.AreEqual(2, m.Triajes.Count);
            Assert.AreEqual("  Rojo ", m.Triajes[0]);
            Assert.AreEqual("AMARILLO", m.Triajes[1]);
        }

        [Test]
        public void DeclararTriaje_no_publica_ni_toca_la_sesion_de_M13()
        {
            var m = Armar();
            var aperturas = 0;
            var cierres = 0;
            m.Cascara.CablearParaPrueba(m.Director, _ => aperturas++, () => cierres++);

            // Con una sesion ya abierta un cierre indebido SI se veria (sin sesion, FinalizarSesion
            // es no-op y un cero en cierres no probaria nada). Linea base: 1 apertura, 0 cierres.
            m.Cascara.IniciarSesion();
            Assert.AreEqual(1, aperturas);
            Assert.AreEqual(0, cierres);

            m.Cascara.DeclararTriaje("rojo");

            // Control positivo: el delegado de triaje del director SI recibio la categoria.
            Assert.AreEqual(1, m.Triajes.Count);
            Assert.AreEqual("rojo", m.Triajes[0]);
            Assert.AreEqual(0, m.Replies.Count);
            Assert.AreEqual(1, aperturas);
            Assert.AreEqual(0, cierres);
        }

        [Test]
        public void DeclararTriaje_sin_director_es_no_op()
        {
            var m = Armar();
            m.Cascara.CablearParaPrueba(); // habilitada, SIN director

            Assert.DoesNotThrow(() => m.Cascara.DeclararTriaje("rojo"));
        }

        // --- Requirement: Fronteras estructurales de la cascara ---

        [Test]
        public void Ensamblado_compilado_no_referencia_otros_modulos_NpcAi()
        {
            // Lista blanca, no negra: falla ante cualquier par, presente o futuro (p. ej.
            // NpcAi.RequirementResponse, que ninguna lista negra escrita hoy nombraria).
            var permitidos = new HashSet<string>
            {
                "NpcAi.Core", "NpcAi.Core.Channels", "NpcAi.Harness", "NpcAi.Harness.Unity",
            };

            var referenciados = typeof(HarnessBehaviour).Assembly.GetReferencedAssemblies()
                .Select(a => a.Name)
                .Where(nombre => nombre.StartsWith("NpcAi.", StringComparison.Ordinal))
                .ToArray();

            // Guarda anti-vacuidad: sin esto una enumeracion vacia pasaria la asercion de abajo.
            CollectionAssert.Contains(referenciados, "NpcAi.Core");

            var ajenos = referenciados.Where(nombre => !permitidos.Contains(nombre)).ToArray();
            Assert.IsEmpty(ajenos, "referencias a otros modulos NpcAi: " + string.Join(", ", ajenos));
        }

        [Test]
        public void DefaultExecutionOrder_de_la_cascara_es_positivo()
        {
            // AD2: solo se afirma que el atributo existe y es positivo; el efecto sobre el orden de
            // los oyentes no es observable en EditMode (lo cubre la compuerta humana de escena).
            // En Unity la clase del atributo se llama DefaultExecutionOrder, sin sufijo Attribute.
            var atributo = typeof(HarnessBehaviour).GetCustomAttribute<DefaultExecutionOrder>();

            Assert.IsNotNull(atributo);
            Assert.Greater(atributo.order, 0);
        }

        /// <summary>
        /// Una cascara completa con su entorno: 3 canales, los 5 espias, un director real, un
        /// contador de <see cref="NpcReply"/> suscrito a <see cref="NpcReplyChannel"/> y el
        /// delegado de triaje que graba. Cada llamada a <see cref="Armar"/> produce un montaje
        /// independiente (canales, espias y contadores propios).
        /// </summary>
        private sealed class Montaje
        {
            public UtteranceChannel Utterances;
            public PhysicalActionChannel Acciones;
            public NpcReplyChannel Respuestas;

            public readonly EspiaClasificador M2 = new EspiaClasificador();
            public readonly EspiaReceptividad M4 = new EspiaReceptividad();
            public readonly EspiaDialogo M6 = new EspiaDialogo();
            public readonly EspiaObjetivo M9 = new EspiaObjetivo();
            public readonly EspiaClinico M15 = new EspiaClinico();

            /// <summary>Cada <see cref="NpcReply"/> que llego a <see cref="NpcReplyChannel"/>, en orden.</summary>
            public readonly List<NpcReply> Replies = new List<NpcReply>();

            /// <summary>Cada categoria que llego al delegado de triaje del director, en orden.</summary>
            public readonly List<string> Triajes = new List<string>();

            public SessionDirector Director;
            public HarnessBehaviour Cascara;
        }

        /// <summary>
        /// Arma un <see cref="Montaje"/> SIN habilitar la cascara: cada prueba decide como
        /// cablearla (con o sin director, con o sin costuras de M13, con o sin reloj de prueba).
        /// Un canal excluido queda <c>null</c> en su slot (y, si es el de respuesta, sin contador).
        /// </summary>
        private Montaje Armar(
            ClinicalCaseId[] casos = null,
            bool conCanalDeUtterance = true,
            bool conCanalDeAccion = true,
            bool conCanalDeRespuesta = true)
        {
            var m = new Montaje();

            if (conCanalDeUtterance)
                m.Utterances = Registrar(ScriptableObject.CreateInstance<UtteranceChannel>());

            if (conCanalDeAccion)
                m.Acciones = Registrar(ScriptableObject.CreateInstance<PhysicalActionChannel>());

            if (conCanalDeRespuesta)
            {
                m.Respuestas = Registrar(ScriptableObject.CreateInstance<NpcReplyChannel>());
                m.Respuestas.Subscribe(m.Replies.Add);
            }

            m.Director = new SessionDirector(
                m.M2, m.M4, m.M6, m.M9, m.M15,
                m.M9.AssignCase,
                m.Triajes.Add,
                casos ?? new[] { Caso01 },
                new[] { Personalidad },
                semilla: 1);

            var go = Registrar(new GameObject("m11-harness-test"));
            m.Cascara = go.AddComponent<HarnessBehaviour>();
            m.Cascara._canalDeUtterance = m.Utterances;
            m.Cascara._canalDeAccion = m.Acciones;
            m.Cascara._canalDeRespuesta = m.Respuestas;

            return m;
        }

        private T Registrar<T>(T objeto) where T : UnityEngine.Object
        {
            _creados.Add(objeto);
            return objeto;
        }

        private static Utterance Dicho(string texto = "buenos dias") => new Utterance(texto, 0.9f, 1.5f);
    }
}
