using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MafTi.Infrastructure;
using MafTi.Api.Seguridad;

namespace MafTi.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly UsuarioActual _usuario;

    public AuthController(AppDbContext context, UsuarioActual usuario)
    {
        _context = context;
        _usuario = usuario;
    }

    // Usuario de la sesión, con su rol y permisos (el middleware ya exigió sesión válida).
    [HttpGet("yo")]
    public async Task<IActionResult> Yo()
    {
        var areaNombre = _usuario.AreaId.HasValue
            ? await _context.Catalogos.Where(c => c.Id == _usuario.AreaId).Select(c => c.Nombre).FirstOrDefaultAsync()
            : null;
        var permisos = await _context.RolPermisos
            .Where(rp => rp.RolId == _usuario.RolId)
            .Select(rp => rp.Permiso!.Codigo)
            .OrderBy(c => c)
            .ToListAsync();

        return Ok(new
        {
            userId = _usuario.Id,
            correo = _usuario.Correo,
            nombre = _usuario.Nombre,
            rol = _usuario.Rol,
            areaId = _usuario.AreaId,
            areaNombre,
            // "area" = solo ve su área (Jefatura); "empresa" = ve todo. Lo decide UsuarioActual.AreaRestringida.
            alcance = _usuario.AreaRestringida.HasValue ? "area" : "empresa",
            permisos
        });
    }
}
