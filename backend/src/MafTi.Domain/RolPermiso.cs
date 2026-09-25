namespace MafTi.Domain;
using System.Text.Json.Serialization;

public class RolPermiso
{
    public int Id { get; set; }
    public int RolId { get; set; }
    [JsonIgnore]    public Rol? Rol { get; set; }
    public int PermisoId { get; set; }
    [JsonIgnore]
    public Permiso? Permiso { get; set; }
}