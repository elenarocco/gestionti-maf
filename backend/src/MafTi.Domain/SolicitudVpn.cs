using System.Text.Json.Serialization;

namespace MafTi.Domain;

// Fase 1: Jefatura elige el motivo. Fase 2: TI aprueba y habilita la ficha.
// Fase 3: Jefatura completa la ficha (campos nulos hasta entonces) y TI la envía al proveedor.
public class SolicitudVpn
{
    public int Id { get; set; }
    public int SolicitudId { get; set; }
    [JsonIgnore] public Solicitud? Solicitud { get; set; }

    public int MotivoId { get; set; }
    [JsonIgnore] public Catalogo? Motivo { get; set; }

    public bool FormularioHabilitado { get; set; }
    public DateTime? FechaEnvioProveedor { get; set; }   // UTC

    // Ficha de habilitación
    public DateOnly? FechaIncorporacion { get; set; }
    public DateOnly? FechaCaducidad { get; set; }
    public string? Rut { get; set; }
    public string? Cargo { get; set; }
    public string? Nombres { get; set; }
    public string? Apellidos { get; set; }
    public string? Departamento { get; set; }
    public string? UbicacionFisica { get; set; }
    public bool? EquipoCorporativo { get; set; }
    public string? CompaniaExterna { get; set; }   // solo si EquipoCorporativo = false
    public string? CuentaCorreo { get; set; }
    public string? Justificacion { get; set; }
}
