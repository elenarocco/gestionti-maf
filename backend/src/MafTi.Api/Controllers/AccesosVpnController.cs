using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MafTi.Application;
using MafTi.Infrastructure;
using MafTi.Api.Common;
using MafTi.Api.Seguridad;

namespace MafTi.Api.Controllers;

// Registro de accesos VPN de proveedores (se llena al completar cada solicitud VPN). Lo consulta TI y
// quien puede ver todas las solicitudes (Auditoría, Gestión General).
[ApiController]
[Route("api/accesos-vpn")]
[RequierePermiso("GestionarBandejaTI", "ConsultarTodasLasSolicitudes")]
public class AccesosVpnController : ControllerBase
{
    private readonly AppDbContext _context;

    public AccesosVpnController(AppDbContext context)
    {
        _context = context;
    }

    // Estado = Vigente | PorVencer | Vencido y DiasRestantes los calcula ReglasVpn: el frontend solo los muestra.
    public record AccesoVpnDto(int Id, int SolicitudId, string Rut, string Nombre, string? Cargo, string? CompaniaExterna,
        string? Departamento, string? CuentaCorreo, string Motivo, DateOnly? FechaDesde, DateOnly? FechaHasta,
        string Estado, int? DiasRestantes, DateTime FechaRegistro);

    // estado = Vigentes (por defecto; incluye los por vencer) | Vencidos | Todos.
    [HttpGet]
    public async Task<ActionResult<PaginacionResultado<AccesoVpnDto>>> GetAll(
        string? q = null, string estado = "Vigentes", int pagina = 1, int tamanoPagina = 20)
    {
        var hoy = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, HoraChile.Zona()));
        var query = _context.AccesosVpn.AsQueryable();

        if (string.Equals(estado, "Vigentes", StringComparison.OrdinalIgnoreCase))
            query = query.Where(a => a.FechaHasta == null || a.FechaHasta >= hoy);
        else if (string.Equals(estado, "Vencidos", StringComparison.OrdinalIgnoreCase))
            query = query.Where(a => a.FechaHasta != null && a.FechaHasta < hoy);

        if (!string.IsNullOrWhiteSpace(q))
        {
            var patron = $"%{q.Trim()}%";
            query = query.Where(a =>
                EF.Functions.ILike(a.Nombres + " " + a.Apellidos, patron) ||
                EF.Functions.ILike(a.Rut, $"%{q.Replace(".", "").Trim()}%") ||
                EF.Functions.ILike(a.CompaniaExterna ?? "", patron));
        }

        var pagina_ = await query
            .OrderBy(a => a.FechaHasta == null).ThenBy(a => a.FechaHasta).ThenBy(a => a.Id)
            .PaginarAsync(pagina, tamanoPagina);

        return Ok(new PaginacionResultado<AccesoVpnDto>
        {
            Total = pagina_.Total,
            Pagina = pagina_.Pagina,
            TamanoPagina = pagina_.TamanoPagina,
            Datos = pagina_.Datos.Select(a =>
            {
                var situacion = ReglasVpn.Calcular(a.FechaHasta, hoy);
                return new AccesoVpnDto(a.Id, a.SolicitudId, a.Rut, a.Nombres + " " + a.Apellidos, a.Cargo, a.CompaniaExterna,
                    a.Departamento, a.CuentaCorreo, a.Motivo, a.FechaDesde, a.FechaHasta,
                    situacion.Estado, situacion.DiasRestantes, a.FechaRegistro);
            }).ToList()
        });
    }
}
