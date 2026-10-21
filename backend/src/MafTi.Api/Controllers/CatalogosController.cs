using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MafTi.Infrastructure;
using MafTi.Api.Dtos;

namespace MafTi.Api.Controllers;

// Opciones de un catálogo para los formularios (contrato: GET /api/catalogos?tipo=MotivoVpn → [ { id, nombre } ]).
// La administración de catálogos sigue en CatalogoController (/api/Catalogo).
[ApiController]
[Route("api/catalogos")]
public class CatalogosController : ControllerBase
{
    private readonly AppDbContext _context;

    public CatalogosController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<List<CatalogoOpcionDto>>> Opciones(string? tipo)
    {
        if (string.IsNullOrWhiteSpace(tipo)) return BadRequest(new { error = "Debe indicar el tipo de catálogo." });

        return await _context.Catalogos
            .Where(c => c.Tipo == tipo && c.Activo)
            .OrderBy(c => c.Nombre)
            .Select(c => new CatalogoOpcionDto { Id = c.Id, Nombre = c.Nombre })
            .ToListAsync();
    }
}
