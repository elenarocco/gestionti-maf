namespace MafTi.Application;

public static class SolicitudBloqueoValidator
{
    public static List<string> Validar(
        bool esTemporal, DateTime? desdeUtc, DateTime? hastaUtc, string? justificacion, DateTime ahoraUtc)
    {
        var errores = new List<string>();

        var ahoraChile = TimeZoneInfo.ConvertTimeFromUtc(ahoraUtc, HoraChile.Zona());
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

        errores.AddRange(JustificacionValidator.Validar(justificacion));

        return errores;
    }
}