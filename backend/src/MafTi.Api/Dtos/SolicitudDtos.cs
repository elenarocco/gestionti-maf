namespace MafTi.Api.Dtos;

public class SolicitudListaDto
{
    public int Id {get; set;}
    public string TrabajadorNombre {get; set;} = string.Empty;
    public string Tipo { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public DateTime FechaCreacion { get; set; }
    public DateTime FechaVencimientoSLA { get; set; }

}

public class SolicitudDetalleDto
{
    public int Id { get; set; }
    public int TrabajadorId { get; set; }
    public string TrabajadorNombre { get; set; } = string.Empty;
    public int CreadoPorId { get; set; }
    public string CreadoPorCorreo { get; set; } = string.Empty;
    public string Tipo { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public string? MotivoRechazo { get; set; }
    public int? SolicitudOrigenId { get; set; }
    public DateTime FechaCreacion { get; set; }
    public DateTime FechaVencimientoSLA { get; set; }
}

public class SolicitudCreateDto
{
    public int TrabajadorId { get; set; }
    public int CreadoPorId { get; set; }
    public string Tipo { get; set; } = string.Empty;
}