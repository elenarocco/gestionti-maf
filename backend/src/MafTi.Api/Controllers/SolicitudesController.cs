using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MafTi.Infrastructure;
using MafTi.Domain;
using MafTi.Api.Dtos;
using MafTi.Api.Seguridad;
using MafTi.Application;
using static MafTi.Application.SolicitudValores;

namespace MafTi.Api.Controllers;

// Bandeja de TI, flujo VPN y "mis solicitudes". Toda la regla de negocio se decide aquí y en
// MafTi.Application (FlujoSolicitud, SolicitudVpnValidator); el frontend solo muestra lo que recibe.
[ApiController]
[Route("api/solicitudes")]
public class SolicitudesController : ControllerBase
{
    private const string PermisoBandeja = "GestionarBandejaTI";          // solo Admin TI
    private const string PermisoCrearVpn = "SolicitarAccesoVPN";         // Jefatura
    private const string PermisoFichaVpn = "CompletarFichaVPN";          // + validación de creador
    private const string PermisoPropias = "ConsultarSolicitudesPropias"; // Jefatura, RRHH
    private const string PermisoAprobarBloqueo = "RevisarAprobarBloqueo"; // RRHH

    private readonly AppDbContext _context;

    public SolicitudesController(AppDbContext context)
    {
        _context = context;
    }

    // ---------- Bandeja de TI ----------

