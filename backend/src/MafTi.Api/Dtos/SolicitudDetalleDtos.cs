namespace MafTi.Api.Dtos;

public class SolicitudDetalleListaDto
{
    public int Id { get; set; }
    public int SolicitudId { get; set; }
    public string CatalogoNombre { get; set; } = string.Empty;
}

public class SolicitudDetalleCreateDto
{
    public int SolicitudId { get; set; }
    public int CatalogoId { get; set; }
}