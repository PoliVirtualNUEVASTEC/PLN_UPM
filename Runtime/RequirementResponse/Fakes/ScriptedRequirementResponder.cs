using System.Collections.Generic;
using NpcAi.Core;

namespace NpcAi.RequirementResponse.Fakes
{
    /// <summary>
    /// Doble determinista de M16. Tabla de requerimientos embebida (sin leer
    /// <c>Data/Requirements/</c>): sirve para que otros modulos (M10, M11) integren contra
    /// <see cref="IRequirementResponder"/> sin depender del catalogo. C# puro, sin
    /// UnityEngine ni IO. Reutiliza <see cref="RequirementMatcher"/> y
    /// <see cref="RequirementDisclosurePolicy"/> (regla 4 del repo), asi la paridad con la
    /// implementacion real no depende de replicar logica a mano.
    /// <para>
    /// Reconoce los mismos 4 ids que el catalogo real (<c>caso-juntas-01</c> a
    /// <c>caso-juntas-04</c>) como "casos existentes"; cualquier otro id (incluido
    /// <c>"no-existe"</c>, que usa <c>RequirementResponderContract</c>) deja <c>IsReady</c> en
    /// <c>false</c>. Sin memoria entre turnos: la revelacion depende SOLO del
    /// <see cref="Receptivity"/> de cada llamada, y <c>intent</c> no filtra nada (AD5).
    /// </para>
    /// <para>
    /// El requerimiento <c>presupuesto</c> es dato SINTETICO: el catalogo real no lo declara
    /// (decision #66), pero el doble si puede usarlo y es lo que permite que los 10 <c>[Test]</c>
    /// heredados corran sin <c>Assume</c>-omitirse. El personaje no aplica matiz de
    /// personalidad: eso es del respondedor real.
    /// </para>
    /// </summary>
    public sealed class ScriptedRequirementResponder : IRequirementResponder
    {
        // Desvio unico y constante: la garantia del contrato es "nunca vacio y sin el hecho".
        private const string Desvio = "Prefiero que hablemos de eso mas adelante.";

        private static readonly HashSet<string> IdsConocidos = new HashSet<string>
            { "caso-juntas-01", "caso-juntas-02", "caso-juntas-03", "caso-juntas-04" };

        // 2 requerimientos por nivel de receptividad, en el dominio del caso 01.
        private static readonly Requerimiento[] RequerimientosEmbebidos =
        {
            new Requerimiento("equipos", Receptivity.NoReceptivo,
                new[] { "como identifican a cada equipo", "que datos manejan del equipo" },
                "Cada equipo se registra con su nombre y el ano de fundacion.", null, null),
            new Requerimiento("jugadores", Receptivity.NoReceptivo,
                new[] { "que datos manejan de cada jugador", "puede un jugador jugar en dos equipos" },
                "De cada jugador guardamos documento, nombre y posicion.", null, null),
            new Requerimiento("partidos", Receptivity.Neutral,
                new[] { "como arman un partido", "quien dirige cada partido" },
                "Un partido enfrenta a dos equipos, uno local y uno visitante.", null, null),
            new Requerimiento("estadio", Receptivity.Neutral,
                new[] { "que informacion registran del estadio", "un estadio tiene varios partidos" },
                "Cada estadio queda identificado por nombre, ciudad y capacidad.", null, null),
            new Requerimiento("arbitros", Receptivity.Receptivo,
                new[] { "como asignan los arbitros", "puede un arbitro pitar varios partidos" },
                "Cada arbitro tiene codigo de licencia y nombre.", null, null),
            new Requerimiento("presupuesto", Receptivity.Receptivo,
                new[] { "presupuesto del proyecto", "cuanto piensan invertir" },
                "El presupuesto del proyecto es de doscientos millones de pesos.",
                "reservado", "cruzar_brazos"),
        };

        private RequirementCaseId _caseId = RequirementCaseId.None;

        public bool IsReady => IdsConocidos.Contains(_caseId.Value ?? string.Empty);

        public void AssignCase(RequirementCaseId caseId, PersonalityId personality)
        {
            _caseId = caseId;
        }

        // "Core." es obligatorio aqui: el namespace del propio modulo (NpcAi.RequirementResponse)
        // choca con el nombre del DTO (NpcAi.Core.RequirementResponse) — mismo problema y misma
        // solucion que en M15 y que Core.Receptivity en NpcAi.Receptivity (M4).
        public Core.RequirementResponse Respond(Utterance studentUtterance, IntentResult intent, Receptivity receptivity)
        {
            if (!IsReady) return Core.RequirementResponse.NoAplica;

            var normalizado = RequirementMatcher.Normalizar(studentUtterance.Text);
            var indice = RequirementMatcher.Match(normalizado, RequerimientosEmbebidos);
            if (indice < 0) return Core.RequirementResponse.NoAplica;

            var requerimiento = RequerimientosEmbebidos[indice];
            var outcome = RequirementDisclosurePolicy.Decidir(receptivity, requerimiento.ReceptividadMinima);
            var texto = outcome == RequirementOutcome.Revelado ? requerimiento.Respuesta : Desvio;

            return new Core.RequirementResponse(
                outcome,
                new NpcReply(texto, requerimiento.EmotionTag, requerimiento.AnimationCue),
                new RequirementId(requerimiento.Id));
        }
    }
}
