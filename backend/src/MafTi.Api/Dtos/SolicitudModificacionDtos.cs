namespace MafTi.Api.Dtos;

public class SolicitudModificacionCreateDto
{
    public int TrabajadorId { get; set; }
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
    public string Justificacion { get; set; } = "";
    public List<int> CatalogoIdsAgregar { get; set; } = new();
    public List<int> CatalogoIdsQuitar { get; set; } = new();
    public int? SolicitudOrigenId { get; set; } // si corrige una solicitud rechazada
}

public class SolicitudModificacionResultadoDto
{
    public int SolicitudId { get; set; }
    public DateTime FechaVencimientoSLA { get; set; }
}
