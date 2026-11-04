using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MafTi.Application;
using MafTi.Domain;
using MafTi.Infrastructure;
using MafTi.Api.Dtos;
using MafTi.Api.Seguridad;
using static MafTi.Application.SolicitudValores;

namespace MafTi.Api.Controllers;

// Sostenibilidad (computación verde): papel evitado al digitalizar los formularios vs. huella
// estimada del sistema, en gramos de CO2e. Son estimaciones con supuestos editables.
[ApiController]
[Route("api/sostenibilidad")]
public class SostenibilidadController : ControllerBase
{
    private const string PermisoVer = "VerSostenibilidad";                    // Admin TI, Auditoría, Gestión General
    private const string PermisoEditar = "EditarParametrosSostenibilidad";    // Gestión General

    private readonly AppDbContext _context;
    private readonly UsuarioActual _usuario;

    public SostenibilidadController(AppDbContext context, UsuarioActual usuario)
    {
        _context = context;
        _usuario = usuario;
    }

    // desde/hasta: fechas de Chile, ambas incluidas; por defecto, el mes en curso.
    // Cuenta las solicitudes Completadas según la fecha en que se completaron (historial).
    // ejemplo=true usa cantidades ficticias y exige el permiso de edición (solo para probar la pantalla).
    [HttpGet("resumen")]
    [RequierePermiso(PermisoVer)]
    public async Task<ActionResult<ResumenSostenibilidadDto>> Resumen(DateOnly? desde = null, DateOnly? hasta = null, bool ejemplo = false)
    {
        if (ejemplo && !await TienePermisoAsync(PermisoEditar))
            return StatusCode(403, new { error = "Solo Gestión General puede ver los datos de ejemplo." });

        var zona = HoraChile.Zona();
        var hoy = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, zona));
        var inicio = desde ?? new DateOnly(hoy.Year, hoy.Month, 1);
        var fin = hasta ?? new DateOnly(hoy.Year, hoy.Month, 1).AddMonths(1).AddDays(-1);
        if (fin < inicio)
            return BadRequest(new { errores = new Dictionary<string, string> { ["hasta"] = "La fecha final no puede ser anterior a la inicial." } });

        var parametros = await _context.ParametrosSostenibilidad.AsNoTracking().FirstOrDefaultAsync(p => p.Id == ParametrosSostenibilidad.IdUnico);
        if (parametros == null)
            return StatusCode(500, new { error = "Faltan los parámetros de Sostenibilidad. Aplica la migración AgregarSostenibilidad." });

        var alcance = "Empresa completa";
        IReadOnlyDictionary<string, int> cantidades;
        if (ejemplo)
        {
            cantidades = CalculadoraSostenibilidad.CantidadesEjemplo;
        }
        else
        {
            var desdeUtc = TimeZoneInfo.ConvertTimeToUtc(inicio.ToDateTime(TimeOnly.MinValue), zona);
            var hastaUtc = TimeZoneInfo.ConvertTimeToUtc(fin.AddDays(1).ToDateTime(TimeOnly.MinValue), zona);

            var solicitudes = _context.Solicitudes.Where(s => s.Estado == Completada);
            if (_usuario.AreaRestringida is int areaPropia)
            {
                solicitudes = solicitudes.Where(s => s.Trabajador!.AreaId == areaPropia || s.CreadoPorId == _usuario.Id);
                alcance = await _context.Catalogos.Where(c => c.Id == areaPropia).Select(c => c.Nombre).FirstOrDefaultAsync() ?? "Sin área asignada";
            }

            cantidades = await solicitudes
                .Select(s => new
                {
                    s.Tipo,
                    FechaCompletada = _context.HistorialSolicitudes
                        .Where(h => h.SolicitudId == s.Id &&
                                    (h.Accion == FlujoSolicitud.HistorialCompletada || h.Accion == FlujoSolicitud.HistorialFichaEnviada))
                        .Max(h => (DateTime?)h.Fecha)
                })
                .Where(x => x.FechaCompletada >= desdeUtc && x.FechaCompletada < hastaUtc)
                .GroupBy(x => x.Tipo)
                .Select(g => new { Tipo = g.Key, Cantidad = g.Count() })
                .ToDictionaryAsync(x => x.Tipo, x => x.Cantidad);
        }

        var p = AParametros(parametros);
        var r = CalculadoraSostenibilidad.Calcular(cantidades, p);

        return new ResumenSostenibilidadDto
        {
            Desde = inicio,
            Hasta = fin,
            EsEjemplo = ejemplo,
            Alcance = alcance,
            TotalSolicitudes = r.TotalSolicitudes,
            CantidadPorTipo = r.PorTipo.Select(t => new SostenibilidadTipoDto
            {
                Tipo = TipoAApi(t.Tipo),
                Cantidad = t.Cantidad,
                HojasPorFormulario = CalculadoraSostenibilidad.HojasPorFormulario(p, t.Tipo),
                HojasEvitadas = t.HojasEvitadas,
                Co2PapelEvitadoGramos = t.Co2PapelEvitadoGramos
            }).ToList(),
            HojasEvitadas = r.HojasEvitadas,
            Co2PapelEvitadoGramos = r.Co2PapelEvitadoGramos,
            Co2SistemaGramos = r.Co2SistemaGramos,
            AhorroNetoGramos = r.AhorroNetoGramos,
            Supuestos = new SupuestosSostenibilidadDto
            {
                Parametros = ADto(parametros),
                FechaActualizacion = parametros.FechaActualizacion,
                Formulas = CalculadoraSostenibilidad.Formulas.ToList(),
                Fuentes = CalculadoraSostenibilidad.Fuentes
                    .Select(f => new FuenteSostenibilidadDto { Parametro = f.Parametro, Descripcion = f.Descripcion }).ToList()
            }
        };
    }

    [HttpPut("parametros")]
    [RequierePermiso(PermisoEditar)]
    public async Task<ActionResult<ParametrosSostenibilidadDto>> ActualizarParametros(ParametrosSostenibilidadDto dto)
    {
        var p = new CalculadoraSostenibilidad.Parametros(
            dto.HojasIngreso, dto.HojasModificacion, dto.HojasBloqueo, dto.HojasVpn,
            dto.CopiasImpresas, dto.GramosCO2PorHoja, dto.KbTransferidosPorSolicitud, dto.GramosCO2PorMB);
        var errores = CalculadoraSostenibilidad.Validar(p);
        if (errores.Count > 0) return BadRequest(new { errores });

        var parametros = await _context.ParametrosSostenibilidad.FirstOrDefaultAsync(x => x.Id == ParametrosSostenibilidad.IdUnico);
        if (parametros == null)
        {
            parametros = new ParametrosSostenibilidad { Id = ParametrosSostenibilidad.IdUnico };
            _context.ParametrosSostenibilidad.Add(parametros);
        }

        parametros.HojasIngreso = dto.HojasIngreso;
        parametros.HojasModificacion = dto.HojasModificacion;
        parametros.HojasBloqueo = dto.HojasBloqueo;
        parametros.HojasVpn = dto.HojasVpn;
        parametros.CopiasImpresas = dto.CopiasImpresas;
        parametros.GramosCO2PorHoja = dto.GramosCO2PorHoja;
        parametros.KbTransferidosPorSolicitud = dto.KbTransferidosPorSolicitud;
        parametros.GramosCO2PorMB = dto.GramosCO2PorMB;
        parametros.FechaActualizacion = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return ADto(parametros);
    }

    private Task<bool> TienePermisoAsync(string codigo) =>
        _context.RolPermisos.AnyAsync(rp => rp.RolId == _usuario.RolId && rp.Permiso!.Codigo == codigo);

    private static CalculadoraSostenibilidad.Parametros AParametros(ParametrosSostenibilidad x) => new(
        x.HojasIngreso, x.HojasModificacion, x.HojasBloqueo, x.HojasVpn,
        x.CopiasImpresas, x.GramosCO2PorHoja, x.KbTransferidosPorSolicitud, x.GramosCO2PorMB);

    private static ParametrosSostenibilidadDto ADto(ParametrosSostenibilidad x) => new()
    {
        HojasIngreso = x.HojasIngreso,
        HojasModificacion = x.HojasModificacion,
        HojasBloqueo = x.HojasBloqueo,
        HojasVpn = x.HojasVpn,
        CopiasImpresas = x.CopiasImpresas,
        GramosCO2PorHoja = x.GramosCO2PorHoja,
        KbTransferidosPorSolicitud = x.KbTransferidosPorSolicitud,
        GramosCO2PorMB = x.GramosCO2PorMB
    };
}
