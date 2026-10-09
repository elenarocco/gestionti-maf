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