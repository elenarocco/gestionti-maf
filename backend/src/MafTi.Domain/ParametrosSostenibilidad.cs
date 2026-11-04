namespace MafTi.Domain;

// Supuestos del módulo Sostenibilidad (computación verde). Es una sola fila (Id = 1) que edita
// Gestión General desde la pantalla; los valores iniciales y sus fuentes están en la migración
// AgregarSostenibilidad. Son ESTIMACIONES, no mediciones.
public class ParametrosSostenibilidad
{
    public const int IdUnico = 1;

    public int Id { get; set; }

    // Hojas que tendría el formulario en papel de cada tipo de solicitud.
    public int HojasIngreso { get; set; }
    public int HojasModificacion { get; set; }
    public int HojasBloqueo { get; set; }
    public int HojasVpn { get; set; }

    public int CopiasImpresas { get; set; }

    // g CO2e por hoja A4 (ciclo de vida del papel).
    public decimal GramosCO2PorHoja { get; set; }

    // KB que transfiere el flujo completo de una solicitud en el sistema (login, crear, Bandeja de TI, completar).
    public decimal KbTransferidosPorSolicitud { get; set; }

    // g CO2e por MB transferido (modelo Sustainable Web Design). 1 MB = 1.000 KB.
    public decimal GramosCO2PorMB { get; set; }

    public DateTime FechaActualizacion { get; set; }   // UTC
}
