using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MafTi.Infrastructure;
using MafTi.Api.Dtos;
using MafTi.Api.Seguridad;

namespace MafTi.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
// Solo lectura: los roles y sus permisos se cambian con una migración revisada, no desde la app,
// porque un cambio afecta a todos los usuarios del rol. Solo Super Admin (AdministrarUsuariosYRoles).
[RequierePermiso("AdministrarUsuariosYRoles")]
public class RolController : ControllerBase
{
    private readonly AppDbContext _context;

    public RolController(AppDbContext context)
    {
        _context = context;
    }

    // Roles con sus permisos y cuántos usuarios activos tienen (para asignar roles y ver qué puede hacer cada uno).
    [HttpGet]
    public async Task<ActionResult<IEnumerable<RolListaDto>>> GetAll()
    {
        return await _context.Roles
            .OrderBy(r => r.Nombre)
            .Select(r => new RolListaDto
            {
                Id = r.Id,
                Nombre = r.Nombre,
                Descripcion = r.Descripcion,
                Permisos = _context.RolPermisos.Where(rp => rp.RolId == r.Id).Select(rp => rp.Permiso!.Codigo).ToList(),
                UsuariosActivos = _context.UsuariosSistema.Count(u => u.RolId == r.Id && u.Activo)
            })
            .ToListAsync();
    }
}
