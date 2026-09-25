namespace MafTi.Api.Dtos;

public class RolListaDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
}

public class RolCreateDto
{
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
}