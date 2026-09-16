using System;
using NpcAi.Core;

namespace NpcAi.VrInput
{
    /// <summary>
    /// Estado de la banda de histeresis del detector de distancia (Detector 2, design.md).
    /// </summary>
    internal enum EstadoDeDistancia
    {
        Indeterminado = 0,
        Cerca = 1,
        Lejos = 2,
    }

    /// <summary>
    /// Snapshot POCO de umbrales que consume el nucleo (AD1/AD6): nunca lee <c>UnityEngine</c>.
    /// Fase 1 la define aqui con los defaults de <c>design.md</c> (tabla "Configuracion").
    /// Fase 2 (tasks.md 2.2/2.4) la traslada intacta a
    /// <c>Runtime/VrInput/Config/VrInputSettings.cs</c>, junto al <c>VrInputSettingsAsset</c>
    /// que la produce via <c>OnValidate</c>/<c>ToSettings()</c>; este archivo deja de definirla
    /// en ese momento.
    /// </summary>
    internal sealed class VrInputSettings
    {
        public float GradosDelConoDeMirada { get; set; } = 20f;
        public float GradosDeLiberacionDeMirada { get; set; } = 30f;
        public float SegundosDePermanenciaDeMirada { get; set; } = 0.6f;
        public float MetrosParaAcercarse { get; set; } = 1.2f;
        public float MetrosParaAlejarse { get; set; } = 2.0f;
        public float SegundosDeEnfriamientoDeContacto { get; set; } = 1.0f;
    }

    /// <summary>
    /// Nucleo real de <see cref="IPhysicalActionSource"/> (M7): tres detectores con histeresis
    /// y antirrebote sobre <see cref="SpatialSample"/> ya digeridas. No conoce <c>Camera</c>,
    /// <c>Transform</c>, <c>Collider</c> ni <c>Time</c> (AD1/AD3/AD6): todo corre sincronico
    /// dentro de <c>ProcesarMuestra</c>, en el mismo hilo y el mismo frame que la llama.
    /// </summary>
    public sealed class SpatialPhysicalActionSource : IPhysicalActionSource
    {
        private readonly VrInputSettings _config;
        private readonly ISpatialSampler _muestreador;

        // Detector 1 -- ContactoVisual (mirada de cabeza, permanencia + liberacion)
        private bool _mirando;
        private float _segundosEnCono;

        // Detector 2 -- Acercarse/Alejarse (banda de histeresis)
        private EstadoDeDistancia _distancia = EstadoDeDistancia.Indeterminado;

        // Detector 3 -- TocarPaciente (pulso + enfriamiento). MaxValue: el primer contacto
        // siempre emite (AD, ver design.md Detector 3).
        private float _segundosDesdeContacto = float.MaxValue;

        public event Action<PhysicalAction> OnAction;

        /// <summary>Sujeto sin cablear: solo la via guardada (AD5). Lo usa la gemela de contrato.</summary>
        public SpatialPhysicalActionSource()
            : this(new VrInputSettings(), null)
        {
        }

        /// <summary>internal: expone tipos internos del modulo (regla dura 3), como en M1.</summary>
        internal SpatialPhysicalActionSource(VrInputSettings config, ISpatialSampler muestreador)
        {
            _config = config;
            _muestreador = muestreador;
        }

        /// <summary>Jala una muestra del muestreador cableado y corre los detectores (AD6).</summary>
        internal void Bombear()
        {
            if (_muestreador == null) return;
            if (_muestreador.LeerMuestra(out var muestra)) ProcesarMuestra(muestra);
        }

        /// <summary>
        /// Corre los tres detectores en orden fijo: mirada, distancia, contacto. Un tick puede
        /// levantar mas de una accion (encarar y cruzar el umbral a la vez es legitimo).
        /// </summary>
        internal void ProcesarMuestra(in SpatialSample m)
        {
            DetectarMirada(m);
            DetectarDistancia(m);
            DetectarContacto(m);
        }

        /// <summary>AD5: costura de prueba, delega en <see cref="Levantar"/> sin estrangular.</summary>
        internal bool EmitirParaPrueba(PhysicalAction accion) => Levantar(accion);

        /// <summary>Observabilidad de prueba: estado actual de la banda de histeresis de distancia.</summary>
        internal EstadoDeDistancia DistanciaActual => _distancia;

        /// <summary>Observabilidad de prueba: si el latch de mirada sostenida esta activo.</summary>
        internal bool MirandoAlObjetivo => _mirando;

        private void DetectarMirada(in SpatialSample m)
        {
            if (!m.HayObjetivo)
            {
                _segundosEnCono = 0f;
                _mirando = false;
                return;
            }

            var direccionAlObjetivo = m.PosicionDelObjetivo - m.PosicionDeCabeza;
            var angulo = Vec3.AnguloEnGrados(m.FrenteDeCabeza, direccionAlObjetivo);
            if (angulo < 0f) return; // direccion degenerada: no toca el estado

            if (!_mirando)
            {
                if (angulo <= _config.GradosDelConoDeMirada)
                {
                    _segundosEnCono += m.DeltaSegundos;
                    if (_segundosEnCono >= _config.SegundosDePermanenciaDeMirada)
                    {
                        _mirando = true;
                        Levantar(PhysicalAction.ContactoVisual); // una sola vez por mirada
                    }
                }
                else
                {
                    _segundosEnCono = 0f; // salir del cono reinicia la permanencia
                }
            }
            else if (angulo > _config.GradosDeLiberacionDeMirada) // cono ancho de salida (histeresis)
            {
                _mirando = false;
                _segundosEnCono = 0f;
            }
        }

        private void DetectarDistancia(in SpatialSample m)
        {
            if (!m.HayObjetivo) return; // conserva el estado: perder el objetivo no es alejarse

            var d = Vec3.Distancia(m.PosicionDeCabeza, m.PosicionDelObjetivo);

            if (_distancia == EstadoDeDistancia.Indeterminado) // AD8: sembrar sin emitir
            {
                var puntoMedio = (_config.MetrosParaAcercarse + _config.MetrosParaAlejarse) * 0.5f;
                _distancia = d <= puntoMedio ? EstadoDeDistancia.Cerca : EstadoDeDistancia.Lejos;
                return;
            }

            var nuevo = _distancia;
            if (d <= _config.MetrosParaAcercarse) nuevo = EstadoDeDistancia.Cerca;
            else if (d >= _config.MetrosParaAlejarse) nuevo = EstadoDeDistancia.Lejos;
            // dentro de la banda: nuevo == _distancia -> no hay transicion

            if (nuevo == _distancia) return;
            _distancia = nuevo;
            Levantar(nuevo == EstadoDeDistancia.Cerca ? PhysicalAction.Acercarse : PhysicalAction.Alejarse);
        }

        private void DetectarContacto(in SpatialSample m)
        {
            if (_segundosDesdeContacto < _config.SegundosDeEnfriamientoDeContacto)
                _segundosDesdeContacto += m.DeltaSegundos; // acumulacion saturada, sin desbordar

            if (!m.PulsoDeContacto) return;
            if (_segundosDesdeContacto < _config.SegundosDeEnfriamientoDeContacto) return; // rebote

            _segundosDesdeContacto = 0f;
            Levantar(PhysicalAction.TocarPaciente);
        }

        /// <summary>AD4: unica via de emision; solo rechaza <see cref="PhysicalAction.Ninguna"/>.</summary>
        private bool Levantar(PhysicalAction accion)
        {
            if (accion == PhysicalAction.Ninguna) return false;
            OnAction?.Invoke(accion);
            return true;
        }
    }
}
