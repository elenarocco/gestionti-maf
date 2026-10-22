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
public class HistorialSolicitudController : ControllerBase
{
    private readonly AppDbContext _context;

    public HistorialSolicitudController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<PaginacionResultado<HistorialSolicitudListaDto>>> GetAll(int pagina = 1, int tamanoPagina = 20)
    {
        var query = _context.HistorialSolicitudes
            .OrderByDescending(h => h.Fecha)
            .Select(h => new HistorialSolicitudListaDto
            {
                Id = h.Id,
                SolicitudId = h.SolicitudId,
                Accion = h.Accion,
                RealizadoPorCorreo = h.RealizadoPor!.Trabajador!.Correo,
                Fecha = h.Fecha,
                Comentario = h.Comentario
            });

        var resultado = await query.PaginarAsync(pagina, tamanoPagina);
        return Ok(resultado);
    }
}