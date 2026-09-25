using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MafTi.Infrastructure;
using MafTi.Domain;
using MafTi.Api.Dtos;

namespace MafTi.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RolController : ControllerBase
{
    private readonly AppDbContext _context;

    public RolController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<RolListaDto>>> GetAll()
    {
        return await _context.Roles
            .Select(r => new RolListaDto
            {
                Id = r.Id,
                Nombre = r.Nombre,
                Descripcion = r.Descripcion
            })
            .ToListAsync();
    }

    [HttpPost]
    public async Task<ActionResult<RolListaDto>> Create(RolCreateDto dto)
    {
        var nuevo = new Rol
        {
            Nombre = dto.Nombre,
            Descripcion = dto.Descripcion
        };

        _context.Roles.Add(nuevo);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetAll), new { id = nuevo.Id }, dto);
    }
}