using System;
using System.Collections.Generic;
using NpcAi.Core;

namespace NpcAi.RequirementResponse
{
    /// <summary>
    /// Caso de sala de juntas ya cargado y validado, listo para que el respondedor de
    /// requerimientos (Fase 3 de este cambio) lo consulte. Espejo de
    /// <c>ClinicalCase</c> (M14/M15) adaptado al esquema de requerimientos: <see cref="Cliente"/>
    /// en vez de <c>Paciente</c>, <see cref="Requerimiento"/> en vez de <c>Hecho</c>, y una
    /// puerta de receptividad propia por requerimiento (<see cref="Requerimiento.ReceptividadMinima"/>)
    /// que el caso clinico no necesita.
    /// </summary>
    public sealed class RequirementCase
    {
        public string Id { get; }
        public Cliente Cliente { get; }
        public IReadOnlyList<Requerimiento> Requerimientos { get; }

        public RequirementCase(string id, Cliente cliente, IReadOnlyList<Requerimiento> requerimientos)
        {
            Id             = id ?? string.Empty;
            Cliente        = cliente;
            Requerimientos = requerimientos ?? Array.Empty<Requerimiento>();
        }
    }

    /// <summary>
    /// El cliente de sala de juntas del caso: quien pidio el proyecto, no quien lo construye.
    /// Solo dato descriptivo, ningun campo aqui gatea nada (esa es responsabilidad exclusiva
    /// de <see cref="Requerimiento.ReceptividadMinima"/>).
    /// </summary>
    public sealed class Cliente
    {
        public string Empresa { get; }
        public string Rol { get; }
        public string Proyecto { get; }
        public string Contexto { get; }

        public Cliente(string empresa, string rol, string proyecto, string contexto)
        {
            Empresa  = empresa  ?? string.Empty;
            Rol      = rol      ?? string.Empty;
            Proyecto = proyecto ?? string.Empty;
            Contexto = contexto ?? string.Empty;
        }
    }

    /// <summary>
    /// Una entrada de la tabla de requerimientos: el contrato entre el catalogo de datos de
    /// M16 y su respondedor. <see cref="RequirementMatcher"/> (Fase 2) compara
    /// <see cref="EjemplosDePregunta"/> contra lo que pregunta el estudiante; si empareja,
    /// <see cref="RequirementDisclosurePolicy"/> (Fase 2) decide si <see cref="Respuesta"/> sale
    /// tal cual o si el turno se desvia, comparando la receptividad actual contra
    /// <see cref="ReceptividadMinima"/> (AD1). <see cref="EmotionTag"/> y
    /// <see cref="AnimationCue"/> son opcionales en el JSON: ausentes, caen en los defaults
    /// "neutral" / "idle" (AD8) para que agregar un caso nuevo nunca obligue a declararlos.
    /// </summary>
    public sealed class Requerimiento
    {
        public string Id { get; }
        public Receptivity ReceptividadMinima { get; }
        public IReadOnlyList<string> EjemplosDePregunta { get; }
        public string Respuesta { get; }
        public string EmotionTag { get; }
        public string AnimationCue { get; }

        public Requerimiento(
            string id, Receptivity receptividadMinima, IReadOnlyList<string> ejemplosDePregunta,
            string respuesta, string emotionTag, string animationCue)
        {
            Id                 = id ?? string.Empty;
            ReceptividadMinima = receptividadMinima;
            EjemplosDePregunta = ejemplosDePregunta ?? Array.Empty<string>();
            Respuesta          = respuesta ?? string.Empty;
            EmotionTag         = string.IsNullOrWhiteSpace(emotionTag) ? "neutral" : emotionTag;
            AnimationCue       = string.IsNullOrWhiteSpace(animationCue) ? "idle" : animationCue;
        }
    }
}
