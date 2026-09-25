using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MafTi.Infrastructure;
using MafTi.Domain;
using MafTi.Api.Dtos;
using MafTi.Api.Common;

namespace MafTi.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsuarioSistemaController : ControllerBase
{
    private readonly AppDbContext _context;

    public UsuarioSistemaController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<PaginacionResultado<UsuarioSistemaListaDto>>> GetAll(int pagina = 1, int tamanoPagina = 20)
    {
        var query = _context.UsuariosSistema
            .OrderBy(u => u.Id)
            .Select(u => new UsuarioSistemaListaDto
            {
                Id = u.Id,
                TrabajadorNombre = u.Trabajador!.PrimerNombre + " " + u.Trabajador.PrimerApellido,
                TrabajadorCorreo = u.Trabajador.Correo,
                RolNombre = u.Rol!.Nombre,
                AreaNombre = u.Area != null ? u.Area.Nombre : null,
                Activo = u.Activo
            });

        var resultado = await query.PaginarAsync(pagina, tamanoPagina);
        return Ok(resultado);
    }

    [HttpPost]
    public async Task<ActionResult<UsuarioSistemaListaDto>> Create(UsuarioSistemaCreateDto dto)
    {
        var nuevo = new UsuarioSistema
        {
            TrabajadorId = dto.TrabajadorId,
            RolId = dto.RolId,
            AreaId = dto.AreaId,
            Activo = true
        };

        _context.UsuariosSistema.Add(nuevo);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetAll), new { id = nuevo.Id }, dto);
    }
}