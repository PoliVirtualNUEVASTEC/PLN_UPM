using System;
using System.Collections.Generic;
using NpcAi.Core;

namespace NpcAi.Scenarios.Boardroom.Fakes
{
    /// <summary>
    /// Doble determinista de M10 — Sala de Juntas (levantamiento de requerimientos).
    /// Espeja la superficie aditiva de <see cref="RequirementsScenarioObjective"/> firma por
    /// firma (<see cref="HasCase"/>/<see cref="RequirementCount"/>/
    /// <see cref="RequirementsDisclosed"/>/<see cref="SummaryIsFaithful"/>,
    /// <see cref="AssignCase"/>/<see cref="RegisterDisclosure"/>/<see cref="PresentSummary"/>/
    /// <see cref="Reset"/>). Sin IO ni <c>Data/</c>: los 4 casos reales del catalogo de M16
    /// (<c>caso-juntas-01</c>..<c>04</c>) viven embebidos como tabla de ids en C# puro (mismo
    /// criterio que <c>ScriptedRequirementResponder</c>, M16), listando solo los
    /// <c>RequirementId</c> de cada caso — nunca <c>respuesta</c> ni <c>receptividadMinima</c>,
    /// que son del respondedor, no del objetivo. Reusa
    /// <see cref="BoardroomObjectiveSettings.Mezclar"/> para la paridad aritmetica con el real:
    /// no reimplementa la formula a mano (regla 4 del repo).
    /// </summary>
    public sealed class ScriptedScenarioObjective : IScenarioObjective
    {
        // Copia embebida de los ids de requerimiento de Data/Requirements/caso-juntas-0N.json
        // (catalogo real de M16). Solo los ids importan aqui: cobertura y RegisterDisclosure
        // no leen ni respuesta ni receptividadMinima.
        private static readonly Dictionary<string, RequirementId[]> CasosEmbebidos =
            new Dictionary<string, RequirementId[]>
            {
                ["caso-juntas-01"] = IdsDe(
                    "equipos", "jugadores", "partidos", "estadio", "arbitros", "estadisticas"),
                ["caso-juntas-02"] = IdsDe(
                    "producto", "proveedor", "precio_compra", "inventario", "cliente_frecuente", "factura"),
                ["caso-juntas-03"] = IdsDe(
                    "rangos_edad", "alergias", "menu", "descuento_empleados"),
                ["caso-juntas-04"] = IdsDe(
                    "avion", "vuelo", "tripulante", "tiquete", "pasajero_identificacion", "pasajero_multivuelo"),
            };

        private static RequirementId[] IdsDe(params string[] ids)
        {
            var resultado = new RequirementId[ids.Length];
            for (var i = 0; i < ids.Length; i++) resultado[i] = new RequirementId(ids[i]);
            return resultado;
        }

        private readonly BoardroomObjectiveSettings _pesos = new BoardroomObjectiveSettings();

        private HashSet<RequirementId> _checklist; // null = ningun caso asignado (HasCase falso)
        private readonly HashSet<RequirementId> _revelados = new HashSet<RequirementId>();
        private HashSet<RequirementId> _resumen; // null = ningun cierre presentado aun
        private int _trato; // libro mayor simetrico, en [0, _pesos.PasosDeTrato]

        // --- Lecturas aditivas (espejo de RequirementsScenarioObjective) ---

        /// <summary><c>true</c> solo si el caso asignado es uno de los 4 ids conocidos.</summary>
        public bool HasCase => _checklist != null;

        /// <summary>Total de requerimientos del caso asignado; 0 sin caso. Denominador.</summary>
        public int RequirementCount => _checklist?.Count ?? 0;

        /// <summary>Ids distintos ya acreditados como revelados. Numerador.</summary>
        public int RequirementsDisclosed => _revelados.Count;

        /// <summary>
        /// <c>true</c> si hay un resumen presentado, se revelo al menos un requerimiento y el
        /// conjunto presentado es EXACTAMENTE el revelado. Se recalcula en cada lectura, igual
        /// que en el real.
        /// </summary>
        public bool SummaryIsFaithful =>
            _resumen != null && _revelados.Count > 0 && _resumen.SetEquals(_revelados);

        // --- IScenarioObjective (puerto congelado) ---

        public float Progress01
        {
            get
            {
                var cobertura = HasCase && _checklist.Count > 0
                    ? (float)_revelados.Count / _checklist.Count
                    : 0f;
                var cierre = SummaryIsFaithful ? 1f : 0f;
                var trato = (float)_trato / _pesos.PasosDeTrato;

                return _pesos.Mezclar(HasCase, cobertura, cierre, trato);
            }
        }

        public bool IsComplete => Math.Abs(Progress01 - 1f) <= 1e-4f;

        /// <summary>Mismo libro mayor simetrico que el real (AD1): +1/-1 saturado en [0, PasosDeTrato].</summary>
        public void Notify(ReceptivityChange change)
        {
            if (!change.Changed) return;

            if (change.Improved && _trato < _pesos.PasosDeTrato) _trato++;
            else if (change.Worsened && _trato > 0) _trato--;
        }

        // --- Superficie aditiva de M10 ---

        /// <summary>
        /// Busca <paramref name="caseId"/> en la tabla embebida de 4 casos. Descarta SIEMPRE el
        /// progreso anterior; solo un id conocido puebla el checklist y siembra el trato lleno.
        /// Id desconocido o <see cref="RequirementCaseId.IsNone"/> dejan <see cref="HasCase"/>
        /// en falso. Nunca lanza (no hay IO que pueda fallar).
        /// </summary>
        public void AssignCase(RequirementCaseId caseId)
        {
            _checklist = null;
            _revelados.Clear();
            _resumen = null;
            _trato = 0;

            if (caseId.IsNone) return;
            if (!CasosEmbebidos.TryGetValue(caseId.Value, out var ids)) return;

            _checklist = new HashSet<RequirementId>(ids);
            _trato = _pesos.PasosDeTrato; // credito sembrado lleno, igual que el real
        }

        /// <summary>
        /// Acredita SOLO <c>Outcome == Revelado</c> de un id que pertenezca al caso asignado.
        /// Idempotente por id. Sin efecto antes de <see cref="AssignCase"/>. Nunca lanza.
        /// </summary>
        public void RegisterDisclosure(Core.RequirementResponse response)
        {
            if (_checklist == null) return;
            if (response.Outcome != RequirementOutcome.Revelado) return;
            if (!_checklist.Contains(response.RequirementId)) return;

            _revelados.Add(response.RequirementId);
        }

        /// <summary>
        /// Cierre explicito, sobrescribible. <c>null</c> o coleccion vacia se tratan como
        /// resumen vacio, nunca lanzan. Sin efecto antes de <see cref="AssignCase"/>.
        /// </summary>
        public void PresentSummary(IReadOnlyCollection<RequirementId> requirementIds)
        {
            if (_checklist == null) return;

            _resumen = new HashSet<RequirementId>(requirementIds ?? Array.Empty<RequirementId>());
        }

        /// <summary>Vuelve al estado recien construido. Idempotente. Paridad con el real.</summary>
        public void Reset()
        {
            _checklist = null;
            _revelados.Clear();
            _resumen = null;
            _trato = 0;
        }
    }
}
