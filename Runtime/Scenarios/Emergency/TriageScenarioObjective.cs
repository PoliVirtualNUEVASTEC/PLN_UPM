using System;
using System.Collections.Generic;
using NpcAi.Core;

namespace NpcAi.Scenarios.Emergency
{
    /// <summary>
    /// M9 — primera implementacion real de <see cref="IScenarioObjective"/> para el
    /// escenario de emergencia (triaje). Mide una mezcla de receptividad sostenida
    /// (<see cref="Notify"/>, el puerto congelado) y correccion clinica (banderas rojas
    /// descubiertas + triaje declarado, via la superficie aditiva propia de esta clase).
    /// Sin caso asignado (<see cref="HasKey"/> falso), los pesos se renormalizan: la
    /// receptividad sostenida es el 100% del progreso, que es lo que ejercita
    /// <c>ScenarioObjectiveContract</c> a traves del constructor sin parametros.
    /// <para>
    /// AD2: toda la superficie es <c>public</c>, no <c>internal</c>. A diferencia de M1/M7/M8
    /// (donde el precedente <c>internal</c> expone un tipo interno del modulo), aqui la firma
    /// solo usa <see cref="ClinicalCaseId"/> (M0) y <see cref="TriageObjectiveSettings"/> (POCO
    /// publico), y el llamador real es M11, otro ensamblado.
    /// </para>
    /// </summary>
    public sealed class TriageScenarioObjective : IScenarioObjective
    {
        private readonly TriageObjectiveSettings _pesos;
        private readonly Func<ClinicalCaseId, string> _cargar;

        private TriageKey _clave;
        private int _racha;
        private readonly HashSet<int> _banderas = new HashSet<int>();
        private string _triaje; // null = nada declarado; "" = declaracion vacia (no coincide)

        /// <summary>Sujeto sin caso: solo la via receptiva. Lo usa la gemela de contrato.</summary>
        public TriageScenarioObjective() : this(new TriageObjectiveSettings(), null)
        {
        }

        /// <summary>
        /// AD2: publico porque M11 (el compositor real) vive en otro ensamblado.
        /// <paramref name="cargarJson"/> PUEDE ser <c>null</c>: se trata como "ningun caso
        /// carga", nunca lanza.
        /// </summary>
        public TriageScenarioObjective(TriageObjectiveSettings pesos, Func<ClinicalCaseId, string> cargarJson)
        {
            _pesos = pesos ?? new TriageObjectiveSettings();
            _cargar = cargarJson;
        }

        // --- Lecturas aditivas ---

        /// <summary><c>true</c> solo si hay un bloque <c>clave</c> cargado y valido.</summary>
        public bool HasKey => _clave != null;

        /// <summary>Indices distintos de <c>clave.banderasRojas</c> ya registrados.</summary>
        public int RedFlagsFound => _banderas.Count;

        /// <summary>Total de banderas rojas de la clave cargada; 0 sin clave.</summary>
        public int RedFlagCount => _clave?.BanderasRojas.Count ?? 0;

        /// <summary><c>clave.cierreEsperado</c>; cadena vacia sin clave. No mueve <see cref="Progress01"/>.</summary>
        public string ExpectedClosure => _clave?.CierreEsperado ?? string.Empty;

        // --- IScenarioObjective (puerto congelado) ---

        public float Progress01
        {
            get
            {
                double r = _pesos.RachaParaReceptividadPlena <= 0
                    ? 1.0
                    : Math.Min(1.0, (double)_racha / _pesos.RachaParaReceptividadPlena);

                if (_clave == null)
                    return Clamp01((float)r); // renormalizacion: receptividad = 100% del progreso

                double f = _clave.BanderasRojas.Count == 0
                    ? 1.0 // AD11: sin banderas rojas, ese componente cuenta como satisfecho
                    : (double)_banderas.Count / _clave.BanderasRojas.Count;

                double t = (_triaje != null && _triaje == _clave.TriajeEsperado) ? 1.0 : 0.0;

                double clinico = _pesos.PesoDeBanderasRojas * f + _pesos.PesoDeTriaje * t;
                double total = _pesos.PesoDeReceptividad * r + _pesos.PesoClinico * clinico;
                return Clamp01((float)total);
            }
        }

        public bool IsComplete => Math.Abs(Progress01 - 1f) <= 1e-4f;

        public void Notify(ReceptivityChange change)
        {
            if (!change.Changed) return; // cubre default(ReceptivityChange) y From == To

            if (change.Improved && _racha < _pesos.RachaParaReceptividadPlena) _racha++; // AD6: saturado
            else if (change.Worsened) _racha = 0; // reinicio total, no se descuenta un paso
        }

        // --- Superficie aditiva de M9 ---

        /// <summary>
        /// Vincula un caso clinico y carga su <c>clave</c>. Descarta SIEMPRE el progreso
        /// clinico anterior (banderas y triaje), incluso si la carga falla. La racha de
        /// receptividad no se toca: no esta atada al caso asignado (AD8).
        /// </summary>
        public void AssignCase(ClinicalCaseId caseId)
        {
            _clave = null;
            _banderas.Clear();
            _triaje = null;

            if (caseId.IsNone || _cargar == null) return;

            string json;
            try
            {
                json = _cargar(caseId);
            }
            catch (Exception)
            {
                return; // "ningun caso carga": nunca propaga
            }

            if (json != null && TriageKeyLoader.TryParse(json, out var clave))
                _clave = clave;
        }

        /// <summary>
        /// Idempotente por indice; rechaza indices fuera de rango sin lanzar; sin efecto
        /// antes de <see cref="AssignCase"/>.
        /// </summary>
        public void RegisterRedFlag(int index)
        {
            if (_clave == null) return;
            if (index < 0 || index >= _clave.BanderasRojas.Count) return;
            _banderas.Add(index);
        }

        /// <summary>
        /// Compara contra <c>clave.triajeEsperado</c> normalizado (AD10). Sobrescribible: la
        /// ultima llamada gana. Sin efecto antes de <see cref="AssignCase"/>.
        /// </summary>
        public void DeclareTriage(string category)
        {
            if (_clave == null) return;
            _triaje = Normalizar(category); // null/vacio -> "": declaracion que no coincide
        }

        /// <summary>
        /// Vuelve al estado recien construido: racha en 0, clave descartada, banderas y
        /// triaje limpios (AD8). Idempotente.
        /// </summary>
        public void Reset()
        {
            _clave = null;
            _banderas.Clear();
            _triaje = null;
            _racha = 0;
        }

        private static string Normalizar(string s) => s == null ? string.Empty : s.Trim().ToUpperInvariant();

        private static float Clamp01(float x) => x < 0f ? 0f : (x > 1f ? 1f : x);
    }
}
