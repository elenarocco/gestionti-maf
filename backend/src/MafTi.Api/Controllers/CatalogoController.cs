using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MafTi.Infrastructure;
using MafTi.Domain;
using MafTi.Api.Dtos;
using MafTi.Api.Common;

namespace MafTi.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CatalogoController : ControllerBase
{
    private readonly AppDbContext _context;

    public CatalogoController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<PaginacionResultado<CatalogoListaDto>>> GetAll(int pagina = 1, int tamanoPagina = 20, string? tipo = null)
    {
        var query = _context.Catalogos.AsQueryable();

        if (!string.IsNullOrEmpty(tipo))
        {
            query = query.Where(c => c.Tipo == tipo);
        }

        var resultado = await query
            .OrderBy(c => c.Id)
            .Select(c => new CatalogoListaDto
            {
                Id = c.Id,
                Tipo = c.Tipo,
                Nombre = c.Nombre,
                Activo = c.Activo
            })
            .PaginarAsync(pagina, tamanoPagina);

        return Ok(resultado);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<CatalogoListaDto>> GetById(int id)
    {
        var c = await _context.Catalogos.FindAsync(id);
        if (c == null) return NotFound();

        return new CatalogoListaDto
        {
            Id = c.Id,
            Tipo = c.Tipo,
            Nombre = c.Nombre,
            Activo = c.Activo
        };
    }

    [HttpPost]
    public async Task<ActionResult<CatalogoListaDto>> Create(CatalogoCreateDto dto)
    {
        var nuevo = new Catalogo
        {
            Tipo = dto.Tipo,
            Nombre = dto.Nombre,
            Activo = true
        };

        _context.Catalogos.Add(nuevo);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = nuevo.Id }, dto);
    }
}