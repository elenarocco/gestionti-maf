using System.Text.Json.Serialization;

namespace MafTi.Domain;

// Datos del trabajador que se pide ingresar. El trabajador NO existe en Trabajadores hasta que TI completa
// la solicitud: ahí se crea con estos datos. Si se rechaza, nunca llega a la base.
public class SolicitudIngreso
{
    public int Id { get; set; }
    public int SolicitudId { get; set; }
    [JsonIgnore] public Solicitud? Solicitud { get; set; }

    public string Rut { get; set; } = string.Empty;
    public string PrimerNombre { get; set; } = string.Empty;
    public string? SegundoNombre { get; set; }
    public string PrimerApellido { get; set; } = string.Empty;
    public string? SegundoApellido { get; set; }
    public DateOnly FechaNacimiento { get; set; }
    public string Sexo { get; set; } = string.Empty;
    public string Correo { get; set; } = string.Empty;
    public int DireccionCorporativaId { get; set; }
    public int AreaId { get; set; }
    public int CargoId { get; set; }
    public int LugarTrabajoId { get; set; }
    public DateOnly FechaIncorporacion { get; set; }
    public string? DireccionDomicilio { get; set; }
    public string? JefeDirecto { get; set; }
    public string? HomologarAccesosDesde { get; set; }
    public bool TieneTelefonoCorporativo { get; set; }
    public bool SolicitaTelefono { get; set; }
}
