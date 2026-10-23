using System.Globalization;
using static MafTi.Application.SolicitudValores;

namespace MafTi.Application;

// Plazo (SLA) de cada tipo de solicitud, en hora de Chile y solo en días hábiles (lunes a viernes sin feriados):
//   Ingreso       3 días hábiles. Cuenta el día del envío si es hábil y antes de las 12:00; si no, desde el
//                 siguiente día hábil. Vence al final (23:59) del 3.er día.
//   Bloqueo       el mismo día si es hábil; si no, el siguiente día hábil. Vence a las 23:59.
//   Modificación  48 horas; el reloj solo corre en días hábiles (viernes 15:00 → martes 15:00).
//   VPN           igual que Modificación.
public static class CalculadoraSla
{
    public static readonly TimeSpan CorteIngreso = new(12, 0, 0);
    public const int DiasIngreso = 3;
    public const int HorasModificacionYVpn = 48;

    public record Plazo(DateTime InicioUtc, DateTime VenceUtc, string Regla, string Mensaje);

    public static bool EsHabil(DateOnly dia, ISet<DateOnly> feriados) =>
        dia.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday) && !feriados.Contains(dia);

    public static DateOnly SiguienteHabil(DateOnly dia, ISet<DateOnly> feriados)
    {
        do dia = dia.AddDays(1); while (!EsHabil(dia, feriados));
        return dia;
    }

    public static Plazo Calcular(string tipoDb, DateTime creacionUtc, ISet<DateOnly> feriados)
    {
        var zona = HoraChile.Zona();
        var local = TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(creacionUtc, DateTimeKind.Utc), zona);
        var hoy = DateOnly.FromDateTime(local);

        DateTime inicio, vence;   // hora de Chile
        string regla;
        switch (tipoDb)
        {
            case TipoIngreso:
            {
                var dia = EsHabil(hoy, feriados) && local.TimeOfDay < CorteIngreso ? hoy : SiguienteHabil(hoy, feriados);
                inicio = dia.ToDateTime(TimeOnly.MinValue);
                for (var i = 1; i < DiasIngreso; i++) dia = SiguienteHabil(dia, feriados);
                vence = FinDelDia(dia);
                regla = $"{DiasIngreso} días hábiles (si se envía después de las 12:00, cuenta desde el día hábil siguiente)";
                break;
            }
            case TipoBloqueo:
            {
                var dia = EsHabil(hoy, feriados) ? hoy : SiguienteHabil(hoy, feriados);
                inicio = dia == hoy ? local : dia.ToDateTime(TimeOnly.MinValue);
                vence = FinDelDia(dia);
                regla = "el mismo día hábil";
                break;
            }
            case TipoModificacion or TipoVpn:
            {
                // El reloj parte ahora si es día hábil, o a las 00:00 del siguiente día hábil.
                var cursor = EsHabil(hoy, feriados) ? local : SiguienteHabil(hoy, feriados).ToDateTime(TimeOnly.MinValue);
                inicio = cursor;
                var restante = TimeSpan.FromHours(HorasModificacionYVpn);
                while (true)
                {
                    var finDelDia = cursor.Date.AddDays(1);
                    var disponible = finDelDia - cursor;
                    if (restante <= disponible) { vence = cursor + restante; break; }
                    restante -= disponible;
                    cursor = SiguienteHabil(DateOnly.FromDateTime(cursor), feriados).ToDateTime(TimeOnly.MinValue);
                }
                // Vencer justo a las 00:00 es lo mismo que al final del día anterior: se muestra así.
                if (vence.TimeOfDay == TimeSpan.Zero) vence = vence.AddSeconds(-1);
                regla = $"{HorasModificacionYVpn} horas hábiles";
                break;
            }
            default:
                throw new ArgumentException($"Tipo de solicitud sin SLA: {tipoDb}", nameof(tipoDb));
        }

        var mensaje = $"Plazo: {regla}. Empieza a contar el {Fecha(inicio, conHora: tipoDb is TipoModificacion or TipoVpn)} " +
                      $"y vence el {Fecha(vence, conHora: true)}.";
        return new Plazo(AUtc(inicio, zona), AUtc(vence, zona), regla, mensaje);
    }

    private static DateTime FinDelDia(DateOnly dia) => dia.ToDateTime(new TimeOnly(23, 59, 59));

    private static DateTime AUtc(DateTime local, TimeZoneInfo zona) =>
        TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(local, DateTimeKind.Unspecified), zona);

    private static readonly CultureInfo Es = CultureInfo.GetCultureInfo("es-CL");

    private static string Fecha(DateTime local, bool conHora) =>
        local.ToString(conHora ? "dddd dd-MM-yyyy 'a las' HH:mm" : "dddd dd-MM-yyyy", Es);
}
