namespace MafTi.Domain;

public class Acceso
{
    public int Id { get; set; }
    public int TrabajadorId { get; set; }
    public Trabajador? Trabajador { get; set; }
    public int CatalogoId { get; set; }
    public Catalogo? Catalogo { get; set; }
    public int? SolicitudId { get; set; }
    public Solicitud? Solicitud { get; set; }
    public DateTime FechaOtorgado { get; set; }
    public string Estado { get; set; } = "Activo";
}