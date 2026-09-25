namespace MafTi.Domain;
using System.Text.Json.Serialization;

public class Acceso
{
    public int Id { get; set; }
    public int TrabajadorId { get; set; }
    [JsonIgnore]
    public Trabajador? Trabajador { get; set; }
    public int CatalogoId { get; set; }
    [JsonIgnore]
    public Catalogo? Catalogo { get; set; }
    public int? SolicitudId { get; set; }
    [JsonIgnore]
    public Solicitud? Solicitud { get; set; }
    public DateTime FechaOtorgado { get; set; }
    public string Estado { get; set; } = "Activo";
}