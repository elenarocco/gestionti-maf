namespace MafTi.Api.Dtos;

public class HistorialSolicitudListaDto
{
    public int Id { get; set; }
    public int SolicitudId { get; set; }
    public string Accion { get; set; } = string.Empty;
    public string RealizadoPorCorreo { get; set; } = string.Empty;
    public DateTime Fecha { get; set; }
    public string? Comentario { get; set; }
}

public class HistorialSolicitudCreateDto
{
    public int SolicitudId { get; set; }
    public string Accion { get; set; } = string.Empty;
    public int RealizadoPorId { get; set; }
    public string? Comentario { get; set; }
}