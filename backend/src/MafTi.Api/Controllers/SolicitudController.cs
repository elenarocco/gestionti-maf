using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MafTi.Infrastructure;
using MafTi.Domain;
using MafTi.Api.Dtos;
using MafTi.Api.Common;
using MafTi.Application;
using MafTi.Api.Seguridad;
namespace MafTi.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SolicitudController : ControllerBase
{
    private readonly AppDbContext _context;

    public SolicitudController(AppDbContext context)
    {
        _context = context;
    }
    private static readonly Dictionary<string, string> PermisoPorTipo = new()
{
    { "Ingreso", "CrearSolicitudIngreso" },
    { "Modificación", "CrearSolicitudModificacion" },
    { "Bloqueo", "CrearSolicitudBloqueo" },
    { "VPN", "SolicitarAccesoVPN" }
};

    [HttpGet]
    public async Task<ActionResult<PaginacionResultado<SolicitudListaDto>>> GetAll(
        int pagina = 1, int tamanoPagina = 20, string? tipo = null,
        string? estado = null, bool? urgente = null, DateTime? desde = null, DateTime? hasta = null)
    {
        var rolActual = Request.Headers["X-Rol-Simulado"].ToString();

        bool veTodo = await _context.RolPermisos
            .Include(rp => rp.Rol).Include(rp => rp.Permiso)
            .AnyAsync(rp => rp.Rol!.Nombre == rolActual && rp.Permiso!.Codigo == "ConsultarTodasLasSolicitudes");

        var query = _context.Solicitudes.AsQueryable();

        if (!veTodo)
        {
            var tiposPermitidos = new List<string>();
            foreach (var par in PermisoPorTipo)
            {
                var tieneCreacion = await _context.RolPermisos
                    .Include(rp => rp.Rol).Include(rp => rp.Permiso)
                    .AnyAsync(rp => rp.Rol!.Nombre == rolActual && rp.Permiso!.Codigo == par.Value);
                if (tieneCreacion) tiposPermitidos.Add(par.Key);
            }
            query = query.Where(s => tiposPermitidos.Contains(s.Tipo));
        }

        // Jefatura solo ve solicitudes de trabajadores de su área.
        var areaPropia = (await UsuarioActual.ObtenerAsync(_context, Request))?.AreaId;
        if (areaPropia.HasValue) query = query.Where(s => s.Trabajador!.AreaId == areaPropia);

         if (!string.IsNullOrEmpty(tipo)) query = query.Where(s => s.Tipo == tipo);
         if (!string.IsNullOrEmpty(estado)) query = query.Where(s => s.Estado == estado);
         if (urgente == true) query = query.Where(s => s.Estado == "Pendiente" && s.FechaVencimientoSLA < DateTime.UtcNow);
         if (desde.HasValue) query = query.Where(s => s.FechaCreacion >= desde.Value);
         if (hasta.HasValue) query = query.Where(s => s.FechaCreacion <= hasta.Value);

        var resultado = await query
            .OrderByDescending(s => s.FechaCreacion)
            .Select(s => new SolicitudListaDto
            {
                Id = s.Id,
                TrabajadorNombre = s.Trabajador!.PrimerNombre + " " + s.Trabajador.PrimerApellido,
                AreaNombre = s.Trabajador!.Area!.Nombre,
                Tipo = s.Tipo,
                Estado = s.Estado,
                FechaCreacion = s.FechaCreacion,
                FechaVencimientoSLA = s.FechaVencimientoSLA,
                EsUrgente = s.Estado == "Pendiente" && s.FechaVencimientoSLA < DateTime.UtcNow
            })
            .PaginarAsync(pagina, tamanoPagina);

        return Ok(resultado);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<SolicitudDetalleDto>> GetById(int id)
    {
       var s = await _context.Solicitudes
            .Include(x => x.Trabajador)
            .Include(x => x.CreadoPor)
                .ThenInclude(u => u!.Trabajador)
             .FirstOrDefaultAsync(x => x.Id == id);

        if (s == null) return NotFound();

        var areaPropia = (await UsuarioActual.ObtenerAsync(_context, Request))?.AreaId;
        if (areaPropia.HasValue && s.Trabajador!.AreaId != areaPropia)
            return StatusCode(403, new { error = "Solo puedes consultar solicitudes de tu área." });

        SolicitudBloqueoDetalleDto? bloqueo = null;
        if (s.Tipo == "Bloqueo")
        {
            bloqueo = await _context.SolicitudesBloqueo
                .Where(b => b.SolicitudId == s.Id)
                .Select(b => new SolicitudBloqueoDetalleDto
                {
                    Id = b.Id,
                    SolicitudId = b.SolicitudId,
                    EsTemporal = b.EsTemporal,
                    FechaInicio = b.FechaInicio,
                    FechaFin = b.FechaFin,
                    Justificacion = b.Justificacion,
                    TienePc = b.TienePc,
                    CasillaOpera = b.CasillaOpera
                })
                .FirstOrDefaultAsync();
        }

        SolicitudModificacionDetalleDto? modificacion = null;
        if (s.Tipo == "Modificación")
        {
            modificacion = await _context.SolicitudesModificacion
                .Where(m => m.SolicitudId == s.Id)
                .Select(m => new SolicitudModificacionDetalleDto
                {
                    Id = m.Id,
                    SolicitudId = m.SolicitudId,
                    Justificacion = m.Justificacion,
                    PrimerNombre = m.PrimerNombre,
                    SegundoNombre = m.SegundoNombre,
                    PrimerApellido = m.PrimerApellido,
                    SegundoApellido = m.SegundoApellido,
                    FechaNacimiento = m.FechaNacimiento,
                    Sexo = m.Sexo,
                    Correo = m.Correo,
                    DireccionCorporativaId = m.DireccionCorporativaId,
                    AreaId = m.AreaId,
                    CargoId = m.CargoId,
                    LugarTrabajoId = m.LugarTrabajoId,
                    FechaIncorporacion = m.FechaIncorporacion,
                    DireccionDomicilio = m.DireccionDomicilio,
                    JefeDirecto = m.JefeDirecto,
                    HomologarAccesosDesde = m.HomologarAccesosDesde,
                    TieneTelefonoCorporativo = m.TieneTelefonoCorporativo,
                    SolicitaTelefono = m.SolicitaTelefono
                })
                .FirstOrDefaultAsync();

            if (modificacion != null)
            {
                var accesos = await _context.SolicitudDetalles
                    .Where(d => d.SolicitudId == s.Id)
                    .Select(d => new { d.Accion, Acceso = new SolicitudModificacionAccesoDto
                    {
                        CatalogoId = d.CatalogoId,
                        Nombre = d.Catalogo!.Nombre,
                        Tipo = d.Catalogo.Tipo
                    } })
                    .ToListAsync();
                modificacion.AccesosAgregar = accesos.Where(a => a.Accion == "Agregar").Select(a => a.Acceso).ToList();
                modificacion.AccesosQuitar = accesos.Where(a => a.Accion == "Quitar").Select(a => a.Acceso).ToList();
            }
        }

        return new SolicitudDetalleDto
        {
            Id = s.Id,
            TrabajadorId = s.TrabajadorId,
            TrabajadorNombre = s.Trabajador!.PrimerNombre + " " + s.Trabajador.PrimerApellido,
            CreadoPorId = s.CreadoPorId,
            CreadoPorCorreo = s.CreadoPor!.Trabajador!.Correo,
            Tipo = s.Tipo,
            Estado = s.Estado,
            MotivoRechazo = s.MotivoRechazo,
            SolicitudOrigenId = s.SolicitudOrigenId,
            FechaCreacion = s.FechaCreacion,
            FechaVencimientoSLA = s.FechaVencimientoSLA,
            Bloqueo = bloqueo,
            Modificacion = modificacion
        };
    }

    [HttpPost("ingreso")]
    [RequierePermiso("CrearSolicitudIngreso")]
    public async Task<ActionResult<IngresoResultadoDto>> CrearIngreso(IngresoCreateDto dto)
    {
        var creadoPorId = await UsuarioActualIdAsync();
        if (creadoPorId == null) return SinUsuarioParaRol();

        var nuevoTrabajador = new Trabajador
        {
            Rut = TrabajadorValidator.FormatearParaGuardar(dto.Rut),
            PrimerNombre = FormatoNombre.Normalizar(dto.PrimerNombre)!,
            SegundoNombre = FormatoNombre.Normalizar(dto.SegundoNombre),
            PrimerApellido = FormatoNombre.Normalizar(dto.PrimerApellido)!,
            SegundoApellido = FormatoNombre.Normalizar(dto.SegundoApellido),
            FechaNacimiento = dto.FechaNacimiento,
            Sexo = dto.Sexo,
            Correo = dto.Correo,
            DireccionCorporativaId = dto.DireccionCorporativaId,
            AreaId = dto.AreaId,
            CargoId = dto.CargoId,
            LugarTrabajoId = dto.LugarTrabajoId,
            FechaIncorporacion = dto.FechaIncorporacion,
            DireccionDomicilio = dto.DireccionDomicilio,
            JefeDirecto = dto.JefeDirecto,
            HomologarAccesosDesde = dto.HomologarAccesosDesde,
            TieneTelefonoCorporativo = dto.TieneTelefonoCorporativo,
            SolicitaTelefono = dto.SolicitaTelefono,
            Activo = true
        };
        

        var errores = await TrabajadorValidator.ValidarAsync(
            nuevoTrabajador,
            async correo => await _context.Trabajadores.AnyAsync(t => t.Correo.ToLower() == correo),
            async rut => await _context.Trabajadores.AnyAsync(t => t.Rut == rut && t.Activo),
            async (areaId, direccionId) => await _context.Catalogos.AnyAsync(
                c => c.Id == areaId && c.Tipo == "Area" && c.PadreId == direccionId));
            
        if (errores.Any())
        {
            return BadRequest(new { errores });
        }
        using var transaccion = await _context.Database.BeginTransactionAsync();

        _context.Trabajadores.Add(nuevoTrabajador);
        await _context.SaveChangesAsync();

        var nuevaSolicitud = new Solicitud
        {
            TrabajadorId = nuevoTrabajador.Id,
            CreadoPorId = creadoPorId.Value,
            Tipo = "Ingreso",
            Estado = "Pendiente",
            FechaCreacion = DateTime.UtcNow,
            FechaVencimientoSLA = DateTime.UtcNow.AddDays(4)
        };

        _context.Solicitudes.Add(nuevaSolicitud);
        await _context.SaveChangesAsync();

        foreach (var catalogoId in dto.CatalogoIds)
        {
            _context.SolicitudDetalles.Add(new SolicitudDetalle
            {
                SolicitudId = nuevaSolicitud.Id,
                CatalogoId = catalogoId
            });
        }

        _context.HistorialSolicitudes.Add(new HistorialSolicitud
        {
            SolicitudId = nuevaSolicitud.Id,
            Accion = "Creada",
            RealizadoPorId = creadoPorId.Value,
            Fecha = nuevaSolicitud.FechaCreacion
        });
        
        await _context.SaveChangesAsync();
        await transaccion.CommitAsync();
        
        return Ok(new IngresoResultadoDto
        {
                TrabajadorId = nuevoTrabajador.Id,
                SolicitudId = nuevaSolicitud.Id,
                FechaVencimientoSLA = nuevaSolicitud.FechaVencimientoSLA
        });

        
               
            
    }

    [HttpPost("bloqueo")]
    [RequierePermiso("CrearSolicitudBloqueo")]
    public async Task<ActionResult<SolicitudBloqueoResultadoDto>> CrearBloqueo(SolicitudBloqueoCreateDto dto)
    {
        var creadoPorId = await UsuarioActualIdAsync();
        if (creadoPorId == null) return SinUsuarioParaRol();

        var ahora = DateTime.UtcNow;
        var desde = AUtc(dto.Desde);
        var hasta = AUtc(dto.Hasta);

        using var transaccion = await _context.Database.BeginTransactionAsync();

        var trabajadorActivo = await _context.Trabajadores
            .AnyAsync(t => t.Id == dto.TrabajadorId && t.Activo);
        if (!trabajadorActivo)
        {
            return BadRequest(new { errores = new List<string> { "El trabajador no existe o no está activo." } });
        }

        var tieneBloqueoAbierto = await _context.Solicitudes
            .AnyAsync(s => s.TrabajadorId == dto.TrabajadorId && s.Tipo == "Bloqueo"
                && (s.Estado == "Pendiente" || s.Estado == "En proceso"));
        if (tieneBloqueoAbierto)
        {
            return BadRequest(new { errores = new List<string> { "El trabajador ya tiene una solicitud de bloqueo pendiente o en proceso." } });
        }

        var errores = SolicitudBloqueoValidator.Validar(dto.EsTemporal, desde, hasta, dto.Justificacion, ahora);
        if (errores.Any())
        {
            return BadRequest(new { errores });
        }

        var nuevaSolicitud = new Solicitud
        {
            TrabajadorId = dto.TrabajadorId,
            CreadoPorId = creadoPorId.Value,
            Tipo = "Bloqueo",
            Estado = "Pendiente",
            FechaCreacion = ahora,
            // TODO: SLA real en días hábiles considerando feriados; por ahora, fin del mismo día en hora de Chile.
            FechaVencimientoSLA = HoraChile.FinDelDiaUtc(ahora)
        };

        _context.Solicitudes.Add(nuevaSolicitud);
        await _context.SaveChangesAsync();

        _context.SolicitudesBloqueo.Add(new SolicitudBloqueo
        {
            SolicitudId = nuevaSolicitud.Id,
            EsTemporal = dto.EsTemporal,
            FechaInicio = dto.EsTemporal ? desde!.Value : ahora,
            FechaFin = dto.EsTemporal ? hasta : null,
            Justificacion = dto.Justificacion.Trim(),
            TienePc = dto.TienePc,
            CasillaOpera = dto.CasillaOpera
        });

        _context.HistorialSolicitudes.Add(new HistorialSolicitud
        {
            SolicitudId = nuevaSolicitud.Id,
            Accion = "Creada",
            RealizadoPorId = creadoPorId.Value,
            Fecha = ahora
        });

        await _context.SaveChangesAsync();
        await transaccion.CommitAsync();

        return CreatedAtAction(nameof(GetById), new { id = nuevaSolicitud.Id }, new SolicitudBloqueoResultadoDto
        {
            SolicitudId = nuevaSolicitud.Id,
            FechaVencimientoSLA = nuevaSolicitud.FechaVencimientoSLA
        });
    }

    [HttpPost("modificacion")]
    [RequierePermiso("CrearSolicitudModificacion")]
    public async Task<ActionResult<SolicitudModificacionResultadoDto>> CrearModificacion(SolicitudModificacionCreateDto dto)
    {
        var creadoPorId = await UsuarioActualIdAsync();
        if (creadoPorId == null) return SinUsuarioParaRol();

        var ahora = DateTime.UtcNow;

        using var transaccion = await _context.Database.BeginTransactionAsync();

        var actual = await _context.Trabajadores
            .FirstOrDefaultAsync(t => t.Id == dto.TrabajadorId && t.Activo);
        if (actual == null)
        {
            return BadRequest(new { errores = new List<string> { "El trabajador no existe o no está activo." } });
        }

        var tieneModificacionAbierta = await _context.Solicitudes
            .AnyAsync(s => s.TrabajadorId == dto.TrabajadorId && s.Tipo == "Modificación"
                && (s.Estado == "Pendiente" || s.Estado == "En proceso"));
        if (tieneModificacionAbierta)
        {
            return BadRequest(new { errores = new List<string> { "El trabajador ya tiene una solicitud de modificación pendiente o en proceso." } });
        }

        // Valores propuestos. El RUT no se modifica.
        var propuesto = new Trabajador
        {
            Id = actual.Id,
            Rut = actual.Rut,
            PrimerNombre = FormatoNombre.Normalizar(dto.PrimerNombre)!,
            SegundoNombre = FormatoNombre.Normalizar(dto.SegundoNombre),
            PrimerApellido = FormatoNombre.Normalizar(dto.PrimerApellido)!,
            SegundoApellido = FormatoNombre.Normalizar(dto.SegundoApellido),
            FechaNacimiento = dto.FechaNacimiento,
            Sexo = dto.Sexo,
            Correo = dto.Correo.Trim().ToLower(),
            DireccionCorporativaId = dto.DireccionCorporativaId,
            AreaId = dto.AreaId,
            CargoId = dto.CargoId,
            LugarTrabajoId = dto.LugarTrabajoId,
            FechaIncorporacion = dto.FechaIncorporacion,
            DireccionDomicilio = string.IsNullOrWhiteSpace(dto.DireccionDomicilio) ? null : dto.DireccionDomicilio.Trim(),
            JefeDirecto = string.IsNullOrWhiteSpace(dto.JefeDirecto) ? null : dto.JefeDirecto.Trim(),
            HomologarAccesosDesde = string.IsNullOrWhiteSpace(dto.HomologarAccesosDesde) ? null : dto.HomologarAccesosDesde.Trim(),
            TieneTelefonoCorporativo = dto.TieneTelefonoCorporativo,
            SolicitaTelefono = dto.SolicitaTelefono
        };

        // Mismas reglas que Ingreso, excluyendo al propio trabajador en los chequeos de duplicados.
        var errores = await TrabajadorValidator.ValidarAsync(
            propuesto,
            async correo => await _context.Trabajadores.AnyAsync(t => t.Correo.ToLower() == correo && t.Id != actual.Id),
            async rut => await _context.Trabajadores.AnyAsync(t => t.Rut == rut && t.Activo && t.Id != actual.Id),
            async (areaId, direccionId) => await _context.Catalogos.AnyAsync(
                c => c.Id == areaId && c.Tipo == "Area" && c.PadreId == direccionId));

        errores.AddRange(JustificacionValidator.Validar(dto.Justificacion));

        var agregar = dto.CatalogoIdsAgregar.Distinct().ToList();
        var quitar = dto.CatalogoIdsQuitar.Distinct().ToList();

        var accesosActivos = await _context.Accesos
            .Where(a => a.TrabajadorId == actual.Id && a.Estado == "Activo")
            .Select(a => a.CatalogoId)
            .ToListAsync();

        if (agregar.Intersect(quitar).Any())
            errores.Add("Un mismo acceso no puede agregarse y quitarse en la misma solicitud.");

        var validosParaAgregar = await _context.Catalogos
            .Where(c => agregar.Contains(c.Id) && c.Activo && (c.Tipo == "Sistema" || c.Tipo == "Carpeta"))
            .Select(c => c.Id)
            .ToListAsync();
        if (validosParaAgregar.Count != agregar.Count)
            errores.Add("Uno o más accesos a agregar no son sistemas o carpetas válidos.");
        if (agregar.Any(id => accesosActivos.Contains(id)))
            errores.Add("Uno o más accesos a agregar ya están activos para el trabajador.");
        if (quitar.Any(id => !accesosActivos.Contains(id)))
            errores.Add("Uno o más accesos a quitar no están activos para el trabajador.");

        var hayCambiosDeDatos =
            propuesto.PrimerNombre != actual.PrimerNombre ||
            !MismoTexto(propuesto.SegundoNombre, actual.SegundoNombre) ||
            propuesto.PrimerApellido != actual.PrimerApellido ||
            !MismoTexto(propuesto.SegundoApellido, actual.SegundoApellido) ||
            propuesto.FechaNacimiento != actual.FechaNacimiento ||
            propuesto.Sexo != actual.Sexo ||
            propuesto.Correo != actual.Correo.Trim().ToLower() ||
            propuesto.DireccionCorporativaId != actual.DireccionCorporativaId ||
            propuesto.AreaId != actual.AreaId ||
            propuesto.CargoId != actual.CargoId ||
            propuesto.LugarTrabajoId != actual.LugarTrabajoId ||
            propuesto.FechaIncorporacion != actual.FechaIncorporacion ||
            !MismoTexto(propuesto.DireccionDomicilio, actual.DireccionDomicilio) ||
            !MismoTexto(propuesto.JefeDirecto, actual.JefeDirecto) ||
            !MismoTexto(propuesto.HomologarAccesosDesde, actual.HomologarAccesosDesde) ||
            propuesto.TieneTelefonoCorporativo != actual.TieneTelefonoCorporativo ||
            propuesto.SolicitaTelefono != actual.SolicitaTelefono;
        if (!hayCambiosDeDatos && agregar.Count == 0 && quitar.Count == 0)
            errores.Add("La solicitud no contiene cambios.");

        if (errores.Any())
        {
            return BadRequest(new { errores });
        }

        var nuevaSolicitud = new Solicitud
        {
            TrabajadorId = actual.Id,
            CreadoPorId = creadoPorId.Value,
            Tipo = "Modificación",
            Estado = "Pendiente",
            FechaCreacion = ahora,
            FechaVencimientoSLA = ahora.AddDays(4)
        };

        _context.Solicitudes.Add(nuevaSolicitud);
        await _context.SaveChangesAsync();

        // TODO: aplicar estos valores y accesos al trabajador cuando la solicitud se complete.
        _context.SolicitudesModificacion.Add(new SolicitudModificacion
        {
            SolicitudId = nuevaSolicitud.Id,
            Justificacion = dto.Justificacion.Trim(),
            PrimerNombre = propuesto.PrimerNombre,
            SegundoNombre = propuesto.SegundoNombre,
            PrimerApellido = propuesto.PrimerApellido,
            SegundoApellido = propuesto.SegundoApellido,
            FechaNacimiento = propuesto.FechaNacimiento,
            Sexo = propuesto.Sexo,
            Correo = propuesto.Correo,
            DireccionCorporativaId = propuesto.DireccionCorporativaId,
            AreaId = propuesto.AreaId,
            CargoId = propuesto.CargoId,
            LugarTrabajoId = propuesto.LugarTrabajoId,
            FechaIncorporacion = propuesto.FechaIncorporacion,
            DireccionDomicilio = propuesto.DireccionDomicilio,
            JefeDirecto = propuesto.JefeDirecto,
            HomologarAccesosDesde = propuesto.HomologarAccesosDesde,
            TieneTelefonoCorporativo = propuesto.TieneTelefonoCorporativo,
            SolicitaTelefono = propuesto.SolicitaTelefono
        });

        foreach (var catalogoId in agregar)
        {
            _context.SolicitudDetalles.Add(new SolicitudDetalle
            {
                SolicitudId = nuevaSolicitud.Id,
                CatalogoId = catalogoId,
                Accion = "Agregar"
            });
        }
        foreach (var catalogoId in quitar)
        {
            _context.SolicitudDetalles.Add(new SolicitudDetalle
            {
                SolicitudId = nuevaSolicitud.Id,
                CatalogoId = catalogoId,
                Accion = "Quitar"
            });
        }

        _context.HistorialSolicitudes.Add(new HistorialSolicitud
        {
            SolicitudId = nuevaSolicitud.Id,
            Accion = "Creada",
            RealizadoPorId = creadoPorId.Value,
            Fecha = ahora
        });

        await _context.SaveChangesAsync();
        await transaccion.CommitAsync();

        return CreatedAtAction(nameof(GetById), new { id = nuevaSolicitud.Id }, new SolicitudModificacionResultadoDto
        {
            SolicitudId = nuevaSolicitud.Id,
            FechaVencimientoSLA = nuevaSolicitud.FechaVencimientoSLA
        });
    }

    // ---------- Cambios de estado ----------

    [HttpPost("{id}/aprobar")]
    [RequierePermiso("RevisarAprobarBloqueo")]
    public Task<ActionResult<CambioEstadoResultadoDto>> Aprobar(int id) =>
        CambiarEstadoAsync(id, s => TransicionSolicitud.ValidarAprobar(s.Tipo, s.Estado),
            TransicionSolicitud.EnProceso, "Aprobada");

    [HttpPost("{id}/iniciar")]
    [RequierePermiso("MarcarCompletada")]
    public Task<ActionResult<CambioEstadoResultadoDto>> Iniciar(int id) =>
        CambiarEstadoAsync(id, s => TransicionSolicitud.ValidarIniciar(s.Tipo, s.Estado),
            TransicionSolicitud.EnProceso, "Iniciada");

    [HttpPost("{id}/rechazar")]
    [RequierePermiso("RechazarSolicitud")]
    public Task<ActionResult<CambioEstadoResultadoDto>> Rechazar(int id, RechazarSolicitudDto dto) =>
        CambiarEstadoAsync(id, s => TransicionSolicitud.ValidarRechazar(s.Estado, dto.Motivo),
            TransicionSolicitud.Rechazada, "Rechazada", dto.Motivo?.Trim(), AplicarRechazoAsync);

    [HttpPost("{id}/completar")]
    [RequierePermiso("MarcarCompletada")]
    public Task<ActionResult<CambioEstadoResultadoDto>> Completar(int id) =>
        CambiarEstadoAsync(id, s => TransicionSolicitud.ValidarCompletar(s.Estado),
            TransicionSolicitud.Completada, "Completada", null, AplicarResultadoAsync);

    // Valida la transición, aplica el efecto (si hay), cambia el estado y deja el paso en el historial,
    // todo en una sola transacción.
    private async Task<ActionResult<CambioEstadoResultadoDto>> CambiarEstadoAsync(
        int id, Func<Solicitud, string?> validar, string nuevoEstado, string accion,
        string? comentario = null, Func<Solicitud, DateTime, Task>? efecto = null)
    {
        var usuarioId = await UsuarioActualIdAsync();
        if (usuarioId == null) return SinUsuarioParaRol();

        using var transaccion = await _context.Database.BeginTransactionAsync();

        var s = await _context.Solicitudes.Include(x => x.Trabajador).FirstOrDefaultAsync(x => x.Id == id);
        if (s == null) return NotFound();

        var error = validar(s);
        if (error != null) return BadRequest(new { errores = new List<string> { error } });

        var ahora = DateTime.UtcNow;
        if (efecto != null) await efecto(s, ahora);

        s.Estado = nuevoEstado;
        if (nuevoEstado == TransicionSolicitud.Rechazada) s.MotivoRechazo = comentario;

        _context.HistorialSolicitudes.Add(new HistorialSolicitud
        {
            SolicitudId = s.Id,
            Accion = accion,
            RealizadoPorId = usuarioId.Value,
            Fecha = ahora,
            Comentario = comentario
        });

        await _context.SaveChangesAsync();
        await transaccion.CommitAsync();

        return Ok(new CambioEstadoResultadoDto { SolicitudId = s.Id, Estado = s.Estado });
    }

    // Un Ingreso rechazado no debe dejar al trabajador como activo (se creó junto con la solicitud).
    private Task AplicarRechazoAsync(Solicitud s, DateTime ahora)
    {
        if (s.Tipo == "Ingreso") s.Trabajador!.Activo = false;
        return Task.CompletedTask;
    }

    // Lo que significa "completar" para cada tipo de solicitud.
    private async Task AplicarResultadoAsync(Solicitud s, DateTime ahora)
    {
        var t = s.Trabajador!;
        var accesosActivos = await _context.Accesos
            .Where(a => a.TrabajadorId == t.Id && a.Estado == "Activo")
            .ToListAsync();
        var detalles = await _context.SolicitudDetalles.Where(d => d.SolicitudId == s.Id).ToListAsync();

        void OtorgarAccesos()
        {
            foreach (var d in detalles.Where(d => d.Accion == "Agregar"))
            {
                if (accesosActivos.Any(a => a.CatalogoId == d.CatalogoId)) continue;
                _context.Accesos.Add(new Acceso
                {
                    TrabajadorId = t.Id, CatalogoId = d.CatalogoId, SolicitudId = s.Id,
                    FechaOtorgado = ahora, Estado = "Activo"
                });
            }
        }

        switch (s.Tipo)
        {
            case "Ingreso":
                OtorgarAccesos();
                break;

            case "Modificación":
                var m = await _context.SolicitudesModificacion.FirstAsync(x => x.SolicitudId == s.Id);
                t.PrimerNombre = m.PrimerNombre;
                t.SegundoNombre = m.SegundoNombre;
                t.PrimerApellido = m.PrimerApellido;
                t.SegundoApellido = m.SegundoApellido;
                t.FechaNacimiento = m.FechaNacimiento;
                t.Sexo = m.Sexo;
                t.Correo = m.Correo;
                t.DireccionCorporativaId = m.DireccionCorporativaId;
                t.AreaId = m.AreaId;
                t.CargoId = m.CargoId;
                t.LugarTrabajoId = m.LugarTrabajoId;
                t.FechaIncorporacion = m.FechaIncorporacion;
                t.DireccionDomicilio = m.DireccionDomicilio;
                t.JefeDirecto = m.JefeDirecto;
                t.HomologarAccesosDesde = m.HomologarAccesosDesde;
                t.TieneTelefonoCorporativo = m.TieneTelefonoCorporativo;
                t.SolicitaTelefono = m.SolicitaTelefono;

                OtorgarAccesos();
                var quitar = detalles.Where(d => d.Accion == "Quitar").Select(d => d.CatalogoId).ToList();
                foreach (var a in accesosActivos.Where(a => quitar.Contains(a.CatalogoId))) a.Estado = "Revocado";
                break;

            case "Bloqueo":
                var b = await _context.SolicitudesBloqueo.FirstAsync(x => x.SolicitudId == s.Id);
                // TODO: el bloqueo temporal no cambia al trabajador; falta reactivar/suspender según FechaInicio/FechaFin.
                if (!b.EsTemporal)
                {
                    t.Activo = false;
                    t.FechaSalida = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(ahora, HoraChile.Zona()));
                    foreach (var a in accesosActivos) a.Estado = "Revocado";
                }
                break;
        }
    }

