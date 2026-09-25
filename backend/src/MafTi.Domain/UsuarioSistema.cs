namespace MafTi.Domain;
using System.Text.Json.Serialization;

public class UsuarioSistema
{
    public int Id { get; set; }
    public int TrabajadorId { get; set; }
    [JsonIgnore]
    public Trabajador? Trabajador { get; set; }
    public int RolId { get; set; }
    [JsonIgnore]
    public Rol? Rol { get; set; }
    public int? AreaId { get; set; }
    [JsonIgnore]
    public Catalogo? Area { get; set; }
    public bool Activo { get; set; } = true;
}