namespace MafTi.Api.Dtos;

public class UsuarioSistemaListaDto
{
    public int Id { get; set; }
    public string TrabajadorNombre { get; set; } = string.Empty;
    public string TrabajadorCorreo { get; set; } = string.Empty;
    public string RolNombre { get; set; } = string.Empty;
    public string? AreaNombre { get; set; }
    public bool Activo { get; set; }
}

public class UsuarioSistemaCreateDto
{
    public int TrabajadorId { get; set; }
    public int RolId { get; set; }
    public int? AreaId { get; set; }
}