using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MafTi.Infrastructure;
using MafTi.Domain;
using MafTi.Api.Dtos;
using MafTi.Api.Common;

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