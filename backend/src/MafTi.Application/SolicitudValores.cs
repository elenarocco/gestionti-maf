namespace MafTi.Application;

// Valores de tipo y estado. En la BD se guardan como siempre ("Modificación", "VPN", "En proceso")
// y la API de /api/solicitudes expone los del contrato ("Modificacion", "Vpn", "EnProceso").
public static class SolicitudValores
{
    public const string Pendiente = "Pendiente";
    public const string EnProceso = "En proceso";
    public const string Completada = "Completada";
    public const string Rechazada = "Rechazada";

    public const string TipoIngreso = "Ingreso";
    public const string TipoModificacion = "Modificación";
    public const string TipoBloqueo = "Bloqueo";
    public const string TipoVpn = "VPN";

    private static readonly Dictionary<string, string> TipoApiADb = new(StringComparer.OrdinalIgnoreCase)
    {
        { "Ingreso", TipoIngreso }, { "Modificacion", TipoModificacion }, { "Bloqueo", TipoBloqueo }, { "Vpn", TipoVpn }
    };

    private static readonly Dictionary<string, string> EstadoApiADb = new(StringComparer.OrdinalIgnoreCase)
    {
        { "Pendiente", Pendiente }, { "EnProceso", EnProceso }, { "Completada", Completada }, { "Rechazada", Rechazada }
    };

    // null si el valor no es válido.
    public static string? TipoADb(string tipoApi) => TipoApiADb.GetValueOrDefault(tipoApi.Trim());
    public static string? EstadoADb(string estadoApi) => EstadoApiADb.GetValueOrDefault(estadoApi.Trim());

    public static string TipoAApi(string tipoDb) =>
        TipoApiADb.FirstOrDefault(p => p.Value == tipoDb).Key ?? tipoDb;

    public static string EstadoAApi(string estadoDb) =>
        EstadoApiADb.FirstOrDefault(p => p.Value == estadoDb).Key ?? estadoDb;

    // El SLA solo corre mientras la solicitud está abierta.
    public static bool EstaVencida(string estadoDb, DateTime vencimientoUtc, DateTime ahoraUtc) =>
        (estadoDb == Pendiente || estadoDb == EnProceso) && vencimientoUtc < ahoraUtc;
}