    [HttpGet]
    [RequierePermiso(PermisoBandeja)]
    public async Task<ActionResult<PaginaDto<SolicitudBandejaDto>>> Listar(
        string? tipo = null, string? estado = null, int pagina = 1, int tamano = 20)
    {
        var query = _context.Solicitudes.AsQueryable();

        if (!string.IsNullOrWhiteSpace(tipo))
        {
            var tipoDb = TipoADb(tipo);
            if (tipoDb == null) return BadRequest(new { error = "Tipo de solicitud no válido." });
            query = query.Where(s => s.Tipo == tipoDb);
        }
        if (!string.IsNullOrWhiteSpace(estado))
        {
            var estadoDb = EstadoADb(estado);
            if (estadoDb == null) return BadRequest(new { error = "Estado de solicitud no válido." });
            query = query.Where(s => s.Estado == estadoDb);
        }

        pagina = Math.Max(1, pagina);
        tamano = Math.Clamp(tamano, 1, 100);
        var total = await query.CountAsync();

        var filas = await query
            .OrderByDescending(s => s.FechaCreacion)
            .Skip((pagina - 1) * tamano)
            .Take(tamano)
            .Select(s => new
            {
                s.Id, s.Tipo, s.Estado, s.FechaCreacion, s.FechaVencimientoSLA,
                // VPN no tiene trabajador: se muestra el proveedor de la ficha cuando ya existe.
                TrabajadorNombre = s.TrabajadorId != null
                    ? s.Trabajador!.PrimerNombre + " " + s.Trabajador.PrimerApellido
                    : _context.SolicitudesVpn.Where(v => v.SolicitudId == s.Id && v.Nombres != null)
                        .Select(v => v.Nombres + " " + v.Apellidos).FirstOrDefault(),
                CreadoPorNombre = s.CreadoPor!.Trabajador!.PrimerNombre + " " + s.CreadoPor.Trabajador.PrimerApellido
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
                CreadoPorNombre = f.CreadoPorNombre,
                FechaCreacion = f.FechaCreacion,
                FechaVencimientoSLA = f.FechaVencimientoSLA,
                Vencida = EstaVencida(f.Estado, f.FechaVencimientoSLA, ahora)
            }).ToList()
        };
    }

    [HttpGet("{id:int}")]
    [RequierePermiso(PermisoBandeja)]
    public async Task<ActionResult<SolicitudBandejaDetalleDto>> Detalle(int id)
    {
        var s = await _context.Solicitudes
            .Include(x => x.Trabajador).ThenInclude(t => t!.Area)
            .Include(x => x.Trabajador).ThenInclude(t => t!.Cargo)
            .Include(x => x.Trabajador).ThenInclude(t => t!.DireccionCorporativa)
            .Include(x => x.Trabajador).ThenInclude(t => t!.LugarTrabajo)
            .Include(x => x.CreadoPor).ThenInclude(u => u!.Trabajador)
            .FirstOrDefaultAsync(x => x.Id == id);
        if (s == null) return NotFound(new { error = "La solicitud no existe." });

        var vpn = s.Tipo == TipoVpn
            ? await _context.SolicitudesVpn.Include(v => v.Motivo).FirstOrDefaultAsync(v => v.SolicitudId == s.Id)
            : null;

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
            AccionesPermitidas = FlujoSolicitud.AccionesTi(Situacion(s, vpn))
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

    // RRHH aprueba un Bloqueo pendiente y queda En proceso para TI.
    [HttpPut("{id:int}/aprobar-rrhh")]
    [RequierePermiso(PermisoAprobarBloqueo)]
    public async Task<IActionResult> AprobarRrhh(int id)
    {
        var usuarioId = SesionSimulada.UsuarioId(Request);
        if (usuarioId == null) return FaltaUsuario();

        var s = await _context.Solicitudes.FirstOrDefaultAsync(x => x.Id == id);
        if (s == null) return NotFound(new { error = "La solicitud no existe." });
        if (!FlujoSolicitud.PuedeAprobarRrhh(Situacion(s, null)))
            return Conflict(new { error = "Solo se pueden aprobar solicitudes de bloqueo pendientes." });

        s.Estado = EnProceso;
        _context.HistorialSolicitudes.Add(new HistorialSolicitud
        {
            SolicitudId = s.Id, Accion = "Aprobada por RRHH", RealizadoPorId = usuarioId.Value, Fecha = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();
        return NoContent();
    }

    private async Task<IActionResult> EjecutarAccionTiAsync(int id, string accion, string? justificacion = null, bool soloVpn = false)
    {
        var usuarioId = SesionSimulada.UsuarioId(Request);
        if (usuarioId == null) return FaltaUsuario();

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

        _context.HistorialSolicitudes.Add(new HistorialSolicitud
        {
            SolicitudId = s.Id,
            Accion = historial,
            RealizadoPorId = usuarioId.Value,
            Fecha = ahora,
            Comentario = esRechazo ? s.MotivoRechazo : null
        });
        await _context.SaveChangesAsync();
        return NoContent();
    }

    // ---------- Jefatura: mis solicitudes ----------

    [HttpGet("mias")]
    [RequierePermiso(PermisoPropias)]
    public async Task<ActionResult<PaginaDto<MiSolicitudDto>>> Mias(
        string? tipo = null, string? estado = null, int pagina = 1, int tamano = 20)
    {
        var usuarioId = SesionSimulada.UsuarioId(Request);
        if (usuarioId == null) return FaltaUsuario();

        var query = _context.Solicitudes.Where(s => s.CreadoPorId == usuarioId.Value);
        if (!string.IsNullOrWhiteSpace(tipo))
        {
            var tipoDb = TipoADb(tipo);
            if (tipoDb == null) return BadRequest(new { error = "Tipo de solicitud no válido." });
            query = query.Where(s => s.Tipo == tipoDb);
        }
        if (!string.IsNullOrWhiteSpace(estado))
        {
            var estadoDb = EstadoADb(estado);
            if (estadoDb == null) return BadRequest(new { error = "Estado de solicitud no válido." });
            query = query.Where(s => s.Estado == estadoDb);
        }

        pagina = Math.Max(1, pagina);
        tamano = Math.Clamp(tamano, 1, 100);
        var total = await query.CountAsync();

        var filas = await query
            .OrderByDescending(s => s.FechaCreacion)
            .Skip((pagina - 1) * tamano)
            .Take(tamano)
            .Select(s => new
            {
                s.Id, s.Tipo, s.Estado, s.FechaCreacion, s.FechaVencimientoSLA, s.MotivoRechazo,
                Vpn = _context.SolicitudesVpn.Where(v => v.SolicitudId == s.Id)
                    .Select(v => new { MotivoNombre = v.Motivo!.Nombre, v.FormularioHabilitado, FichaCompletada = v.Nombres != null })
                    .FirstOrDefault()
            })
            .ToListAsync();

        var ahora = DateTime.UtcNow;
        return new PaginaDto<MiSolicitudDto>
        {
            Total = total,
            Pagina = pagina,
            Tamano = tamano,
            Items = filas.Select(f => new MiSolicitudDto
            {
                Id = f.Id,
                Tipo = TipoAApi(f.Tipo),
                Estado = EstadoAApi(f.Estado),
                FechaCreacion = f.FechaCreacion,
                FechaVencimientoSLA = f.FechaVencimientoSLA,
                Vencida = EstaVencida(f.Estado, f.FechaVencimientoSLA, ahora),
                MotivoNombre = f.Vpn?.MotivoNombre,
                FormularioHabilitado = f.Vpn?.FormularioHabilitado ?? false,
                MotivoRechazo = f.MotivoRechazo,
                FichaCompletada = f.Vpn?.FichaCompletada ?? false
            }).ToList()
        };
    }

    // ---------- VPN ----------

    // Fase 1: Jefatura elige solo el motivo. El proveedor aún no existe, por eso no hay trabajador.
    [HttpPost("vpn")]
    [RequierePermiso(PermisoCrearVpn)]
    public async Task<ActionResult<CreadaDto>> CrearVpn(CrearSolicitudVpnDto dto)
    {
        var usuarioId = SesionSimulada.UsuarioId(Request);
        if (usuarioId == null) return FaltaUsuario();
        if (!await _context.UsuariosSistema.AnyAsync(u => u.Id == usuarioId.Value && u.Activo))
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
            CreadoPorId = usuarioId.Value,
            Tipo = TipoVpn,
            Estado = Pendiente,
            FechaCreacion = ahora,
            // TODO: SLA de VPN por confirmar con MAF; por ahora el mismo plazo que Ingreso y Modificación.
            FechaVencimientoSLA = ahora.AddDays(4)
        };
        _context.Solicitudes.Add(solicitud);
        await _context.SaveChangesAsync();

        _context.SolicitudesVpn.Add(new SolicitudVpn { SolicitudId = solicitud.Id, MotivoId = dto.MotivoId });
        // TODO: notificar a TI por correo. Por ahora la solicitud aparece en la Bandeja de TI.
        _context.HistorialSolicitudes.Add(new HistorialSolicitud
        {
            SolicitudId = solicitud.Id, Accion = "Creada", RealizadoPorId = usuarioId.Value, Fecha = ahora
        });
        await _context.SaveChangesAsync();
        await transaccion.CommitAsync();

        return CreatedAtAction(nameof(DetalleVpn), new { id = solicitud.Id }, new CreadaDto { Id = solicitud.Id });
    }

    // Para la ficha: lo ve la jefatura creadora o TI.
    [HttpGet("vpn/{id:int}")]
    public async Task<ActionResult<SolicitudVpnDetalleDto>> DetalleVpn(int id)
    {
        var rol = SesionSimulada.Rol(Request);
        if (string.IsNullOrEmpty(rol)) return Unauthorized("Falta el header X-Rol-Simulado (temporal, hasta tener login real).");
        var usuarioId = SesionSimulada.UsuarioId(Request);
        if (usuarioId == null) return FaltaUsuario();

        var s = await _context.Solicitudes.FirstOrDefaultAsync(x => x.Id == id && x.Tipo == TipoVpn);
        if (s == null) return NotFound(new { error = "La solicitud no existe." });

        var esCreador = s.CreadoPorId == usuarioId.Value;
        if (!esCreador && !await SesionSimulada.TienePermisoAsync(_context, rol, PermisoBandeja))
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
        var usuarioId = SesionSimulada.UsuarioId(Request);
        if (usuarioId == null) return FaltaUsuario();

        var s = await _context.Solicitudes.FirstOrDefaultAsync(x => x.Id == id && x.Tipo == TipoVpn);
        if (s == null) return NotFound(new { error = "La solicitud no existe." });
        if (s.CreadoPorId != usuarioId.Value)
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
            SolicitudId = s.Id, Accion = "Ficha VPN completada", RealizadoPorId = usuarioId.Value, Fecha = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();
        return NoContent();
    }

    // ---------- Apoyo ----------

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

    private UnauthorizedObjectResult FaltaUsuario() =>
        Unauthorized("Falta el header X-Usuario-Simulado (temporal, hasta tener login real).");
}
