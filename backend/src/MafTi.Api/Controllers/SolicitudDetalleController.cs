using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MafTi.Infrastructure;
using MafTi.Domain;
using MafTi.Api.Dtos;
using MafTi.Api.Common;

namespace MafTi.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SolicitudDetalleController : ControllerBase
{
    private readonly AppDbContext _context;

    public SolicitudDetalleController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<PaginacionResultado<SolicitudDetalleListaDto>>> GetAll(int pagina = 1, int tamanoPagina = 20)
    {
        var query = _context.SolicitudDetalles
            .OrderBy(sd => sd.Id)
            .Select(sd => new SolicitudDetalleListaDto
            {
                Id = sd.Id,
                SolicitudId = sd.SolicitudId,
                CatalogoNombre = sd.Catalogo!.Nombre
            });

        var resultado = await query.PaginarAsync(pagina, tamanoPagina);
        return Ok(resultado);
    }

    [HttpPost]
    public async Task<ActionResult<SolicitudDetalleListaDto>> Create(SolicitudDetalleCreateDto dto)
    {
        var nuevo = new SolicitudDetalle
        {
            SolicitudId = dto.SolicitudId,
            CatalogoId = dto.CatalogoId
        };

        _context.SolicitudDetalles.Add(nuevo);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetAll), new { id = nuevo.Id }, dto);
    }
}