    private static bool MismoTexto(string? a, string? b) => (a ?? "").Trim() == (b ?? "").Trim();

    // Quien realiza la acción es el usuario actual (ver UsuarioActual), no el creadoPorId que envía el cliente.
    private async Task<int?> UsuarioActualIdAsync() =>
        (await UsuarioActual.ObtenerAsync(_context, Request))?.Id;

    private ObjectResult SinUsuarioParaRol() => StatusCode(403, new
    {
        error = $"No hay un usuario activo con el rol '{Request.Headers["X-Rol-Simulado"]}'."
    });

    // Npgsql exige Kind=Utc en columnas timestamptz; si el cliente no envía zona, se asume UTC.
    private static DateTime? AUtc(DateTime? fecha)
    {
        if (fecha == null) return null;
        return fecha.Value.Kind switch
        {
            DateTimeKind.Utc => fecha.Value,
            DateTimeKind.Local => fecha.Value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(fecha.Value, DateTimeKind.Utc)
        };
    }

    [HttpPost]
    public async Task<ActionResult<SolicitudDetalleDto>> Create(SolicitudCreateDto dto)
    {
        var nueva = new Solicitud
        {
            TrabajadorId = dto.TrabajadorId,
            CreadoPorId = dto.CreadoPorId,
            Tipo = dto.Tipo,
            Estado = "Pendiente",
            FechaCreacion = DateTime.UtcNow,
            FechaVencimientoSLA = DateTime.UtcNow.AddDays(4) 
        };

        _context.Solicitudes.Add(nueva);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = nueva.Id }, dto);
    }
}