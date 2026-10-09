namespace MafTi.Api.Dtos;

public class SolicitudBloqueoCreateDto
{
    public int TrabajadorId { get; set; }
    public int CreadoPorId { get; set; }
    public bool EsTemporal { get; set; }
    public DateTime? Desde { get; set; }
    public DateTime? Hasta { get; set; }
    public string Justificacion { get; set; } = "";
    public bool? TienePc { get; set; }
    public bool? CasillaOpera { get; set; }
}

public class SolicitudBloqueoResultadoDto
{
    public int SolicitudId { get; set; }
    public DateTime FechaVencimientoSLA { get; set; }
}

public class SolicitudBloqueoDetalleDto
{
    public int Id { get; set; }
    public int SolicitudId { get; set; }
    public bool EsTemporal { get; set; }
    public DateTime FechaInicio { get; set; }
    public DateTime? FechaFin { get; set; }
    public string Justificacion { get; set; } = string.Empty;
    public bool? TienePc { get; set; }
    public bool? CasillaOpera { get; set; }
}
