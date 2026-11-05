namespace MafTi.Infrastructure.Correo;

// Sección "Graph" de la configuración. Los secretos (TenantId, ClientId, ClientSecret) van en user-secrets
// en desarrollo y en variables de entorno en el servidor (Graph__ClientSecret, ...): nunca en appsettings.
public class OpcionesGraph
{
    public const string Seccion = "Graph";

    public bool Habilitado { get; set; }          // false: no se envía nada, solo se registra en el log
    public string TenantId { get; set; } = "";
    public string ClientId { get; set; } = "";
    public string ClientSecret { get; set; } = "";
    public string Buzon { get; set; } = "";       // casilla desde la que se envía (POST /users/{Buzon}/sendMail)

    // Nombres de las claves que faltan para poder enviar (vacío = todo listo).
    public IReadOnlyList<string> Faltantes() =>
        new (string Clave, string Valor)[] { ("TenantId", TenantId), ("ClientId", ClientId), ("ClientSecret", ClientSecret), ("Buzon", Buzon) }
            .Where(c => string.IsNullOrWhiteSpace(c.Valor))
            .Select(c => $"{Seccion}:{c.Clave}")
            .ToList();
}
