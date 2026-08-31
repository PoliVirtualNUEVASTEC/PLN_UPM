namespace NpcAi.Core
{
    /// <summary>
    /// Que quiso hacer el usuario con lo que dijo. Producido por M2.
    /// <c>Desconocida = 0</c> a proposito: un valor sin inicializar nunca significa algo fuerte.
    /// </summary>
    public enum Intent
    {
        Desconocida        = 0,
        SolicitudRespetuosa = 1,
        SolicitudAgresiva   = 2,
        Empatia             = 3,
        AportaInformacion   = 4,
        PreguntaFueraDeTema = 5,
        Interrupcion        = 6,
    }

    /// <summary>Como lo dijo. Producido por M2, ortogonal a <see cref="Intent"/>.</summary>
    public enum Tone
    {
        Neutral     = 0,
        Respetuoso  = 1,
        Agresivo    = 2,
        Empatico    = 3,
        Ansioso     = 4,
    }

    /// <summary>Que hizo el usuario con el cuerpo o los controles VR. Producido por M7.</summary>
    public enum PhysicalAction
    {
        Ninguna         = 0,
        ContactoVisual  = 1,
        Acercarse       = 2,
        Alejarse        = 3,
        EntregarObjeto  = 4,
        SenalarPantalla = 5,
        GestoCalma      = 6,
        TocarPaciente   = 7,
    }

    /// <summary>
    /// Estado del NPC frente al usuario. Numerado -1/0/1 a proposito: el orden es
    /// comparable, de modo que una transicion se puede probar aritmeticamente
    /// (<c>(int)To &gt; (int)From</c> es una mejora).
    /// </summary>
    public enum Receptivity
    {
        NoReceptivo = -1,
        Neutral     =  0,
        Receptivo   =  1,
    }
}
