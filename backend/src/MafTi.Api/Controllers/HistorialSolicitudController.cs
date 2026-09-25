using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MafTi.Infrastructure;
using MafTi.Domain;
using MafTi.Api.Dtos;
using MafTi.Api.Common;

namespace MafTi.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HistorialSolicitudController : ControllerBase
{
    private readonly AppDbContext _context;

    public HistorialSolicitudController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<PaginacionResultado<HistorialSolicitudListaDto>>> GetAll(int pagina = 1, int tamanoPagina = 20)
    {
        var query = _context.HistorialSolicitudes
            .OrderByDescending(h => h.Fecha)
            .Select(h => new HistorialSolicitudListaDto
            {
                Id = h.Id,
                SolicitudId = h.SolicitudId,
                Accion = h.Accion,
                RealizadoPorCorreo = h.RealizadoPor!.Trabajador!.Correo,
                Fecha = h.Fecha,
                Comentario = h.Comentario
            });

        var resultado = await query.PaginarAsync(pagina, tamanoPagina);
        return Ok(resultado);
    }

    [HttpPost]
    public async Task<ActionResult<HistorialSolicitudListaDto>> Create(HistorialSolicitudCreateDto dto)
    {
        var nuevo = new HistorialSolicitud
        {
            SolicitudId = dto.SolicitudId,
            Accion = dto.Accion,
            RealizadoPorId = dto.RealizadoPorId,
            Fecha = DateTime.UtcNow,
            Comentario = dto.Comentario
        };

        _context.HistorialSolicitudes.Add(nuevo);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetAll), new { id = nuevo.Id }, dto);
    }
}