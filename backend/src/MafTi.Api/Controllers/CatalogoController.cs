using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MafTi.Application;
using MafTi.Infrastructure;
using MafTi.Domain;
using MafTi.Api.Dtos;
using MafTi.Api.Common;
using MafTi.Api.Seguridad;

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

    private static CatalogoListaDto ADto(Catalogo c) => new()
    {
        Id = c.Id, Tipo = c.Tipo, Nombre = c.Nombre, Activo = c.Activo, PadreId = c.PadreId
    };

    [HttpGet]
    public async Task<ActionResult<PaginacionResultado<CatalogoListaDto>>> GetAll(
        int pagina = 1, int tamanoPagina = 20, string? tipo = null, string? q = null, bool soloActivos = false, int? padreId = null)
        
    {
        var query = _context.Catalogos.AsQueryable();

        if (!string.IsNullOrEmpty(tipo)) query = query.Where(c => c.Tipo == tipo);
        if (padreId.HasValue) query = query.Where(c => c.PadreId == padreId.Value);
        if (soloActivos) query = query.Where(c => c.Activo);
        if (!string.IsNullOrWhiteSpace(q))
        {
            var texto = q.Trim().ToLower();
            query = query.Where(c => c.Nombre.ToLower().Contains(texto));
        }

        var resultado = await query
            .OrderBy(c => c.Nombre)
            .Select(c => new CatalogoListaDto { Id = c.Id, Tipo = c.Tipo, Nombre = c.Nombre, Activo = c.Activo ,PadreId = c.PadreId  })
            .PaginarAsync(pagina, tamanoPagina);

        return Ok(resultado);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<CatalogoListaDto>> GetById(int id)
    {
        var c = await _context.Catalogos.FindAsync(id);
        if (c == null) return NotFound();
        return ADto(c);
    }

    [HttpPost]
    [RequierePermiso("AdministrarCatalogos")]
    public async Task<ActionResult<CatalogoListaDto>> Create(CatalogoCreateDto dto)
    {
        var nombre = CatalogoValidator.Normalizar(dto.Nombre);

        var errores = await CatalogoValidator.ValidarAsync(dto.Tipo, nombre,
            async n => await _context.Catalogos.AnyAsync(c => c.Tipo == dto.Tipo && c.Nombre.ToLower() == n.ToLower()));
        if (errores.Any()) return BadRequest(new { errores });

        var nuevo = new Catalogo { Tipo = dto.Tipo, Nombre = nombre, Activo = true };
        _context.Catalogos.Add(nuevo);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = nuevo.Id }, ADto(nuevo));
    }

    [HttpPut("{id}")]
    [RequierePermiso("AdministrarCatalogos")]
    public async Task<ActionResult<CatalogoListaDto>> Update(int id, CatalogoUpdateDto dto)
    {
        var c = await _context.Catalogos.FindAsync(id);
        if (c == null) return NotFound();

        var nombre = CatalogoValidator.Normalizar(dto.Nombre);

        var errores = await CatalogoValidator.ValidarAsync(c.Tipo, nombre,
            async n => await _context.Catalogos.AnyAsync(x => x.Tipo == c.Tipo && x.Id != id && x.Nombre.ToLower() == n.ToLower()));
        if (errores.Any()) return BadRequest(new { errores });

        c.Nombre = nombre;
        await _context.SaveChangesAsync();
        return ADto(c);
    }

    [HttpPatch("{id}/activo")]
    [RequierePermiso("AdministrarCatalogos")]
    public async Task<ActionResult<CatalogoListaDto>> CambiarActivo(int id, [FromQuery] bool activo)
    {
        var c = await _context.Catalogos.FindAsync(id);
        if (c == null) return NotFound();

        if (!CatalogoValidator.TiposAdministrables.Contains(c.Tipo))
            return BadRequest(new { errores = new[] { "Solo se pueden administrar sistemas y carpetas de red." } });

        c.Activo = activo;
        await _context.SaveChangesAsync();
        return ADto(c);
    }
}