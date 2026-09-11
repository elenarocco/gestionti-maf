namespace MafTi.Domain;

public class Solicitud
{
    public int Id { get; set; }
    public int TrabajadorId { get; set; }
    public Trabajador? Trabajador { get; set; }
    public int CreadoPorId { get; set; }
    public UsuarioSistema? CreadoPor { get; set; }
    public string Tipo { get; set; } = string.Empty;
    public string Estado { get; set; } = "Pendiente";
    public string? MotivoRechazo { get; set; }
    public int? SolicitudOrigenId { get; set; }
    public Solicitud? SolicitudOrigen { get; set; }
    public DateTime FechaCreacion { get; set; } = DateTime.Now;
    public DateTime FechaVencimientoSLA { get; set; }
}