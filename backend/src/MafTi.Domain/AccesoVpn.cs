using System.Text.Json.Serialization;

namespace MafTi.Domain;

// Acceso VPN otorgado a un proveedor externo. Se registra cuando TI confirma el envío de la ficha al
// proveedor (la solicitud VPN queda Completada). Vigente mientras no pase FechaHasta.
public class AccesoVpn
{
    public int Id { get; set; }
    public int SolicitudId { get; set; }
    [JsonIgnore] public Solicitud? Solicitud { get; set; }

    public string Rut { get; set; } = string.Empty;
    public string Nombres { get; set; } = string.Empty;
    public string Apellidos { get; set; } = string.Empty;
    public string? Cargo { get; set; }
    public string? CompaniaExterna { get; set; }
    public string? Departamento { get; set; }
    public string? CuentaCorreo { get; set; }
    public string Motivo { get; set; } = string.Empty;   // nombre del motivo de la VPN
    public DateOnly? FechaDesde { get; set; }
    public DateOnly? FechaHasta { get; set; }
    public DateTime FechaRegistro { get; set; } = DateTime.UtcNow;
}
