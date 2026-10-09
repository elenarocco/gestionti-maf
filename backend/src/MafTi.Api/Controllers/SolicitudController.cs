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
            Bloqueo = bloqueo
        };
    }

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
            CreadoPorId = dto.CreadoPorId,
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
        if (errores.Any())
        {
            return BadRequest(new { errores });
        }

        var nuevaSolicitud = new Solicitud
        {
            TrabajadorId = dto.TrabajadorId,
            CreadoPorId = dto.CreadoPorId,
            Tipo = "Bloqueo",
            Estado = "Pendiente",
            FechaCreacion = ahora,
            FechaVencimientoSLA = ahora.AddDays(4)
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
            RealizadoPorId = dto.CreadoPorId,
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