namespace NpcAi.SessionLog
{
    /// <summary>Normaliza la etiqueta de sesion (AD2): mismo criterio para ambos adaptadores.</summary>
    internal static class EtiquetaSesion
    {
        public const string PorDefecto = "sesion-sin-etiqueta";

        public static string Normalizar(string etiqueta) =>
            string.IsNullOrWhiteSpace(etiqueta) ? PorDefecto : etiqueta.Trim();
    }
}
