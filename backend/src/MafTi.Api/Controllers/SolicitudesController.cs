using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MafTi.Infrastructure;
using MafTi.Domain;
using MafTi.Api.Common;
using MafTi.Api.Dtos;
using MafTi.Api.Seguridad;
using MafTi.Api.Reportes;
using MafTi.Application;
using static MafTi.Application.SolicitudValores;

namespace MafTi.Api.Controllers;

// Bandeja de TI, flujo VPN y "mis solicitudes". Toda la regla de negocio se decide aquí y en
// MafTi.Application (FlujoSolicitud, SolicitudVpnValidator); el frontend solo muestra lo que recibe.
[ApiController]
[Route("api/solicitudes")]
public partial class SolicitudesController : ControllerBase
{
    private const string PermisoBandeja = "GestionarBandejaTI";          // solo Admin TI
    private const string PermisoCrearVpn = "SolicitarAccesoVPN";         // Jefatura
    private const string PermisoFichaVpn = "CompletarFichaVPN";          // + validación de creador
    private const string PermisoPropias = "ConsultarSolicitudesPropias"; // Jefatura, RRHH
    private const string PermisoVerTodas = "ConsultarTodasLasSolicitudes"; // Admin TI, Auditoría, Super Admin

    private readonly AppDbContext _context;
    private readonly UsuarioActual _usuario;
    private readonly ServicioSla _sla;

    public SolicitudesController(AppDbContext context, UsuarioActual usuario, ServicioSla sla)
    {
        _context = context;
        _usuario = usuario;
        _sla = sla;
    }

    // ---------- Bandeja de TI ----------

    // Una sola lista de solicitudes para todos los roles. El backend decide el alcance:
    // quien puede ver todas (TI, Auditoría, Super Admin) las ve todas; el resto ve solo las que creó.
    // estado = Pendiente | EnProceso | Completada | Rechazada | Abiertas (Pendiente y En proceso).
    [HttpGet]
    public async Task<ActionResult<PaginaDto<SolicitudBandejaDto>>> Listar(
        string? tipo = null, string? estado = null, int pagina = 1, int tamano = 20)
    {
        var permisos = await PermisosDelRolAsync();
        if (!PuedeVerSolicitudes(permisos))
            return StatusCode(403, new { error = $"Tu rol '{_usuario.Rol}' no puede ver solicitudes." });

        var (query, error) = Filtrar(permisos, tipo, estado);
        if (query == null) return error!;

        pagina = Math.Max(1, pagina);
        tamano = Math.Clamp(tamano, 1, 100);
        var total = await query.CountAsync();

        var filas = await query
            .OrderByDescending(s => s.FechaCreacion)
            .Skip((pagina - 1) * tamano)
            .Take(tamano)
            .Select(s => new
            {
                s.Id, s.Tipo, s.Estado, s.FechaCreacion, s.FechaVencimientoSLA, s.TrabajadorId, s.CreadoPorId, s.MotivoRechazo,
                // VPN no tiene trabajador: se muestra el proveedor de la ficha cuando ya existe.
                TrabajadorNombre = s.TrabajadorId != null
                    ? s.Trabajador!.PrimerNombre + " " + s.Trabajador.PrimerApellido
                    : _context.SolicitudesVpn.Where(v => v.SolicitudId == s.Id && v.Nombres != null)
                        .Select(v => v.Nombres + " " + v.Apellidos).FirstOrDefault(),
                AreaNombre = s.TrabajadorId != null
                    ? s.Trabajador!.Area!.Nombre
                    : _context.SolicitudesVpn.Where(v => v.SolicitudId == s.Id).Select(v => v.Departamento).FirstOrDefault(),
                CreadoPorNombre = s.CreadoPor!.Trabajador!.PrimerNombre + " " + s.CreadoPor.Trabajador.PrimerApellido,
                YaCorregida = _context.Solicitudes.Any(x => x.SolicitudOrigenId == s.Id),
                Vpn = _context.SolicitudesVpn.Where(v => v.SolicitudId == s.Id)
                    .Select(v => new { MotivoNombre = v.Motivo!.Nombre, v.FormularioHabilitado, FichaCompletada = v.Nombres != null })
                    .FirstOrDefault()
            })
            .ToListAsync();

        var ahora = DateTime.UtcNow;
        return new PaginaDto<SolicitudBandejaDto>
        {
            Total = total,
            Pagina = pagina,
            Tamano = tamano,
            Items = filas.Select(f => new SolicitudBandejaDto
            {
                Id = f.Id,
                Tipo = TipoAApi(f.Tipo),
                Estado = EstadoAApi(f.Estado),
                TrabajadorNombre = f.TrabajadorNombre,
                AreaNombre = f.AreaNombre,
                CreadoPorNombre = f.CreadoPorNombre,
                FechaCreacion = f.FechaCreacion,
                FechaVencimientoSLA = f.FechaVencimientoSLA,
                Vencida = EstaVencida(f.Estado, f.FechaVencimientoSLA, ahora),
                TrabajadorId = f.TrabajadorId,
                MotivoRechazo = f.MotivoRechazo,
                MotivoVpn = f.Vpn?.MotivoNombre,
                AccionesPermitidas = f.CreadoPorId != _usuario.Id ? new() : FlujoSolicitud.AccionesCreador(
                    new FlujoSolicitud.Situacion(f.Tipo, f.Estado, f.Vpn?.FormularioHabilitado ?? false, f.Vpn?.FichaCompletada ?? false),
                    f.YaCorregida, PuedeCrearTipo(permisos, f.Tipo), permisos.Contains(PermisoFichaVpn))
            }).ToList()
        };
    }

