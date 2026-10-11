using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MafTi.Infrastructure;
using MafTi.Api.Dtos;
using MafTi.Api.Seguridad;

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

    // Jefatura solo ve los datos de su área; el resto de los roles, la empresa completa.
    var areaPropia = (await UsuarioActual.ObtenerAsync(_context, Request))?.AreaId;
    var trabajadores = _context.Trabajadores.AsQueryable();
    var solicitudes = _context.Solicitudes.AsQueryable();
    var alcance = "Empresa completa";
    if (areaPropia.HasValue)
    {
        trabajadores = trabajadores.Where(t => t.AreaId == areaPropia);
        solicitudes = solicitudes.Where(s => s.Trabajador!.AreaId == areaPropia);
        alcance = await _context.Catalogos.Where(c => c.Id == areaPropia).Select(c => c.Nombre).FirstAsync();
    }

    var movimientos = new List<MovimientoMesDto>();
    for (int i = 5; i >= 0; i--)
    {
        var mes = inicioMes.AddMonths(-i);
        var siguiente = mes.AddMonths(1);
        movimientos.Add(new MovimientoMesDto
        {
            Mes = mes.ToString("yyyy-MM-dd"),
            Ingresos = await trabajadores.CountAsync(t => t.FechaIncorporacion >= mes && t.FechaIncorporacion < siguiente),
            Bajas = await trabajadores.CountAsync(t => t.FechaSalida != null && t.FechaSalida >= mes && t.FechaSalida < siguiente)
        });
    }

    var porTipo = await solicitudes
        .Where(s => s.Estado == "Pendiente")
        .GroupBy(s => s.Tipo)
        .Select(g => new ResumenTipoDto { Tipo = g.Key, Cantidad = g.Count() })
        .ToListAsync();

    return new ResumenDashboardDto
    {
        Alcance = alcance,
        DotacionActual = await trabajadores.CountAsync(t => t.Activo),
        IngresosDelMes = movimientos.Last().Ingresos,
        BajasDelMes = movimientos.Last().Bajas,
        SolicitudesPendientes = await solicitudes.CountAsync(s => s.Estado == "Pendiente"),
        SolicitudesUrgentes = await solicitudes.CountAsync(s => s.Estado == "Pendiente" && s.FechaVencimientoSLA < ahora),
        PorTipo = porTipo,
        Movimientos = movimientos
    };
}
}
