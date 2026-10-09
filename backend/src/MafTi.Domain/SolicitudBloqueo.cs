using System.Text.Json.Serialization;

namespace MafTi.Domain;

public class SolicitudBloqueo
{
    public int Id { get; set; }
    public int SolicitudId { get; set; }
    [JsonIgnore] public Solicitud? Solicitud { get; set; }

    public bool EsTemporal { get; set; }
    public DateTime FechaInicio { get; set; }   // UTC
    public DateTime? FechaFin { get; set; }     // UTC, solo si es temporal
    public string Justificacion { get; set; } = "";
    public bool? TienePc { get; set; }
    public bool? CasillaOpera { get; set; }
}