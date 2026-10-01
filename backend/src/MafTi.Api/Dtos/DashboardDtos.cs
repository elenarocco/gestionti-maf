namespace MafTi.Api.Dtos;

public class ResumenDashboardDto
{
    public int DotacionActual { get; set; }
    public int IngresosDelMes { get; set; }
    public int BajasDelMes { get; set; }
    public int SolicitudesPendientes { get; set; }
    public int SolicitudesUrgentes { get; set; }
    public List<ResumenTipoDto> PorTipo { get; set; } = new();
    public List<MovimientoMesDto> Movimientos { get; set; } = new();
}

public class ResumenTipoDto
{
    public string Tipo { get; set; } = string.Empty;
    public int Cantidad { get; set; }
}

public class MovimientoMesDto
{
    public string Mes { get; set; } = string.Empty; // "2026-09-01"
    public int Ingresos { get; set; }
    public int Bajas { get; set; }
}