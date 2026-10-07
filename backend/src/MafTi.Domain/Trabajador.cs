using System.Text.Json.Serialization;

namespace MafTi.Domain;

public class Trabajador
{
    public int Id { get; set; }
    
    [JsonIgnore]
    public Catalogo? DireccionCorporativa { get; set; }

    [JsonIgnore]
    public Catalogo? Area { get; set; }

    [JsonIgnore]
    public Catalogo? LugarTrabajo { get; set; }
    
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
    public int LugarTrabajoId { get; set; }      
    public int CargoId { get; set; }
    [JsonIgnore]
    public Catalogo? Cargo { get; set; }     

    public bool EsCuentaGenerica { get; set; }
    public DateOnly FechaIncorporacion { get; set; }
    public DateOnly? FechaSalida { get; set; }
    public string? DireccionDomicilio { get; set; }
    public string? JefeDirecto { get; set; }
    public string? HomologarAccesosDesde { get; set; }
    public bool TieneTelefonoCorporativo { get; set; }
    public bool SolicitaTelefono { get; set; }

    public bool Activo { get; set; } = true; 
}