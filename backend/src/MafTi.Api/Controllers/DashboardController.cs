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
    var ahora = DateTime.UtcNow;
    var hoy = DateOnly.FromDateTime(ahora);
    var inicioMes = new DateOnly(hoy.Year, hoy.Month, 1);

    var movimientos = new List<MovimientoMesDto>();
    for (int i = 5; i >= 0; i--)
    {
        var mes = inicioMes.AddMonths(-i);
        var siguiente = mes.AddMonths(1);
        movimientos.Add(new MovimientoMesDto
        {
            Mes = mes.ToString("yyyy-MM-dd"),
            Ingresos = await _context.Trabajadores.CountAsync(t => t.FechaIncorporacion >= mes && t.FechaIncorporacion < siguiente),
            Bajas = await _context.Trabajadores.CountAsync(t => t.FechaSalida != null && t.FechaSalida >= mes && t.FechaSalida < siguiente)
        });
    }

    var porTipo = await _context.Solicitudes
        .Where(s => s.Estado == "Pendiente")
        .GroupBy(s => s.Tipo)
        .Select(g => new ResumenTipoDto { Tipo = g.Key, Cantidad = g.Count() })
        .ToListAsync();

    return new ResumenDashboardDto
    {
        DotacionActual = await _context.Trabajadores.CountAsync(t => t.Activo),
        IngresosDelMes = movimientos.Last().Ingresos,
        BajasDelMes = movimientos.Last().Bajas,
        SolicitudesPendientes = await _context.Solicitudes.CountAsync(s => s.Estado == "Pendiente"),
        SolicitudesUrgentes = await _context.Solicitudes.CountAsync(s => s.Estado == "Pendiente" && s.FechaVencimientoSLA < ahora),
        PorTipo = porTipo,
        Movimientos = movimientos
    };
}
}
