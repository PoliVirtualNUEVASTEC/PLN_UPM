using System;
using System.Collections.Generic;
using NpcAi.Core;

namespace NpcAi.Scenarios.Boardroom
{
    /// <summary>
    /// M10 — implementacion real de <see cref="IScenarioObjective"/> para la sala de juntas
    /// (levantamiento de requerimientos). Mezcla TRES vias: cobertura del catalogo del caso,
    /// cierre explicito y fiel, y trato sostenido. Sin caso asignado
    /// (<see cref="HasCase"/> falso) los pesos se renormalizan y el trato es el 100 % del
    /// progreso, que es lo que ejercita ScenarioObjectiveContract via el constructor sin
    /// parametros. Superficie publica en ingles como M9; helpers privados en espanol.
    /// </summary>
    public sealed class RequirementsScenarioObjective : IScenarioObjective
    {
        private readonly BoardroomObjectiveSettings _pesos;
        private readonly Func<RequirementCaseId, string> _cargar;

        private RequirementChecklist _checklist; // null = ningun caso asignado (HasCase falso)
        private readonly HashSet<RequirementId> _revelados = new HashSet<RequirementId>();
        private HashSet<RequirementId> _resumen; // null = ningun cierre presentado aun
        private int _trato; // libro mayor simetrico, en [0, _pesos.PasosDeTrato] (AD1/AD2)

        /// <summary>Sujeto sin caso: solo la via de trato. Lo usa la gemela de contrato.</summary>
        public RequirementsScenarioObjective() : this(new BoardroomObjectiveSettings(), null)
        {
        }

        /// <param name="pesos">Null cae en los valores por defecto (AD13).</param>
        /// <param name="cargarJson">
        /// De un RequirementCaseId al JSON del caso, o null. PUEDE ser null: se trata como
        /// "ningun caso carga" y AssignCase nunca lanza (mismo contrato defensivo que
        /// TriageScenarioObjective y que RequirementResponder/AD9 de M16).
        /// </param>
        public RequirementsScenarioObjective(
            BoardroomObjectiveSettings pesos, Func<RequirementCaseId, string> cargarJson)
        {
            _pesos = pesos ?? new BoardroomObjectiveSettings();
            _cargar = cargarJson;
        }

        // --- Lecturas aditivas ---

        /// <summary><c>true</c> solo si hay un checklist cargado con al menos un id (AD4).</summary>
        public bool HasCase => _checklist != null;

        /// <summary>Total de requerimientos del caso asignado; 0 sin caso. Denominador.</summary>
        public int RequirementCount => _checklist?.Count ?? 0;

        /// <summary>Ids distintos ya acreditados como revelados. Numerador.</summary>
        public int RequirementsDisclosed => _revelados.Count;

        /// <summary>
        /// <c>true</c> si hay un resumen presentado, se revelo al menos un requerimiento
        /// y el conjunto presentado es EXACTAMENTE el revelado (AD8/AD9). Se recalcula en
        /// cada lectura: revelar algo despues de cerrar lo vuelve infiel hasta volver a
        /// presentar.
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

        /// <summary>
        /// Libro mayor simetrico (AD1): Improved suma 1 saturado en PasosDeTrato, Worsened
        /// resta 1 con piso en 0. Divergencia consciente frente a AD6 de M9 (que reinicia la
        /// racha a 0 ante un Worsened): con el cliente arrancando en Receptivo, un reinicio
        /// total dejaria el progreso irrecuperable ante un solo gesto malo. El decremento
        /// simetrico hace que "cada Improved lo recupera" sea literalmente cierto.
        /// Notify(default) es no-op via !change.Changed.
        /// </summary>
        public void Notify(ReceptivityChange change)
        {
            if (!change.Changed) return; // cubre default(ReceptivityChange) y From == To

            if (change.Improved && _trato < _pesos.PasosDeTrato) _trato++;
            else if (change.Worsened && _trato > 0) _trato--;
        }

        // --- Superficie aditiva de M10 ---

        /// <summary>
        /// Vincula un caso y carga su checklist. Descarta SIEMPRE el progreso anterior
        /// (checklist, revelados y resumen). Solo cuando la carga TIENE EXITO siembra el
        /// credito de trato LLENO: el cliente llega dispuesto (decision 1 de Jefferson);
        /// precedente de "AssignCase puede subir el progreso": AD11 de M9. Id desconocido,
        /// cargador null, funcion que lanza, JSON invalido o checklist vacio (AD4) dejan
        /// HasCase en false y el credito de trato en 0. Nunca lanza, nunca propaga.
        /// </summary>
        public void AssignCase(RequirementCaseId caseId)
        {
            _checklist = null;
            _revelados.Clear();
            _resumen = null;
            _trato = 0;

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

            if (json != null && RequirementChecklistLoader.TryParse(json, out var checklist))
            {
                _checklist = checklist;
                _trato = _pesos.PasosDeTrato; // credito sembrado lleno (AD1)
            }
        }

        /// <summary>
        /// Acredita SOLO Outcome == Revelado de un RequirementId que pertenezca al caso
        /// asignado (AD5). Idempotente por id (HashSet), mismo criterio que RegisterRedFlag
        /// en M9. Sin efecto antes de AssignCase. Nunca lanza.
        /// "Core." calificado a proposito (AD6): aunque hoy compile sin el prefijo porque el
        /// asmdef no referencia el ensamblado de M16, la busqueda de nombres de C# subiria por
        /// los namespaces contenedores hasta NpcAi, donde NpcAi.RequirementResponse es un
        /// namespace que ganaria contra el tipo importado por using el dia que esa referencia
        /// exista.
        /// </summary>
        public void RegisterDisclosure(Core.RequirementResponse response)
        {
            if (_checklist == null) return;
            if (response.Outcome != RequirementOutcome.Revelado) return;
            if (!_checklist.Contains(response.RequirementId)) return;

            _revelados.Add(response.RequirementId);
        }

        /// <summary>
        /// Cierre explicito, analogo a DeclareTriage en M9. Sobrescribible: la ultima
        /// llamada gana. null o coleccion vacia se tratan como resumen vacio, nunca lanzan.
        /// Sin efecto antes de AssignCase. Cerrar antes de cubrir el catalogo esta PERMITIDO
        /// y acredita su componente: es un cierre temprano honesto, con Progress01 < 1.
        /// </summary>
        public void PresentSummary(IReadOnlyCollection<RequirementId> requirementIds)
        {
            if (_checklist == null) return;

            _resumen = new HashSet<RequirementId>(requirementIds ?? Array.Empty<RequirementId>());
        }

        /// <summary>
        /// Vuelve al estado recien construido: checklist descartado, revelados y resumen
        /// limpios, credito de trato en 0. Idempotente. API real y probada, no codigo
        /// muerto (decision 5 de Jefferson); su paridad con el doble se prueba en el
        /// modulo, no ampliando ScenarioObjectiveContract (regla 2).
        /// </summary>
        public void Reset()
        {
            _checklist = null;
            _revelados.Clear();
            _resumen = null;
            _trato = 0;
        }
    }
}
