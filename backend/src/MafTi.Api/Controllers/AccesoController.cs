using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MafTi.Infrastructure;
using MafTi.Domain;
using MafTi.Api.Dtos;
using MafTi.Api.Seguridad;
using MafTi.Api.Common;

namespace MafTi.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AccesoController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly UsuarioActual _usuario;

    public AccesoController(AppDbContext context, UsuarioActual usuario)
    {
        _context = context;
        _usuario = usuario;
    }

    // Los accesos se otorgan por medio de solicitudes; aquí solo se consultan (Jefatura, solo de su área).
    [HttpGet]
    [RequierePermiso("ConsultarTrabajadoresYAccesos")]
    public async Task<ActionResult<PaginacionResultado<AccesoListaDto>>> GetAll(int pagina = 1, int tamanoPagina = 20)
    {
        var accesos = _context.Accesos.AsQueryable();
        if (_usuario.AreaRestringida is int areaPropia)
            accesos = accesos.Where(a => a.Trabajador!.AreaId == areaPropia);

        var query = accesos
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
}