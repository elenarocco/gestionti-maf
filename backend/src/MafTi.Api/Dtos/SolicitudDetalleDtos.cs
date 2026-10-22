namespace MafTi.Api.Dtos;

public class SolicitudDetalleListaDto
{
    public int Id { get; set; }
    public int SolicitudId { get; set; }
    public string CatalogoNombre { get; set; } = string.Empty;
}
