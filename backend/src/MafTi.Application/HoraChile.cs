namespace MafTi.Application;

public static class HoraChile
{
    public static TimeZoneInfo Zona()
    {
        try { return TimeZoneInfo.FindSystemTimeZoneById("America/Santiago"); }
        catch { return TimeZoneInfo.FindSystemTimeZoneById("Pacific SA Standard Time"); }
    }

    // Último segundo (23:59:59) del día en curso en Chile, expresado en UTC.
    public static DateTime FinDelDiaUtc(DateTime ahoraUtc)
    {
        var zona = Zona();
        var hoyChile = TimeZoneInfo.ConvertTimeFromUtc(ahoraUtc, zona).Date;
        var finDelDia = DateTime.SpecifyKind(hoyChile.AddDays(1).AddSeconds(-1), DateTimeKind.Unspecified);
        return TimeZoneInfo.ConvertTimeToUtc(finDelDia, zona);
    }
}
