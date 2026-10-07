namespace MafTi.Api.Dtos;

public class TrabajadorFichaDto
{
    public int Id { get; set; }
    public string Rut { get; set; } = string.Empty;
    public string NombreCompleto { get; set; } = string.Empty;
    public string Correo { get; set; } = string.Empty;
    public string Cargo { get; set; } = string.Empty;
    public string Area { get; set; } = string.Empty;
    public string DireccionCorporativa { get; set; } = string.Empty;
    public string LugarTrabajo { get; set; } = string.Empty;
    public DateOnly FechaIncorporacion { get; set; }
    public DateOnly? FechaSalida { get; set; }
    public bool Activo { get; set; }
    public List<string> Sistemas { get; set; } = new();
    public List<string> Carpetas { get; set; } = new();
    public List<FichaSolicitudDto> Solicitudes { get; set; } = new();
}