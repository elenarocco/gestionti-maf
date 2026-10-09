using System.Text.Json.Serialization;

namespace MafTi.Domain;

public class Catalogo
{
    public int Id { get; set; }
    public string Tipo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public bool Activo { get; set; } = true;

    // Para las áreas: la dirección corporativa a la que pertenecen (vacío en los demás catálogos)
    public int? PadreId { get; set; }
    [JsonIgnore]
    public Catalogo? Padre { get; set; }
}