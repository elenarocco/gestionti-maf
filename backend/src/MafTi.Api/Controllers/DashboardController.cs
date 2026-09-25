using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MafTi.Infrastructure;
using MafTi.Api.Dtos;

namespace MafTi.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DashboardController : ControllerBase
{
    private readonly AppDbContext _context;

    public DashboardController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet("resumen")]
    public async Task<ActionResult<ResumenDashboardDto>> GetResumen()
    {
        return new ResumenDashboardDto
        {
            DotacionActual = await _context.Trabajadores.CountAsync(t => t.Activo),
            SolicitudesPendientes = await _context.Solicitudes.CountAsync(s => s.Estado == "Pendiente"),
            TrabajadoresInactivos = await _context.Trabajadores.CountAsync(t => !t.Activo)
        };
    }
}