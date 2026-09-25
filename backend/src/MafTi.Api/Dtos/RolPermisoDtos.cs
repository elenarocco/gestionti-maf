namespace MafTi.Api.Dtos;

public class RolPermisoListaDto
{
    public int Id { get; set; }
    public string RolNombre { get; set; } = string.Empty;
    public string PermisoCodigo { get; set; } = string.Empty;
}

public class RolPermisoCreateDto
{
    public int RolId { get; set; }
    public int PermisoId { get; set; }
}