namespace MafTi.Api.Dtos;

// Contrato de /api/solicitudes (Bandeja de TI, VPN y "mis solicitudes").
// Tipos: Ingreso | Modificacion | Bloqueo | Vpn. Estados: Pendiente | EnProceso | Completada | Rechazada.

public class PaginaDto<T>
{
    public List<T> Items { get; set; } = new();
    public int Total { get; set; }
    public int Pagina { get; set; }
    public int Tamano { get; set; }
}

public class SolicitudBandejaDto
{
    public int Id { get; set; }
    public string Tipo { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public string? TrabajadorNombre { get; set; }
    public string? AreaNombre { get; set; }           // área del trabajador; en VPN, el departamento de la ficha
    public string CreadoPorNombre { get; set; } = string.Empty;
    public DateTime FechaCreacion { get; set; }
    public DateTime FechaVencimientoSLA { get; set; }
    public bool Vencida { get; set; }
    public int? TrabajadorId { get; set; }
    public string? MotivoRechazo { get; set; }        // justificación del rechazo (la ve quien la creó)
    public string? MotivoVpn { get; set; }            // solo VPN
    public List<string> AccionesPermitidas { get; set; } = new();   // del creador: "corregir", "completarFicha"
}

public class HistorialItemDto
{
    public string Accion { get; set; } = string.Empty;
    public string RealizadoPorNombre { get; set; } = string.Empty;
    public DateTime Fecha { get; set; }
    public string? Comentario { get; set; }
}

public class FichaVpnDto
{
    public DateOnly? FechaIncorporacion { get; set; }
    public DateOnly? FechaCaducidad { get; set; }
    public string? Rut { get; set; }
    public string? Cargo { get; set; }
    public string? Nombres { get; set; }
    public string? Apellidos { get; set; }
    public string? Departamento { get; set; }
    public string? UbicacionFisica { get; set; }
    public bool? EquipoCorporativo { get; set; }
    public string? CompaniaExterna { get; set; }   // obligatoria si EquipoCorporativo = false
    public string? CuentaCorreo { get; set; }
    public string? Motivo { get; set; }            // máx. 200
}

public class VpnResumenDto
{
    public int MotivoId { get; set; }
    public string MotivoNombre { get; set; } = string.Empty;
    public bool FormularioHabilitado { get; set; }
    public DateTime? FechaEnvioProveedor { get; set; }
    public FichaVpnDto? Ficha { get; set; }
}

public class SolicitudBandejaDetalleDto
{
    public int Id { get; set; }
    public string Tipo { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public string? MotivoRechazo { get; set; }
    public DateTime FechaCreacion { get; set; }
    public DateTime FechaVencimientoSLA { get; set; }
    public bool Vencida { get; set; }
    public string CreadoPorNombre { get; set; } = string.Empty;
    public string? TrabajadorNombre { get; set; }
    public Dictionary<string, object?>? Detalle { get; set; }   // campos según el tipo
    public VpnResumenDto? Vpn { get; set; }
    public List<HistorialItemDto> Historial { get; set; } = new();
    public List<string> AccionesPermitidas { get; set; } = new();
}

public class JustificacionRechazoDto
{
    public string? Justificacion { get; set; }
}

public class CrearSolicitudVpnDto
{
    public int MotivoId { get; set; }
}

public class CreadaDto
{
    public int Id { get; set; }
}

public class SolicitudVpnDetalleDto
{
    public int Id { get; set; }
    public string Estado { get; set; } = string.Empty;
    public int MotivoId { get; set; }
    public string MotivoNombre { get; set; } = string.Empty;
    public bool FormularioHabilitado { get; set; }
    public string? MotivoRechazo { get; set; }
    public DateTime FechaCreacion { get; set; }
    public DateTime? FechaEnvioProveedor { get; set; }
    public FichaVpnDto? Ficha { get; set; }
    public List<HistorialItemDto> Historial { get; set; } = new();
}
