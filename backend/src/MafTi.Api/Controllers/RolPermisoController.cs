using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MafTi.Infrastructure;
using MafTi.Domain;
using MafTi.Api.Dtos;
using MafTi.Api.Seguridad;

namespace MafTi.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
// Quien puede crear roles, asignar permisos o dar acceso a usuarios puede darse cualquier permiso:
// solo Super Admin (AdministrarUsuariosYRoles).
[RequierePermiso("AdministrarUsuariosYRoles")]
public class RolPermisoController : ControllerBase
{
    private readonly AppDbContext _context;

    public RolPermisoController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<RolPermisoListaDto>>> GetAll()
    {
        return await _context.RolPermisos
            .Select(rp => new RolPermisoListaDto
            {
                Id = rp.Id,
                RolNombre = rp.Rol!.Nombre,
                PermisoCodigo = rp.Permiso!.Codigo
            })
            .ToListAsync();
    }

    [HttpPost]
    public async Task<ActionResult<RolPermisoListaDto>> Create(RolPermisoCreateDto dto)
    {
        var nuevo = new RolPermiso
        {
            RolId = dto.RolId,
            PermisoId = dto.PermisoId
        };

        _context.RolPermisos.Add(nuevo);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetAll), new { id = nuevo.Id }, dto);
    }
}