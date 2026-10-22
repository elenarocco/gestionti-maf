namespace MafTi.Domain;
using System.Text.Json.Serialization;

// Cada reporte descargado (Excel o PDF): quién, qué, con qué filtros y cuántas filas.
// Son datos de personas, así que queda constancia de quién se los llevó.
public class RegistroDescarga
{
    public int Id { get; set; }
    public int UsuarioSistemaId { get; set; }
    [JsonIgnore]
    public UsuarioSistema? UsuarioSistema { get; set; }
    public DateTime Fecha { get; set; } = DateTime.UtcNow;
    public string Reporte { get; set; } = string.Empty;   // Trabajadores | Solicitudes
    public string Formato { get; set; } = string.Empty;   // Excel | PDF
    public string Filtros { get; set; } = string.Empty;   // como se muestran en el encabezado del reporte
    public int Filas { get; set; }
}
