using System;
using System.Collections.Generic;
using NpcAi.ClinicalResponse;
using NpcAi.Core;
using NpcAi.Dialogue;
using NpcAi.Harness;
using NpcAi.Harness.Unity;
using NpcAi.Nlu;
using NpcAi.Receptivity;
using NpcAi.Scenarios.Emergency;
using NpcAi.SessionLog.Unity;
using Unity.InferenceEngine;
using UnityEngine;

namespace NpcAi.Samples.Harness
{
    /// <summary>
    /// Raiz de composicion del anfitrion (design.md, AD10): el unico lugar del banco de pruebas
    /// que construye las implementaciones concretas de M2, M4, M6, M9 y M15 y las inyecta en
    /// <see cref="HarnessBehaviour"/> (M11). Vive en <c>Samples~/</c>, sin <c>.asmdef</c> propio:
    /// Unity no la compila dentro del paquete, asi que ve libremente los namespaces de todos los
    /// modulos runtime (todos son <c>autoReferenced: true</c>) sin necesidad de declarar diez
    /// referencias a mano. Elegir, secuenciar y enrutar sigue siendo trabajo de
    /// <see cref="SessionDirector"/> (M11, <c>Runtime/Harness/</c>): esta clase solo construye e
    /// inyecta, nada mas.
    /// <para>
    /// Sin pruebas EditMode por diseno: el Editor no compila <c>Samples~/</c> dentro del paquete
    /// y esta clase depende de Sentis (<see cref="ModelAsset"/>). Se mantiene minima a proposito;
    /// todo el comportamiento observable vive en <c>Runtime/Harness/</c>, que si esta cubierto.
    /// </para>
    /// </summary>
    public sealed class CompositorDeArnes : MonoBehaviour
    {
        /// <summary>
        /// Par personalidad/corpus de dialogo (design.md, AD10): Unity no serializa tuplas, asi
        /// que hace falta esta clase intermedia con campos publicos (mismo patron que la clase
        /// anidada <c>CorpusJson</c> de <see cref="MarkovDialogueGenerator"/>).
        /// </summary>
        [Serializable]
        private sealed class CorpusDePersonalidad
        {
            public string Personalidad;
            public TextAsset Corpus;
        }

        [SerializeField] private TextAsset[] _casos;
        [SerializeField] private CorpusDePersonalidad[] _corpus;
        [SerializeField] private ModelAsset _modelo;
        [SerializeField] private TextAsset _tokenizador;
        [SerializeField] private int _semilla;
        [SerializeField] private HarnessBehaviour _arnes;
        [SerializeField] private SessionLogBehaviour _bitacora;

        // Guardada para OnDestroy: BertIntentClassifier implementa IDisposable y nadie mas lo
        // libera (AD10). El campo queda tipado por el puerto (no por la clase concreta); el cast
        // a IDisposable en OnDestroy es deliberado.
        private IIntentClassifier _clasificador;

        /// <summary>
        /// Construye M2, M4, M6, M9 y M15 desde los assets del Inspector, arma el
        /// <see cref="SessionDirector"/> y lo entrega a <see cref="_arnes"/> junto con la costura
        /// de M13 (design.md, AD5: dos delegados, no un <c>UnityEvent</c>). <see cref="_casos"/> y
        /// <see cref="_corpus"/> son la unica fuente de los cuatro catalogos derivados: agregar un
        /// caso o una personalidad es una entrada de Inspector, cero lineas de codigo.
        /// </summary>
        private void Awake()
        {
            var casos = new List<ClinicalCaseId>();
            if (_casos != null)
            {
                foreach (var asset in _casos)
                    if (asset != null) casos.Add(new ClinicalCaseId(asset.name));
            }

            var corpusPorPersonalidad = new Dictionary<string, TextAsset>();
            var personalidades = new List<PersonalityId>();
            if (_corpus != null)
            {
                foreach (var entrada in _corpus)
                {
                    if (entrada == null || entrada.Corpus == null) continue;

                    var id = new PersonalityId(entrada.Personalidad);
                    if (id.IsNone) continue;

                    corpusPorPersonalidad[id.Value] = entrada.Corpus;
                    personalidades.Add(id);
                }
            }

            _clasificador = new BertIntentClassifier(_modelo, _tokenizador);
            var receptividad = new ReceptivityEngine();
            var dialogo = new MarkovDialogueGenerator(corpusPorPersonalidad);
            var objetivo = new TriageScenarioObjective(new TriageObjectiveSettings(), CargarCaso);
            var respondedorClinico = new ClinicalResponder(CargarCaso);

            var director = new SessionDirector(
                _clasificador,
                receptividad,
                dialogo,
                objetivo,
                respondedorClinico,
                objetivo.AssignCase,
                objetivo.DeclareTriage,
                casos,
                personalidades,
                _semilla);

            _arnes.Inyectar(director, _bitacora.IniciarSesion, _bitacora.FinalizarSesion);
        }

        /// <summary>
        /// Unico lector de <see cref="_casos"/> (M9 y M15 comparten esta misma fuente): compara
        /// <c>new ClinicalCaseId(asset.name) == id</c>, NUNCA <c>asset.name == id.Value</c> — el
        /// constructor de <see cref="ClinicalCaseId"/> recorta y pasa a minusculas su entrada, asi
        /// que comparar el nombre crudo del asset fallaria con un archivo <c>Caso-01.json</c>
        /// (design.md, AD10).
        /// </summary>
        private string CargarCaso(ClinicalCaseId id)
        {
            if (_casos == null) return null;

            foreach (var asset in _casos)
                if (asset != null && new ClinicalCaseId(asset.name) == id) return asset.text;

            return null;
        }

        private void OnDestroy()
        {
            (_clasificador as IDisposable)?.Dispose();
        }
    }
}
