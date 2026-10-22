namespace MafTi.Api.Dtos;

public class SolicitudBloqueoCreateDto
{
    public int TrabajadorId { get; set; }
    public bool EsTemporal { get; set; }
    public DateTime? Desde { get; set; }
    public DateTime? Hasta { get; set; }
    public string Justificacion { get; set; } = "";
    public bool? TienePc { get; set; }
    public bool? CasillaOpera { get; set; }
    public int? SolicitudOrigenId { get; set; } // si corrige una solicitud rechazada
}

public class SolicitudBloqueoResultadoDto
{
    public int SolicitudId { get; set; }
    public DateTime FechaVencimientoSLA { get; set; }
}
