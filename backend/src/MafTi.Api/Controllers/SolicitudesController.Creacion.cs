using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MafTi.Infrastructure;
using MafTi.Domain;
using MafTi.Api.Dtos;
using MafTi.Api.Common;
using MafTi.Application;
using MafTi.Api.Seguridad;
namespace MafTi.Api.Controllers;

// Creación de solicitudes de Ingreso, Bloqueo y Modificación (POST api/solicitudes/ingreso|bloqueo|modificacion).
// Es parte de SolicitudesController: una sola API de solicitudes en api/solicitudes.
public partial class SolicitudesController
{
    private static readonly Dictionary<string, string> PermisoPorTipo = new()
{
    { "Ingreso", "CrearSolicitudIngreso" },
    { "Modificación", "CrearSolicitudModificacion" },
    { "Bloqueo", "CrearSolicitudBloqueo" },
    { "VPN", "SolicitarAccesoVPN" }
};

    [HttpPost("ingreso")]
    [RequierePermiso("CrearSolicitudIngreso")]
    public async Task<ActionResult<IngresoResultadoDto>> CrearIngreso(IngresoCreateDto dto)
    {
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
            // El correo de un registro inactivo de la misma persona (mismo RUT) se puede reutilizar: reincorporación.
            async correo => await _context.Trabajadores.AnyAsync(t => t.Correo.ToLower() == correo
                && !(t.Rut == nuevoTrabajador.Rut && !t.Activo)),
            async rut => await _context.Trabajadores.AnyAsync(t => t.Rut == rut && t.Activo),
            async (areaId, direccionId) => await _context.Catalogos.AnyAsync(
                c => c.Id == areaId && c.Tipo == "Area" && c.PadreId == direccionId));

        if (await ValidarOrigenAsync(dto.SolicitudOrigenId, "Ingreso", rut: nuevoTrabajador.Rut) is string errorOrigenIngreso)
            errores.Add(errorOrigenIngreso);

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
            CreadoPorId = _usuario.Id,
            SolicitudOrigenId = dto.SolicitudOrigenId,
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
            RealizadoPorId = _usuario.Id,
            Fecha = nuevaSolicitud.FechaCreacion,
            Comentario = ComentarioCorreccion(dto.SolicitudOrigenId)
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
        if (await ValidarOrigenAsync(dto.SolicitudOrigenId, "Bloqueo", trabajadorId: dto.TrabajadorId) is string errorOrigenBloqueo)
            errores.Add(errorOrigenBloqueo);
        if (errores.Any())
        {
            return BadRequest(new { errores });
        }

        var nuevaSolicitud = new Solicitud
        {
            TrabajadorId = dto.TrabajadorId,
            CreadoPorId = _usuario.Id,
            SolicitudOrigenId = dto.SolicitudOrigenId,
            Tipo = "Bloqueo",
            // RRHH es quien crea el bloqueo, así que nace aprobado: queda En proceso para que TI lo complete.
            Estado = FlujoSolicitud.EstadoInicial("Bloqueo"),
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
            RealizadoPorId = _usuario.Id,
            Fecha = ahora,
            Comentario = ComentarioCorreccion(dto.SolicitudOrigenId)
        });

        await _context.SaveChangesAsync();
        await transaccion.CommitAsync();

        return CreatedAtAction(nameof(Detalle), new { id = nuevaSolicitud.Id }, new SolicitudBloqueoResultadoDto
        {
            SolicitudId = nuevaSolicitud.Id,
            FechaVencimientoSLA = nuevaSolicitud.FechaVencimientoSLA
        });
    }

    [HttpPost("modificacion")]
    [RequierePermiso("CrearSolicitudModificacion")]
    public async Task<ActionResult<SolicitudModificacionResultadoDto>> CrearModificacion(SolicitudModificacionCreateDto dto)
    {
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

        if (await ValidarOrigenAsync(dto.SolicitudOrigenId, "Modificación", trabajadorId: actual.Id) is string errorOrigenModificacion)
            errores.Add(errorOrigenModificacion);

        if (errores.Any())
        {
            return BadRequest(new { errores });
        }

        var nuevaSolicitud = new Solicitud
        {
            TrabajadorId = actual.Id,
            CreadoPorId = _usuario.Id,
            SolicitudOrigenId = dto.SolicitudOrigenId,
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
            RealizadoPorId = _usuario.Id,
            Fecha = ahora,
            // Una línea por dato: queda el "antes → después" aunque luego se aplique el cambio al trabajador.
            Comentario = CambiosModificacion.UnirLineas(new[] { ComentarioCorreccion(dto.SolicitudOrigenId) }
                .Concat(await CambiosModificacion.DescribirAsync(_context, actual, propuesto)))
        });

        await _context.SaveChangesAsync();
        await transaccion.CommitAsync();

        return CreatedAtAction(nameof(Detalle), new { id = nuevaSolicitud.Id }, new SolicitudModificacionResultadoDto
        {
            SolicitudId = nuevaSolicitud.Id,
            FechaVencimientoSLA = nuevaSolicitud.FechaVencimientoSLA
        });
    }

    // ---------- Corrección de solicitudes rechazadas (adaptado de b5e1422, feature/ariel) ----------

    // La corrección es una solicitud nueva enlazada a la original (que sigue Rechazada, sin reabrirse).
    // Solo la corrige quien la creó. En Ingreso se compara el RUT, porque cada Ingreso crea su propio trabajador.
    private async Task<string?> ValidarOrigenAsync(int? origenId, string tipo, int? trabajadorId = null, string? rut = null)
    {
        if (origenId == null) return null;

        var origen = await _context.Solicitudes.Include(x => x.Trabajador).FirstOrDefaultAsync(x => x.Id == origenId);
        if (origen == null) return "La solicitud a corregir no existe.";
        if (origen.CreadoPorId != _usuario.Id) return "Solo quien creó la solicitud puede corregirla.";
        if (origen.Tipo != tipo) return "La corrección debe ser del mismo tipo que la solicitud original.";
        if (origen.Estado != "Rechazada") return "Solo se puede corregir una solicitud Rechazada.";
        if (trabajadorId.HasValue && origen.TrabajadorId != trabajadorId) return "La corrección debe ser para el mismo trabajador.";
        if (rut != null && origen.Trabajador?.Rut != rut) return "La corrección debe ser para el mismo trabajador (mismo RUT).";
        if (await _context.Solicitudes.AnyAsync(x => x.SolicitudOrigenId == origenId)) return "Esa solicitud ya fue corregida.";
        return null;
    }

    private static string? ComentarioCorreccion(int? origenId) =>
        origenId.HasValue ? $"Corrige la solicitud N° {origenId}" : null;

    private static bool MismoTexto(string? a, string? b) => (a ?? "").Trim() == (b ?? "").Trim();

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
}
