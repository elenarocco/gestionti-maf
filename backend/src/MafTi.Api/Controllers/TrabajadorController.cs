using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MafTi.Application;
using MafTi.Infrastructure;
using MafTi.Domain;
using MafTi.Api.Dtos;
using MafTi.Api.Common;
using MafTi.Api.Seguridad;

namespace MafTi.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TrabajadorController : ControllerBase
{
    private readonly AppDbContext _context;

    public TrabajadorController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    [RequierePermiso("ConsultarTrabajadoresYAccesos")]
    public async Task<ActionResult<PaginacionResultado<TrabajadorListaDto>>> GetAll(
        int pagina = 1, int tamanoPagina = 20, string? q = null, int? areaId = null, bool? activo = null)
    {
        var query = _context.Trabajadores.AsQueryable();

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

    [HttpGet("{id}/ficha")]
    [RequierePermiso("ConsultarTrabajadoresYAccesos")]
    public async Task<ActionResult<TrabajadorFichaDto>> GetFicha(int id)
    {
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

    [HttpGet("{id}")]
    [RequierePermiso("ConsultarTrabajadoresYAccesos")]
    public async Task<ActionResult<TrabajadorDetalleDto>> GetById(int id)
    {
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

    [HttpPost]
    public async Task<ActionResult<TrabajadorDetalleDto>> Create(TrabajadorCreateDto dto)
    {
        var nuevo = new Trabajador
        {
            Rut = TrabajadorValidator.FormatearParaGuardar(dto.Rut),
            PrimerNombre = dto.PrimerNombre,
            SegundoNombre = dto.SegundoNombre,
            PrimerApellido = dto.PrimerApellido,
            SegundoApellido = dto.SegundoApellido,
            FechaNacimiento = dto.FechaNacimiento,
            Sexo = dto.Sexo,
            Correo = dto.Correo,
            DireccionCorporativaId = dto.DireccionCorporativaId,
            AreaId = dto.AreaId,
            CargoId = dto.CargoId,
            LugarTrabajoId = dto.LugarTrabajoId,
            EsCuentaGenerica = false,
            FechaIncorporacion = dto.FechaIncorporacion,
            DireccionDomicilio = dto.DireccionDomicilio,
            JefeDirecto = dto.JefeDirecto,
            HomologarAccesosDesde = dto.HomologarAccesosDesde,
            TieneTelefonoCorporativo = dto.TieneTelefonoCorporativo,
            SolicitaTelefono = dto.SolicitaTelefono,
            Activo = true
        };
        

        var errores = await TrabajadorValidator.ValidarAsync(
            nuevo,
            async correo => await _context.Trabajadores.AnyAsync(t => t.Correo.ToLower() == correo),
            async rut => await _context.Trabajadores.AnyAsync(t => t.Rut == rut && t.Activo));

        if (errores.Any())
        {
            return BadRequest(new { errores });
        }

        _context.Trabajadores.Add(nuevo);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = nuevo.Id }, dto);
    }
}