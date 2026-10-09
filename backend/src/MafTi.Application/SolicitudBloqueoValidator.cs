using System.Text.RegularExpressions;

namespace MafTi.Application;

public static class SolicitudBloqueoValidator
{
    private static TimeZoneInfo ZonaChile()
    {
        try { return TimeZoneInfo.FindSystemTimeZoneById("America/Santiago"); }
        catch { return TimeZoneInfo.FindSystemTimeZoneById("Pacific SA Standard Time"); }
    }

    public static List<string> Validar(
        bool esTemporal, DateTime? desdeUtc, DateTime? hastaUtc, string? justificacion, DateTime ahoraUtc)
    {
        var errores = new List<string>();

        var ahoraChile = TimeZoneInfo.ConvertTimeFromUtc(ahoraUtc, ZonaChile());
        if (ahoraChile.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
            errores.Add("Las solicitudes de bloqueo solo se pueden enviar de lunes a viernes.");

        if (esTemporal)
        {
            if (desdeUtc == null || hastaUtc == null)
                errores.Add("El bloqueo temporal requiere fecha y hora de inicio y de término.");
            else
            {
                if (desdeUtc < ahoraUtc.AddMinutes(-1))
                    errores.Add("La fecha de inicio no puede estar en el pasado.");
                if (hastaUtc <= desdeUtc)
                    errores.Add("La fecha de término debe ser posterior a la de inicio.");
            }
        }

        var j = justificacion?.Trim() ?? "";
        if (j.Length == 0)
            errores.Add("La justificación es obligatoria.");
        else if (j.Length > 200)
            errores.Add("La justificación no puede superar los 200 caracteres.");
        else if (!Regex.IsMatch(j, @"^[\p{L}\s,.]+$"))
            errores.Add("La justificación solo puede contener letras, espacios, comas y puntos.");

        return errores;
    }
}