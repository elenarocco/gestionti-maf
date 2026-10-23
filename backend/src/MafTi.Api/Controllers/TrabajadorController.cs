using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MafTi.Application;
using MafTi.Infrastructure;
using MafTi.Domain;
using MafTi.Api.Dtos;
using MafTi.Api.Common;
using MafTi.Api.Seguridad;
using MafTi.Api.Reportes;

namespace MafTi.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TrabajadorController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly UsuarioActual _usuario;

    public TrabajadorController(AppDbContext context, UsuarioActual usuario)
    {
        _context = context;
        _usuario = usuario;
    }

    private ObjectResult FueraDeSuArea() =>
        StatusCode(403, new { error = "Solo puedes consultar trabajadores de tu área." });

    // null si puede ver al trabajador (o si no existe: eso lo responde cada endpoint con 404).
    private async Task<ObjectResult?> VerificarAreaAsync(int trabajadorId)
    {
        if (_usuario.AreaRestringida is not int areaPropia) return null;

        var areaTrabajador = await _context.Trabajadores
            .Where(t => t.Id == trabajadorId)
            .Select(t => (int?)t.AreaId)
            .FirstOrDefaultAsync();

        return areaTrabajador == null || areaTrabajador == areaPropia ? null : FueraDeSuArea();
    }

    // Filtros de la lista de trabajadores; los usan la pantalla y el reporte, así descargan lo mismo que se ve.
    // null si la Jefatura pide un área que no es la suya.
    private IQueryable<Trabajador>? Filtrar(string? q, int? areaId, bool? activo)
    {
        var query = _context.Trabajadores.AsQueryable();

        // Jefatura ("Mi equipo") solo ve a los trabajadores de su área.
        if (_usuario.AreaRestringida is int areaPropia)
        {
            if (areaId.HasValue && areaId != areaPropia) return null;
            query = query.Where(t => t.AreaId == areaPropia);
        }

        if (!string.IsNullOrWhiteSpace(q))
        {
            var patron = $"%{q.Trim()}%";
            var rutPatron = $"%{q.Replace(".", "").Trim()}%";
            query = query.Where(t =>
                EF.Functions.ILike(t.PrimerNombre + " " + t.PrimerApellido, patron) ||
                EF.Functions.ILike(t.Rut, rutPatron) ||
                EF.Functions.ILike(t.Correo, patron));
        }
        if (areaId.HasValue) query = query.Where(t => t.AreaId == areaId.Value);
        if (activo.HasValue) query = query.Where(t => t.Activo == activo.Value);
        return query;
    }

    [HttpGet]
    [RequierePermiso("ConsultarTrabajadoresYAccesos")]
    public async Task<ActionResult<PaginacionResultado<TrabajadorListaDto>>> GetAll(
        int pagina = 1, int tamanoPagina = 20, string? q = null, int? areaId = null, bool? activo = null)
    {
        var query = Filtrar(q, areaId, activo);
        if (query == null) return FueraDeSuArea();

        var resultado = await query
            .OrderBy(t => t.PrimerApellido).ThenBy(t => t.PrimerNombre).ThenBy(t => t.Id)
            .Select(t => new TrabajadorListaDto
            {
                Id = t.Id,
                Rut = t.Rut,
                PrimerNombre = t.PrimerNombre,
                PrimerApellido = t.PrimerApellido,
                Correo = t.Correo,
                Cargo = t.Cargo!.Nombre,
                AreaNombre = t.Area!.Nombre,
                Activo = t.Activo
            })
            .PaginarAsync(pagina, tamanoPagina);

        return Ok(resultado);
    }

    // Reporte de trabajadores (Excel o PDF) con los mismos filtros de la lista. Quedan fuera a propósito
    // los datos personales que no hacen falta en un reporte: fecha de nacimiento, sexo y domicilio.
    [HttpGet("exportar")]
    [RequierePermiso("ConsultarTrabajadoresYAccesos")]
    [RequierePermiso(DescargaReporte.Permiso)]
    public async Task<IActionResult> Exportar(string formato, string? q = null, int? areaId = null, bool? activo = null)
    {
        if (DescargaReporte.LeerFormato(formato) is not FormatoReporte fmt) return DescargaReporte.FormatoInvalido(this);
        var query = Filtrar(q, areaId, activo);
        if (query == null) return FueraDeSuArea();

        var filas = await query
            .OrderBy(t => t.Area!.Nombre).ThenBy(t => t.PrimerApellido).ThenBy(t => t.PrimerNombre).ThenBy(t => t.Id)
            .Take(Reporte.MaxFilas + 1)
            .Select(t => new object?[]
            {
                t.Rut,
                t.PrimerNombre + (t.SegundoNombre != null ? " " + t.SegundoNombre : ""),
                t.PrimerApellido + (t.SegundoApellido != null ? " " + t.SegundoApellido : ""),
                t.Correo, t.Area!.Nombre, t.Cargo!.Nombre, t.LugarTrabajo!.Nombre, t.JefeDirecto,
                t.FechaIncorporacion, t.FechaSalida, t.Activo ? "Activo" : "Inactivo"
            })
            .ToListAsync();
        if (filas.Count > Reporte.MaxFilas) return DescargaReporte.DemasiadasFilas(this);

        var filtros = new List<string>();
        var areaFiltro = areaId ?? _usuario.AreaRestringida;
        if (areaFiltro.HasValue)
            filtros.Add("Área: " + (await _context.Catalogos.Where(c => c.Id == areaFiltro).Select(c => c.Nombre).FirstOrDefaultAsync() ?? "—"));
        if (activo.HasValue) filtros.Add(activo.Value ? "Solo activos" : "Solo inactivos");
        if (!string.IsNullOrWhiteSpace(q)) filtros.Add($"Búsqueda: \"{q.Trim()}\"");

        var reporte = new Reporte
        {
            Titulo = "Trabajadores",
            NombreArchivo = "trabajadores",
            Columnas =
            [
                new("RUT", 1.1), new("Nombres", 1.4), new("Apellidos", 1.4), new("Correo", 2.2), new("Área", 1.4),
                new("Cargo", 1.6), new("Lugar de trabajo", 1.2), new("Jefe directo", 1.1),
                new("Incorporación", 1.25), new("Salida", 1.05), new("Estado", 0.8)
            ],
            Filas = filas,
            Filtros = filtros.Count == 0 ? "ninguno" : string.Join(" · ", filtros),
            GeneradoPor = _usuario.Nombre
        };
        return await DescargaReporte.EntregarAsync(this, _context, _usuario, reporte, fmt);
    }

    [HttpGet("{id}/ficha")]
    [RequierePermiso("ConsultarTrabajadoresYAccesos")]
    public async Task<ActionResult<TrabajadorFichaDto>> GetFicha(int id)
    {
        if (await VerificarAreaAsync(id) is { } denegado) return denegado;

        var t = await _context.Trabajadores
            .Include(x => x.Area).Include(x => x.Cargo)
            .Include(x => x.DireccionCorporativa).Include(x => x.LugarTrabajo)
            .FirstOrDefaultAsync(x => x.Id == id);
        if (t == null) return NotFound();

        var accesos = await _context.Accesos
            .Where(a => a.TrabajadorId == id && a.Estado == "Activo")
            .Select(a => new { a.Catalogo!.Tipo, a.Catalogo.Nombre })
            .ToListAsync();

        var solicitudes = await _context.Solicitudes
            .Where(s => s.TrabajadorId == id)
            .OrderByDescending(s => s.FechaCreacion)
            .Select(s => new FichaSolicitudDto
            {
                Id = s.Id,
                Tipo = s.Tipo,
                Estado = s.Estado,
                FechaCreacion = s.FechaCreacion,
                CreadoPor = s.CreadoPor!.Trabajador!.PrimerNombre + " " + s.CreadoPor.Trabajador.PrimerApellido
            })
            .ToListAsync();

        var nombre = string.Join(" ", new[] { t.PrimerNombre, t.SegundoNombre, t.PrimerApellido, t.SegundoApellido }
            .Where(p => !string.IsNullOrWhiteSpace(p)));

        return new TrabajadorFichaDto
        {
            Id = t.Id,
            Rut = t.Rut,
            NombreCompleto = nombre,
            Correo = t.Correo,
            Cargo = t.Cargo!.Nombre,
            Area = t.Area!.Nombre,
            DireccionCorporativa = t.DireccionCorporativa!.Nombre,
            LugarTrabajo = t.LugarTrabajo!.Nombre,
            FechaIncorporacion = t.FechaIncorporacion,
            FechaSalida = t.FechaSalida,
            Activo = t.Activo,
            Sistemas = accesos.Where(a => a.Tipo == "Sistema").Select(a => a.Nombre).ToList(),
            Carpetas = accesos.Where(a => a.Tipo == "Carpeta").Select(a => a.Nombre).ToList(),
            Solicitudes = solicitudes
        };
    }

    [HttpGet("{id}/accesos")]
    [RequierePermiso("ConsultarTrabajadoresYAccesos")]
    public async Task<ActionResult<List<TrabajadorAccesoDto>>> GetAccesos(int id)
    {
        if (!await _context.Trabajadores.AnyAsync(t => t.Id == id)) return NotFound();
        if (await VerificarAreaAsync(id) is { } denegado) return denegado;

        return await _context.Accesos
            .Where(a => a.TrabajadorId == id && a.Estado == "Activo")
            .OrderBy(a => a.Catalogo!.Tipo).ThenBy(a => a.Catalogo!.Nombre)
            .Select(a => new TrabajadorAccesoDto
            {
                CatalogoId = a.CatalogoId,
                Nombre = a.Catalogo!.Nombre,
                Tipo = a.Catalogo.Tipo
            })
            .ToListAsync();
    }

    [HttpGet("{id}")]
    [RequierePermiso("ConsultarTrabajadoresYAccesos")]
    public async Task<ActionResult<TrabajadorDetalleDto>> GetById(int id)
    {
        if (await VerificarAreaAsync(id) is { } denegado) return denegado;

        var t = await _context.Trabajadores.FindAsync(id);
        if (t == null) return NotFound();

        return new TrabajadorDetalleDto
        {
            Id = t.Id,
            Rut = t.Rut,
            PrimerNombre = t.PrimerNombre,
            SegundoNombre = t.SegundoNombre,
            PrimerApellido = t.PrimerApellido,
            SegundoApellido = t.SegundoApellido,
            FechaNacimiento = t.FechaNacimiento,
            Sexo = t.Sexo,
            Correo = t.Correo,
            DireccionCorporativaId = t.DireccionCorporativaId,
            AreaId = t.AreaId,
            CargoId = t.CargoId,
            LugarTrabajoId = t.LugarTrabajoId,
            EsCuentaGenerica = false,
            FechaIncorporacion = t.FechaIncorporacion,
            DireccionDomicilio = t.DireccionDomicilio,
            JefeDirecto = t.JefeDirecto,
            HomologarAccesosDesde = t.HomologarAccesosDesde,
            TieneTelefonoCorporativo = t.TieneTelefonoCorporativo,
            SolicitaTelefono = t.SolicitaTelefono,
            Activo = t.Activo
        };
    }

    [HttpGet("sugerir-correo")]
    [RequierePermiso("CrearSolicitudIngreso", "CrearSolicitudModificacion")]
    public async Task<ActionResult<SugerenciaCorreo>> SugerirCorreo(string rut, string primerNombre, string primerApellido, string? segundoApellido = null)
    {
        var rutLimpio = TrabajadorValidator.LimpiarRut(rut);

        var resultado = await GeneradorCorreoService.Generar(
            rutLimpio, primerNombre, primerApellido, segundoApellido,
            async r =>
            {
                var inactivo = await _context.Trabajadores
                    .FirstOrDefaultAsync(t => t.Rut == TrabajadorValidator.FormatearParaGuardar(r) && !t.Activo);
                return inactivo?.Correo;
            },
            async correo => await _context.Trabajadores.AnyAsync(t => t.Correo.ToLower() == correo));

        return Ok(resultado);
    }
    [HttpGet("verificar-rut")]
    [RequierePermiso("CrearSolicitudIngreso")]
    public async Task<ActionResult<object>> VerificarRut(string rut)
    {
        var error = TrabajadorValidator.ValidarFormatoRut(rut);
        if (error != null) return Ok(new { valido = false, mensaje = error });

        var formateado = TrabajadorValidator.FormatearParaGuardar(rut);
        var existe = await _context.Trabajadores.AnyAsync(t => t.Rut == formateado && t.Activo);

        return Ok(new
        {
            valido = !existe,
            mensaje = existe ? "Ya existe un trabajador activo con ese RUT." : null
        });
    }
}