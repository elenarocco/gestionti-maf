namespace MafTi.Api.Dtos;

public class FichaSolicitudDto
{
    public int Id { get; set; }
    public string Tipo { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public string CreadoPor { get; set; } = string.Empty;
    public DateTime FechaCreacion { get; set; }
}