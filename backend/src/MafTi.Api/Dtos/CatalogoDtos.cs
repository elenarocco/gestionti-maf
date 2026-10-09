namespace MafTi.Api.Dtos;

public class CatalogoListaDto
{
    public int Id { get; set; }
    public string Tipo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public bool Activo { get; set; }
    public int? PadreId { get; set; }
}

public class CatalogoCreateDto
{
    public string Tipo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
}

public class CatalogoUpdateDto
{
    public string Nombre { get; set; } = string.Empty;
}