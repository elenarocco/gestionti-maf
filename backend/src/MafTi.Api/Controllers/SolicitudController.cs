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

    [HttpGet]
    public async Task<ActionResult<PaginacionResultado<SolicitudListaDto>>> GetAll(int pagina = 1, int tamanoPagina = 20)
    {
        var query = _context.Solicitudes
            .OrderByDescending(s => s.FechaCreacion)
            .Select(s => new SolicitudListaDto
            {
                Id = s.Id,
                EsUrgente = s.Estado == "Pendiente" && s.FechaVencimientoSLA < DateTime.UtcNow,
                TrabajadorNombre = s.Trabajador!.PrimerNombre + " " + s.Trabajador.PrimerApellido,
                Tipo = s.Tipo,
                Estado = s.Estado,
                FechaCreacion = s.FechaCreacion,
                FechaVencimientoSLA = s.FechaVencimientoSLA
            });

        var resultado = await query.PaginarAsync(pagina, tamanoPagina);
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
            FechaVencimientoSLA = s.FechaVencimientoSLA
        };
    }

    [HttpPost("ingreso")]
    [RequierePermiso("CrearSolicitudIngreso")]
    public async Task<ActionResult<IngresoResultadoDto>> CrearIngreso(IngresoCreateDto dto)
    {
        var nuevoTrabajador = new Trabajador
        {
            Rut = TrabajadorValidator.FormatearParaGuardar(dto.Rut),
            PrimerNombre = dto.PrimerNombre,
            SegundoNombre = dto.SegundoNombre,
            PrimerApellido = dto.PrimerApellido,
            SegundoApellido = dto.SegundoApellido,
            FechaNacimiento = dto.FechaNacimiento,
            Sexo = dto.Sexo,
            Correo = dto.Correo,
            DireccionCorporativaId = dto.DireccionCorporativaId,
            AreaId = dto.AreaId,
            Cargo = dto.Cargo,
            LugarTrabajoId = dto.LugarTrabajoId,
            EsCuentaGenerica = dto.EsCuentaGenerica,
            FechaIncorporacion = dto.FechaIncorporacion,
            DireccionDomicilio = dto.DireccionDomicilio,
            JefeDirecto = dto.JefeDirecto,
            HomologarAccesosDesde = dto.HomologarAccesosDesde,
            TieneTelefonoCorporativo = dto.TieneTelefonoCorporativo,
            SolicitaTelefono = dto.SolicitaTelefono,
            Activo = true
        };

        var errores = await TrabajadorValidator.ValidarAsync(nuevoTrabajador, async correo =>
        await _context.Trabajadores.AnyAsync(t => t.Correo.ToLower() == correo));

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