namespace MafTi.Api.Dtos;

// Plazo que tendría una solicitud creada ahora: lo muestra el formulario antes de enviarla.
public class PlazoSlaDto
{
    public string Regla { get; set; } = string.Empty;    // "3 días hábiles (...)", "48 horas hábiles"...
    public DateTime Inicio { get; set; }                  // desde cuándo cuenta (UTC)
    public DateTime Vence { get; set; }                   // cuándo vence (UTC)
    public string Mensaje { get; set; } = string.Empty;  // texto listo para mostrar, en hora de Chile
}
