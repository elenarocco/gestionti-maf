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
public class TrabajadorAccesoDto
{
    public int CatalogoId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Tipo { get; set; } = string.Empty;
}
