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
// Solo lectura y solo para quien ve todas las solicitudes. El historial lo escriben los propios
// endpoints de solicitudes; no se acepta escritura directa.
[RequierePermiso("ConsultarTodasLasSolicitudes")]
public class SolicitudDetalleController : ControllerBase
{
    private readonly AppDbContext _context;

    public SolicitudDetalleController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<PaginacionResultado<SolicitudDetalleListaDto>>> GetAll(int pagina = 1, int tamanoPagina = 20)
    {
        var query = _context.SolicitudDetalles
            .OrderBy(sd => sd.Id)
            .Select(sd => new SolicitudDetalleListaDto
            {
                Id = sd.Id,
                SolicitudId = sd.SolicitudId,
                CatalogoNombre = sd.Catalogo!.Nombre
            });

        var resultado = await query.PaginarAsync(pagina, tamanoPagina);
        return Ok(resultado);
    }
}