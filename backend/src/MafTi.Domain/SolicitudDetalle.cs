namespace MafTi.Domain;
using System.Text.Json.Serialization;

public class SolicitudDetalle
{
    public int Id { get; set; }
    public int SolicitudId { get; set; }
    [JsonIgnore]
    public Solicitud? Solicitud { get; set; }
    public int CatalogoId { get; set; }
    [JsonIgnore]
    public Catalogo? Catalogo { get; set; }
}