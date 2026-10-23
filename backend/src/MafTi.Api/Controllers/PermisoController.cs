using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MafTi.Infrastructure;
using MafTi.Api.Dtos;
using MafTi.Api.Seguridad;

namespace MafTi.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
// Solo lectura (ver RolController): los permisos se crean con una migración revisada.
[RequierePermiso("AdministrarUsuariosYRoles")]
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
            .OrderBy(p => p.Codigo)
            .Select(p => new PermisoListaDto
            {
                Id = p.Id,
                Codigo = p.Codigo,
                Descripcion = p.Descripcion
            })
            .ToListAsync();
    }
}
