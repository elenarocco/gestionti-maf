namespace MafTi.Api.Dtos;

public class PermisoListaDto
{
    public int Id { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
}

public class PermisoCreateDto
{
    public string Codigo { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
}