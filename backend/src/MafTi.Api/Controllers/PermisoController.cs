using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MafTi.Infrastructure;
using MafTi.Domain;
using MafTi.Api.Dtos;

namespace MafTi.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PermisoController : ControllerBase
{
    private readonly AppDbContext _context;

    public PermisoController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<PermisoListaDto>>> GetAll()
    {
        return await _context.Permisos
            .Select(p => new PermisoListaDto
            {
                Id = p.Id,
                Codigo = p.Codigo,
                Descripcion = p.Descripcion
            })
            .ToListAsync();
    }

    [HttpPost]
    public async Task<ActionResult<PermisoListaDto>> Create(PermisoCreateDto dto)
    {
        var nuevo = new Permiso
        {
            Codigo = dto.Codigo,
            Descripcion = dto.Descripcion
        };

        _context.Permisos.Add(nuevo);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetAll), new { id = nuevo.Id }, dto);
    }
}