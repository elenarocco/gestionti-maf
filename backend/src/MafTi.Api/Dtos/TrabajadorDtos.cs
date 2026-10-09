namespace MafTi.Api.Dtos;

public class TrabajadorListaDto
{
    public int Id { get; set; }
    public string Rut { get; set; } = string.Empty;
    public string PrimerNombre { get; set; } = string.Empty;
    public string PrimerApellido { get; set; } = string.Empty;
    public string Correo { get; set; } = string.Empty;
    public string Cargo { get; set; } = string.Empty;
    public bool Activo { get; set; }
    public string AreaNombre { get; set; } = string.Empty;
}

public class TrabajadorDetalleDto
{
    public int Id { get; set; }
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
    public bool EsCuentaGenerica { get; set; }
    public DateOnly FechaIncorporacion { get; set; }
    public string? DireccionDomicilio { get; set; }
    public string? JefeDirecto { get; set; }
    public string? HomologarAccesosDesde { get; set; }
    public bool TieneTelefonoCorporativo { get; set; }
    public bool SolicitaTelefono { get; set; }
    public bool Activo { get; set; }
}

public class TrabajadorCreateDto
{
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
    public bool EsCuentaGenerica { get; set; }
    public DateOnly FechaIncorporacion { get; set; }
    public DateOnly? FechaSalida { get; set; }
    public string? DireccionDomicilio { get; set; }
    public string? JefeDirecto { get; set; }
    public string? HomologarAccesosDesde { get; set; }
    public bool TieneTelefonoCorporativo { get; set; }
    public bool SolicitaTelefono { get; set; }

public class FichaSolicitudDto
{
    public int Id { get; set; }
    public string Tipo { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public string CreadoPor { get; set; } = string.Empty;
    public DateTime FechaCreacion { get; set; }
}

public class TrabajadorFichaDto
{
    public int Id { get; set; }
    public string Rut { get; set; } = string.Empty;
    public string NombreCompleto { get; set; } = string.Empty;
    public string Correo { get; set; } = string.Empty;
    public string Cargo { get; set; } = string.Empty;
    public string Area { get; set; } = string.Empty;
    public string DireccionCorporativa { get; set; } = string.Empty;
    public string LugarTrabajo { get; set; } = string.Empty;
    public DateOnly FechaIncorporacion { get; set; }
    public DateOnly? FechaSalida { get; set; }
    public bool Activo { get; set; }
    public List<string> Sistemas { get; set; } = new();
    public List<string> Carpetas { get; set; } = new();
    public List<FichaSolicitudDto> Solicitudes { get; set; } = new();
    public string AreaNombre { get; set; } = string.Empty;
    
}
}
public class TrabajadorAccesoDto
{
    public int CatalogoId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Tipo { get; set; } = string.Empty;
}
