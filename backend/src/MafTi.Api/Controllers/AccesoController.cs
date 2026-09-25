using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MafTi.Infrastructure;
using MafTi.Domain;
using MafTi.Api.Dtos;
using MafTi.Api.Common;

namespace MafTi.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AccesoController : ControllerBase
{
    private readonly AppDbContext _context;

    public AccesoController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<PaginacionResultado<AccesoListaDto>>> GetAll(int pagina = 1, int tamanoPagina = 20)
    {
        var query = _context.Accesos
            .OrderByDescending(a => a.FechaOtorgado)
            .Select(a => new AccesoListaDto
            {
                Id = a.Id,
                TrabajadorNombre = a.Trabajador!.PrimerNombre + " " + a.Trabajador.PrimerApellido,
                CatalogoNombre = a.Catalogo!.Nombre,
                FechaOtorgado = a.FechaOtorgado,
                Estado = a.Estado
            });

        var resultado = await query.PaginarAsync(pagina, tamanoPagina);
        return Ok(resultado);
    }

    [HttpPost]
    public async Task<ActionResult<AccesoListaDto>> Create(AccesoCreateDto dto)
    {
        var nuevo = new Acceso
        {
            TrabajadorId = dto.TrabajadorId,
            CatalogoId = dto.CatalogoId,
            SolicitudId = dto.SolicitudId,
            FechaOtorgado = DateTime.UtcNow,
            Estado = "Activo"
        };

        _context.Accesos.Add(nuevo);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetAll), new { id = nuevo.Id }, dto);
    }
}