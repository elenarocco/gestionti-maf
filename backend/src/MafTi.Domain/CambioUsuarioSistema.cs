namespace MafTi.Domain;
using System.Text.Json.Serialization;

// Bitácora de cambios de acceso: quién dio, cambió o quitó el acceso de un usuario, y cuándo.
public class CambioUsuarioSistema
{
    public int Id { get; set; }
    public int UsuarioSistemaId { get; set; }          // el usuario afectado
    [JsonIgnore]
    public UsuarioSistema? UsuarioSistema { get; set; }
    public int RealizadoPorId { get; set; }            // el administrador que hizo el cambio
    [JsonIgnore]
    public UsuarioSistema? RealizadoPor { get; set; }
    public DateTime Fecha { get; set; } = DateTime.UtcNow;
    public string Accion { get; set; } = string.Empty;  // Acceso otorgado | Acceso modificado | Acceso desactivado | Acceso reactivado
    public string Detalle { get; set; } = string.Empty; // p. ej. "Rol: Jefatura → Admin TI · Área: Riesgo → —"
}