    // Filtros de la lista; los usan la pantalla y el reporte, así se descarga lo mismo que se ve.
    private (IQueryable<Solicitud>? Query, ActionResult? Error) Filtrar(List<string> permisos, string? tipo, string? estado)
    {
        var query = _context.Solicitudes.AsQueryable();
        if (!VeTodas(permisos)) query = query.Where(s => s.CreadoPorId == _usuario.Id);

        if (!string.IsNullOrWhiteSpace(tipo))
        {
            var tipoDb = TipoADb(tipo);
            if (tipoDb == null) return (null, BadRequest(new { error = "Tipo de solicitud no válido." }));
            query = query.Where(s => s.Tipo == tipoDb);
        }
        if (string.Equals(estado?.Trim(), "Abiertas", StringComparison.OrdinalIgnoreCase))
            query = query.Where(s => s.Estado == Pendiente || s.Estado == EnProceso);
        else if (!string.IsNullOrWhiteSpace(estado))
        {
            var estadoDb = EstadoADb(estado);
            if (estadoDb == null) return (null, BadRequest(new { error = "Estado de solicitud no válido." }));
            query = query.Where(s => s.Estado == estadoDb);
        }
        return (query, null);
    }

    // Reporte de solicitudes (Excel o PDF) con los mismos filtros y el mismo alcance de la lista:
    // quien ve todas descarga todas; el resto, solo las que creó. desde/hasta filtran por fecha de creación.
    [HttpGet("exportar")]
    [RequierePermiso(DescargaReporte.Permiso)]
    public async Task<IActionResult> Exportar(string formato, string? tipo = null, string? estado = null,
        DateOnly? desde = null, DateOnly? hasta = null)
    {
        if (DescargaReporte.LeerFormato(formato) is not FormatoReporte fmt) return DescargaReporte.FormatoInvalido(this);
        var permisos = await PermisosDelRolAsync();
        if (!PuedeVerSolicitudes(permisos))
            return StatusCode(403, new { error = $"Tu rol '{_usuario.Rol}' no puede ver solicitudes." });

        var (query, error) = Filtrar(permisos, tipo, estado);
        if (query == null) return error!;
        // Las fechas se eligen en hora de Chile; en la base están en UTC.
        if (desde.HasValue)
        {
            var inicio = InicioDelDiaUtc(desde.Value);
            query = query.Where(s => s.FechaCreacion >= inicio);
        }
        if (hasta.HasValue)
        {
            var fin = InicioDelDiaUtc(hasta.Value.AddDays(1));
            query = query.Where(s => s.FechaCreacion < fin);
        }

        var filas = await query
            .OrderByDescending(s => s.FechaCreacion)
            .Take(Reporte.MaxFilas + 1)
            .Select(s => new
            {
                s.Id, s.Tipo, s.Estado, s.FechaCreacion, s.FechaVencimientoSLA, s.MotivoRechazo,
                TrabajadorNombre = s.TrabajadorId != null
                    ? s.Trabajador!.PrimerNombre + " " + s.Trabajador.PrimerApellido
                    : _context.SolicitudesVpn.Where(v => v.SolicitudId == s.Id && v.Nombres != null)
                        .Select(v => v.Nombres + " " + v.Apellidos).FirstOrDefault(),
                Rut = s.TrabajadorId != null ? s.Trabajador!.Rut : null,
                AreaNombre = s.TrabajadorId != null
                    ? s.Trabajador!.Area!.Nombre
                    : _context.SolicitudesVpn.Where(v => v.SolicitudId == s.Id).Select(v => v.Departamento).FirstOrDefault(),
                CreadoPorNombre = s.CreadoPor!.Trabajador!.PrimerNombre + " " + s.CreadoPor.Trabajador.PrimerApellido,
                // Si ya está cerrada (Completada o Rechazada), la fecha del último paso de su historial.
                Cierre = s.Estado == Completada || s.Estado == Rechazada
                    ? _context.HistorialSolicitudes.Where(h => h.SolicitudId == s.Id).Max(h => (DateTime?)h.Fecha)
                    : null
            })
            .ToListAsync();
        if (filas.Count > Reporte.MaxFilas) return DescargaReporte.DemasiadasFilas(this);

        var ahora = DateTime.UtcNow;
        var filtros = new List<string>();
        if (!VeTodas(permisos)) filtros.Add("Solo mis solicitudes");
        if (!string.IsNullOrWhiteSpace(tipo)) filtros.Add("Tipo: " + TipoADb(tipo));
        if (!string.IsNullOrWhiteSpace(estado))
            filtros.Add("Estado: " + (string.Equals(estado.Trim(), "Abiertas", StringComparison.OrdinalIgnoreCase)
                ? "Pendientes y en proceso" : EstadoADb(estado)));
        if (desde.HasValue) filtros.Add($"Desde: {desde:dd-MM-yyyy}");
        if (hasta.HasValue) filtros.Add($"Hasta: {hasta:dd-MM-yyyy}");

        var reporte = new Reporte
        {
            Titulo = "Solicitudes",
            NombreArchivo = "solicitudes",
            Columnas =
            [
                new("N°", 0.5), new("Tipo", 1), new("Estado", 0.9), new("Trabajador / proveedor", 1.7), new("RUT", 1),
                new("Área", 1.4), new("Creada por", 1.4), new("Creada", 1.1), new("Vence SLA", 1.1),
                new("Vencida", 0.7), new("Cerrada", 1.1), new("Motivo del rechazo", 2.2)
            ],
            Filas = filas.Select(f => new object?[]
            {
                f.Id, f.Tipo, f.Estado, f.TrabajadorNombre, f.Rut, f.AreaNombre, f.CreadoPorNombre,
                f.FechaCreacion, f.FechaVencimientoSLA, EstaVencida(f.Estado, f.FechaVencimientoSLA, ahora) ? "Sí" : "No",
                f.Cierre, f.MotivoRechazo
            }).ToList(),
            Filtros = filtros.Count == 0 ? "ninguno" : string.Join(" · ", filtros),
            GeneradoPor = _usuario.Nombre
        };
        return await DescargaReporte.EntregarAsync(this, _context, _usuario, reporte, fmt);
    }

