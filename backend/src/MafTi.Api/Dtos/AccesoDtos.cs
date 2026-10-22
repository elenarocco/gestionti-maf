namespace MafTi.Api.Dtos;

public class AccesoListaDto
{
    public int Id { get; set; }
    public string TrabajadorNombre { get; set; } = string.Empty;
    public string CatalogoNombre { get; set; } = string.Empty;
    public DateTime FechaOtorgado { get; set; }
    public string Estado { get; set; } = string.Empty;
}
