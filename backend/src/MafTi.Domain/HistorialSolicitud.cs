namespace MafTi.Domain;
using System.Text.Json.Serialization;
public class HistorialSolicitud
{
    public int Id { get; set; }
    public int SolicitudId { get; set; }
    [JsonIgnore]
    public Solicitud? Solicitud { get; set; }
    public string Accion { get; set; } = string.Empty;
    public int RealizadoPorId { get; set; }
    [JsonIgnore]
    public UsuarioSistema? RealizadoPor { get; set; }
    public DateTime Fecha { get; set; } = DateTime.UtcNow;
    public string? Comentario { get; set; }
}