    private static DateTime InicioDelDiaUtc(DateOnly dia) =>
        TimeZoneInfo.ConvertTimeToUtc(dia.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified), HoraChile.Zona());

    // Detalle con historial. Lo ve quien puede ver todas las solicitudes o quien la creó; las acciones
    // de TI aparecen solo con el permiso de la Bandeja y las del creador solo a quien la creó.
    [HttpGet("{id:int}")]
    public async Task<ActionResult<SolicitudBandejaDetalleDto>> Detalle(int id)
    {
        var permisos = await PermisosDelRolAsync();
        var s = await _context.Solicitudes
            .Include(x => x.Trabajador).ThenInclude(t => t!.Area)
            .Include(x => x.Trabajador).ThenInclude(t => t!.Cargo)
            .Include(x => x.Trabajador).ThenInclude(t => t!.DireccionCorporativa)
            .Include(x => x.Trabajador).ThenInclude(t => t!.LugarTrabajo)
            .Include(x => x.CreadoPor).ThenInclude(u => u!.Trabajador)
            .FirstOrDefaultAsync(x => x.Id == id);
        if (s == null) return NotFound(new { error = "La solicitud no existe." });
        var esCreador = s.CreadoPorId == _usuario.Id;
        if (!esCreador && !VeTodas(permisos))
            return StatusCode(403, new { error = "Solo puedes ver las solicitudes que creaste." });

        var vpn = s.Tipo == TipoVpn
            ? await _context.SolicitudesVpn.Include(v => v.Motivo).FirstOrDefaultAsync(v => v.SolicitudId == s.Id)
            : null;
        var situacion = Situacion(s, vpn);
        var acciones = permisos.Contains(PermisoBandeja) ? FlujoSolicitud.AccionesTi(situacion) : new List<string>();
        if (esCreador)
            acciones.AddRange(FlujoSolicitud.AccionesCreador(situacion,
                await _context.Solicitudes.AnyAsync(x => x.SolicitudOrigenId == s.Id),
                PuedeCrearTipo(permisos, s.Tipo), permisos.Contains(PermisoFichaVpn)));

        return new SolicitudBandejaDetalleDto
        {
            Id = s.Id,
            Tipo = TipoAApi(s.Tipo),
            Estado = EstadoAApi(s.Estado),
            MotivoRechazo = s.MotivoRechazo,
            FechaCreacion = s.FechaCreacion,
            FechaVencimientoSLA = s.FechaVencimientoSLA,
            Vencida = EstaVencida(s.Estado, s.FechaVencimientoSLA, DateTime.UtcNow),
            CreadoPorNombre = NombreDe(s.CreadoPor?.Trabajador),
            TrabajadorId = s.TrabajadorId,
            TrabajadorNombre = s.Trabajador != null ? NombreDe(s.Trabajador)
                : vpn?.Nombres != null ? $"{vpn.Nombres} {vpn.Apellidos}" : null,
            Detalle = await DetallePorTipoAsync(s),
            Vpn = vpn == null ? null : new VpnResumenDto
            {
                MotivoId = vpn.MotivoId,
                MotivoNombre = vpn.Motivo?.Nombre ?? "",
                FormularioHabilitado = vpn.FormularioHabilitado,
                FechaEnvioProveedor = vpn.FechaEnvioProveedor,
                Ficha = AFicha(vpn)
            },
            Historial = await HistorialAsync(s.Id),
            AccionesPermitidas = acciones
        };
    }

    [HttpPut("{id:int}/tomar")]
    [RequierePermiso(PermisoBandeja)]
    public Task<IActionResult> Tomar(int id) => EjecutarAccionTiAsync(id, FlujoSolicitud.Tomar);

    [HttpPut("{id:int}/completar")]
    [RequierePermiso(PermisoBandeja)]
    public Task<IActionResult> Completar(int id) => EjecutarAccionTiAsync(id, FlujoSolicitud.Completar);

    [HttpPut("{id:int}/rechazar")]
    [RequierePermiso(PermisoBandeja)]
    public Task<IActionResult> Rechazar(int id, JustificacionRechazoDto dto) =>
        EjecutarAccionTiAsync(id, FlujoSolicitud.Rechazar, dto.Justificacion);

    [HttpPut("vpn/{id:int}/confirmar-envio")]
    [RequierePermiso(PermisoBandeja)]
    public Task<IActionResult> ConfirmarEnvio(int id) => EjecutarAccionTiAsync(id, FlujoSolicitud.ConfirmarEnvio, soloVpn: true);

    [HttpPut("vpn/{id:int}/rechazar-ficha")]
    [RequierePermiso(PermisoBandeja)]
    public Task<IActionResult> RechazarFicha(int id, JustificacionRechazoDto dto) =>
        EjecutarAccionTiAsync(id, FlujoSolicitud.RechazarFicha, dto.Justificacion, soloVpn: true);

    private async Task<IActionResult> EjecutarAccionTiAsync(int id, string accion, string? justificacion = null, bool soloVpn = false)
    {
        var usuarioId = _usuario.Id;

        var s = await _context.Solicitudes.FirstOrDefaultAsync(x => x.Id == id);
        if (s == null || (soloVpn && s.Tipo != TipoVpn)) return NotFound(new { error = "La solicitud no existe." });
        var vpn = s.Tipo == TipoVpn ? await _context.SolicitudesVpn.FirstOrDefaultAsync(v => v.SolicitudId == s.Id) : null;

        var situacion = Situacion(s, vpn);
        if (!FlujoSolicitud.AccionesTi(situacion).Contains(accion))
            return Conflict(new { error = "La acción no es válida para el estado actual de la solicitud." });

        var esRechazo = accion is FlujoSolicitud.Rechazar or FlujoSolicitud.RechazarFicha;
        if (esRechazo && SolicitudVpnValidator.ValidarJustificacionRechazo(justificacion) is string error)
            return BadRequest(new { error });

        var ahora = DateTime.UtcNow;
        var historial = FlujoSolicitud.AccionHistorial(accion, situacion);
        if (FlujoSolicitud.EstadoTras(accion, situacion) is string nuevoEstado) s.Estado = nuevoEstado;
        if (accion == FlujoSolicitud.Tomar && vpn != null) vpn.FormularioHabilitado = true;
        if (accion == FlujoSolicitud.ConfirmarEnvio && vpn != null) vpn.FechaEnvioProveedor = ahora;
        if (esRechazo) s.MotivoRechazo = justificacion!.Trim();
        var comentario = esRechazo ? s.MotivoRechazo
            : accion == FlujoSolicitud.Completar && s.Tipo == TipoBloqueo ? await AplicarBloqueoAsync(s) : null;

        _context.HistorialSolicitudes.Add(new HistorialSolicitud
        {
            SolicitudId = s.Id,
            Accion = historial,
            RealizadoPorId = usuarioId,
            Fecha = ahora,
            Comentario = comentario
        });
        await _context.SaveChangesAsync();
        return NoContent();
    }

    // ---------- VPN ----------

    // Fase 1: Jefatura elige solo el motivo. El proveedor aún no existe, por eso no hay trabajador.
    [HttpPost("vpn")]
    [RequierePermiso(PermisoCrearVpn)]
    public async Task<ActionResult<CreadaDto>> CrearVpn(CrearSolicitudVpnDto dto)
    {
        var usuarioId = _usuario.Id;
        if (!await _context.UsuariosSistema.AnyAsync(u => u.Id == usuarioId && u.Activo))
            return BadRequest(new { error = "El usuario que crea la solicitud no existe o no está activo." });

        var motivoValido = await _context.Catalogos
            .AnyAsync(c => c.Id == dto.MotivoId && c.Tipo == "MotivoVpn" && c.Activo);
        if (!motivoValido)
            return BadRequest(new { errores = new Dictionary<string, string> { ["motivoId"] = "Debe seleccionar un motivo de VPN válido." } });

        var ahora = DateTime.UtcNow;
        using var transaccion = await _context.Database.BeginTransactionAsync();

        var solicitud = new Solicitud
        {
            TrabajadorId = null,
            CreadoPorId = usuarioId,
            Tipo = TipoVpn,
            Estado = Pendiente,
            FechaCreacion = ahora,
            FechaVencimientoSLA = (await _sla.CalcularAsync(TipoVpn, ahora)).VenceUtc
        };
        _context.Solicitudes.Add(solicitud);
        await _context.SaveChangesAsync();

        _context.SolicitudesVpn.Add(new SolicitudVpn { SolicitudId = solicitud.Id, MotivoId = dto.MotivoId });
        // TODO: notificar a TI por correo. Por ahora la solicitud aparece en la Bandeja de TI.
        _context.HistorialSolicitudes.Add(new HistorialSolicitud
        {
            SolicitudId = solicitud.Id, Accion = "Creada", RealizadoPorId = usuarioId, Fecha = ahora
        });
        await _context.SaveChangesAsync();
        await transaccion.CommitAsync();

        return CreatedAtAction(nameof(DetalleVpn), new { id = solicitud.Id }, new CreadaDto { Id = solicitud.Id });
    }

    // Para la ficha: lo ve la jefatura creadora o TI (el middleware ya exigió sesión).
    [HttpGet("vpn/{id:int}")]
    public async Task<ActionResult<SolicitudVpnDetalleDto>> DetalleVpn(int id)
    {
        var usuarioId = _usuario.Id;

        var s = await _context.Solicitudes.FirstOrDefaultAsync(x => x.Id == id && x.Tipo == TipoVpn);
        if (s == null) return NotFound(new { error = "La solicitud no existe." });

        var esCreador = s.CreadoPorId == usuarioId;
        if (!esCreador && !await _context.RolPermisos.AnyAsync(rp => rp.RolId == _usuario.RolId && rp.Permiso!.Codigo == PermisoBandeja))
            return StatusCode(403, new { error = "Solo quien creó la solicitud o TI pueden verla." });

        var vpn = await _context.SolicitudesVpn.Include(v => v.Motivo).FirstAsync(v => v.SolicitudId == s.Id);
        return new SolicitudVpnDetalleDto
        {
            Id = s.Id,
            Estado = EstadoAApi(s.Estado),
            MotivoId = vpn.MotivoId,
            MotivoNombre = vpn.Motivo?.Nombre ?? "",
            FormularioHabilitado = vpn.FormularioHabilitado,
            MotivoRechazo = s.MotivoRechazo,
            FechaCreacion = s.FechaCreacion,
            FechaEnvioProveedor = vpn.FechaEnvioProveedor,
            Ficha = AFicha(vpn),
            Historial = await HistorialAsync(s.Id)
        };
    }

    // Fase 3: solo la jefatura creadora, y solo si TI habilitó la ficha de ESTA solicitud.
    [HttpPut("vpn/{id:int}/ficha")]
    [RequierePermiso(PermisoFichaVpn)]
    public async Task<IActionResult> CompletarFicha(int id, FichaVpnDto dto)
    {
        var usuarioId = _usuario.Id;

        var s = await _context.Solicitudes.FirstOrDefaultAsync(x => x.Id == id && x.Tipo == TipoVpn);
        if (s == null) return NotFound(new { error = "La solicitud no existe." });
        if (s.CreadoPorId != usuarioId)
            return StatusCode(403, new { error = "Solo quien creó la solicitud puede completar la ficha." });

        var vpn = await _context.SolicitudesVpn.FirstAsync(v => v.SolicitudId == s.Id);
        if (!FlujoSolicitud.PuedeCompletarFicha(Situacion(s, vpn)))
            return Conflict(new { error = "La ficha de esta solicitud no está habilitada para completarse." });

        var errores = SolicitudVpnValidator.ValidarFicha(new SolicitudVpnValidator.Ficha(
            dto.FechaIncorporacion, dto.FechaCaducidad, dto.Rut, dto.Cargo, dto.Nombres, dto.Apellidos,
            dto.Departamento, dto.UbicacionFisica, dto.EquipoCorporativo, dto.CompaniaExterna, dto.CuentaCorreo, dto.Motivo));
        if (errores.Count > 0) return BadRequest(new { errores });

        vpn.FechaIncorporacion = dto.FechaIncorporacion;
        vpn.FechaCaducidad = dto.FechaCaducidad;
        vpn.Rut = TrabajadorValidator.FormatearParaGuardar(dto.Rut!);
        vpn.Cargo = dto.Cargo!.Trim();
        vpn.Nombres = FormatoNombre.Normalizar(dto.Nombres);
        vpn.Apellidos = FormatoNombre.Normalizar(dto.Apellidos);
        vpn.Departamento = dto.Departamento!.Trim();
        vpn.UbicacionFisica = dto.UbicacionFisica!.Trim();
        vpn.EquipoCorporativo = dto.EquipoCorporativo;
        vpn.CompaniaExterna = dto.EquipoCorporativo == false ? dto.CompaniaExterna!.Trim() : null;
        vpn.CuentaCorreo = dto.CuentaCorreo!.Trim().ToLower();
        vpn.Justificacion = dto.Motivo!.Trim();
        s.Estado = EnProceso;

        _context.HistorialSolicitudes.Add(new HistorialSolicitud
        {
            SolicitudId = s.Id, Accion = "Ficha VPN completada", RealizadoPorId = usuarioId, Fecha = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();
        return NoContent();
    }

    // ---------- Apoyo ----------

    private Task<List<string>> PermisosDelRolAsync() =>
        _context.RolPermisos.Where(rp => rp.RolId == _usuario.RolId).Select(rp => rp.Permiso!.Codigo).ToListAsync();

    private static bool VeTodas(List<string> permisos) =>
        permisos.Contains(PermisoVerTodas) || permisos.Contains(PermisoBandeja);

    // Ve la lista quien ve todas, quien consulta las propias o quien puede crear algún tipo.
    private static bool PuedeVerSolicitudes(List<string> permisos) =>
        VeTodas(permisos) || permisos.Contains(PermisoPropias) || PermisoPorTipo.Values.Any(permisos.Contains);

    private static bool PuedeCrearTipo(List<string> permisos, string tipoDb) =>
        PermisoPorTipo.TryGetValue(tipoDb, out var codigo) && permisos.Contains(codigo);

    // Bloqueo completado: el trabajador queda inactivo en nuestra base (con su fecha de salida), para que
    // la dotación esté al día. Un bloqueo temporal no lo inactiva. Devuelve el comentario para el historial.
    private async Task<string?> AplicarBloqueoAsync(Solicitud s)
    {
        var bloqueo = await _context.SolicitudesBloqueo.FirstOrDefaultAsync(b => b.SolicitudId == s.Id);
        var trabajador = s.TrabajadorId == null ? null : await _context.Trabajadores.FindAsync(s.TrabajadorId);
        if (bloqueo == null || trabajador == null) return null;
        if (bloqueo.EsTemporal) return "Bloqueo temporal: el trabajador sigue activo en la base.";

        var salida = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(bloqueo.FechaInicio, HoraChile.Zona()));
        trabajador.Activo = false;
        trabajador.FechaSalida = salida;
        return $"Trabajador marcado como inactivo (fecha de salida {salida:dd-MM-yyyy}).";
    }

    private static FlujoSolicitud.Situacion Situacion(Solicitud s, SolicitudVpn? vpn) =>
        new(s.Tipo, s.Estado, vpn?.FormularioHabilitado ?? false, vpn?.Nombres != null);

    private static FichaVpnDto? AFicha(SolicitudVpn v) => v.Nombres == null ? null : new FichaVpnDto
    {
        FechaIncorporacion = v.FechaIncorporacion,
        FechaCaducidad = v.FechaCaducidad,
        Rut = v.Rut,
        Cargo = v.Cargo,
        Nombres = v.Nombres,
        Apellidos = v.Apellidos,
        Departamento = v.Departamento,
        UbicacionFisica = v.UbicacionFisica,
        EquipoCorporativo = v.EquipoCorporativo,
        CompaniaExterna = v.CompaniaExterna,
        CuentaCorreo = v.CuentaCorreo,
        Motivo = v.Justificacion
    };

    private static string NombreDe(Trabajador? t) => t == null ? "" : $"{t.PrimerNombre} {t.PrimerApellido}";

    private Task<List<HistorialItemDto>> HistorialAsync(int solicitudId) =>
        _context.HistorialSolicitudes
            .Where(h => h.SolicitudId == solicitudId)
            .OrderBy(h => h.Fecha)
            .Select(h => new HistorialItemDto
            {
                Accion = h.Accion,
                RealizadoPorNombre = h.RealizadoPor!.Trabajador!.PrimerNombre + " " + h.RealizadoPor.Trabajador.PrimerApellido,
                Fecha = h.Fecha,
                Comentario = h.Comentario
            })
            .ToListAsync();

    // Datos propios de cada tipo, con nombres en lugar de ids de catálogo.
    private async Task<Dictionary<string, object?>?> DetallePorTipoAsync(Solicitud s)
    {
        if (s.Tipo == TipoIngreso && s.Trabajador is Trabajador t)
        {
            var accesos = await NombresAccesosAsync(s.Id, "Agregar");
            return new()
            {
                ["rut"] = t.Rut,
                ["primerNombre"] = t.PrimerNombre,
                ["segundoNombre"] = t.SegundoNombre,
                ["primerApellido"] = t.PrimerApellido,
                ["segundoApellido"] = t.SegundoApellido,
                ["fechaNacimiento"] = t.FechaNacimiento,
                ["sexo"] = t.Sexo,
                ["correo"] = t.Correo,
                ["direccionCorporativa"] = t.DireccionCorporativa?.Nombre,
                ["area"] = t.Area?.Nombre,
                ["cargo"] = t.Cargo?.Nombre,
                ["lugarTrabajo"] = t.LugarTrabajo?.Nombre,
                ["fechaIncorporacion"] = t.FechaIncorporacion,
                ["jefeDirecto"] = t.JefeDirecto,
                ["homologarAccesosDesde"] = t.HomologarAccesosDesde,
                ["tieneTelefonoCorporativo"] = t.TieneTelefonoCorporativo,
                ["solicitaTelefono"] = t.SolicitaTelefono,
                ["accesosSolicitados"] = accesos
            };
        }

        if (s.Tipo == TipoModificacion)
        {
            var m = await _context.SolicitudesModificacion.FirstOrDefaultAsync(x => x.SolicitudId == s.Id);
            if (m == null) return null;
            var ids = new[] { m.DireccionCorporativaId, m.AreaId, m.CargoId, m.LugarTrabajoId };
            var nombres = await _context.Catalogos.Where(c => ids.Contains(c.Id)).ToDictionaryAsync(c => c.Id, c => c.Nombre);
            return new()
            {
                ["cambios"] = await CambiosModificacion.DeSolicitudAsync(_context, s),
                ["justificacion"] = m.Justificacion,
                ["primerNombre"] = m.PrimerNombre,
                ["segundoNombre"] = m.SegundoNombre,
                ["primerApellido"] = m.PrimerApellido,
                ["segundoApellido"] = m.SegundoApellido,
                ["fechaNacimiento"] = m.FechaNacimiento,
                ["sexo"] = m.Sexo,
                ["correo"] = m.Correo,
                ["direccionCorporativa"] = nombres.GetValueOrDefault(m.DireccionCorporativaId),
                ["area"] = nombres.GetValueOrDefault(m.AreaId),
                ["cargo"] = nombres.GetValueOrDefault(m.CargoId),
                ["lugarTrabajo"] = nombres.GetValueOrDefault(m.LugarTrabajoId),
                ["fechaIncorporacion"] = m.FechaIncorporacion,
                ["jefeDirecto"] = m.JefeDirecto,
                ["homologarAccesosDesde"] = m.HomologarAccesosDesde,
                ["tieneTelefonoCorporativo"] = m.TieneTelefonoCorporativo,
                ["solicitaTelefono"] = m.SolicitaTelefono,
                ["accesosAgregar"] = await NombresAccesosAsync(s.Id, "Agregar"),
                ["accesosQuitar"] = await NombresAccesosAsync(s.Id, "Quitar")
            };
        }

        if (s.Tipo == TipoBloqueo)
        {
            var b = await _context.SolicitudesBloqueo.FirstOrDefaultAsync(x => x.SolicitudId == s.Id);
            if (b == null) return null;
            return new()
            {
                ["esTemporal"] = b.EsTemporal,
                ["fechaInicio"] = b.FechaInicio,
                ["fechaFin"] = b.FechaFin,
                ["justificacion"] = b.Justificacion,
                ["tienePc"] = b.TienePc,
                ["casillaOpera"] = b.CasillaOpera
            };
        }

        return null;   // VPN: sus datos van en "vpn"
    }

    private Task<List<string>> NombresAccesosAsync(int solicitudId, string accion) =>
        _context.SolicitudDetalles
            .Where(d => d.SolicitudId == solicitudId && d.Accion == accion)
            .Select(d => d.Catalogo!.Nombre)
            .ToListAsync();
}
