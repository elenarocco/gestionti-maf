namespace MafTi.Api.Dtos;

public class ResumenSostenibilidadDto
{
    public DateOnly Desde { get; set; }               // fechas de Chile, ambas incluidas
    public DateOnly Hasta { get; set; }
    public bool EsEjemplo { get; set; }               // true = cantidades ficticias, no reales
    public string Alcance { get; set; } = "Empresa completa";
    public int TotalSolicitudes { get; set; }         // solicitudes completadas (digitalizadas) en el rango
    public List<SostenibilidadTipoDto> CantidadPorTipo { get; set; } = new();
    public int HojasEvitadas { get; set; }
    public decimal Co2PapelEvitadoGramos { get; set; }
    public decimal Co2SistemaGramos { get; set; }
    public decimal AhorroNetoGramos { get; set; }
    public SupuestosSostenibilidadDto Supuestos { get; set; } = new();
}

public class SostenibilidadTipoDto
{
    public string Tipo { get; set; } = string.Empty;  // Ingreso | Modificacion | Bloqueo | Vpn
    public int Cantidad { get; set; }
    public int HojasPorFormulario { get; set; }
    public int HojasEvitadas { get; set; }
    public decimal Co2PapelEvitadoGramos { get; set; }
}

public class SupuestosSostenibilidadDto
{
    public ParametrosSostenibilidadDto Parametros { get; set; } = new();
    public DateTime FechaActualizacion { get; set; }  // UTC
    public List<string> Formulas { get; set; } = new();
    public List<FuenteSostenibilidadDto> Fuentes { get; set; } = new();
}

public class FuenteSostenibilidadDto
{
    public string Parametro { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
}

public class ParametrosSostenibilidadDto
{
    public int HojasIngreso { get; set; }
    public int HojasModificacion { get; set; }
    public int HojasBloqueo { get; set; }
    public int HojasVpn { get; set; }
    public int CopiasImpresas { get; set; }
    public decimal GramosCO2PorHoja { get; set; }
    public decimal KbTransferidosPorSolicitud { get; set; }
    public decimal GramosCO2PorMB { get; set; }
}
