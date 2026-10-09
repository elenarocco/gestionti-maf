namespace MafTi.Api.Dtos;

public class SolicitudModificacionCreateDto
{
    public int TrabajadorId { get; set; }
    public int CreadoPorId { get; set; }
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
}

public class SolicitudModificacionResultadoDto
{
    public int SolicitudId { get; set; }
    public DateTime FechaVencimientoSLA { get; set; }
}

public class SolicitudModificacionAccesoDto
{
    public int CatalogoId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Tipo { get; set; } = string.Empty;
}

public class SolicitudModificacionDetalleDto
{
    public int Id { get; set; }
    public int SolicitudId { get; set; }
    public string Justificacion { get; set; } = string.Empty;
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
    public List<SolicitudModificacionAccesoDto> AccesosAgregar { get; set; } = new();
    public List<SolicitudModificacionAccesoDto> AccesosQuitar { get; set; } = new();
}
