using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MafTi.Application;
using MafTi.Infrastructure;
using MafTi.Domain;
using MafTi.Api.Dtos;
using MafTi.Api.Common;

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
    public async Task<ActionResult<PaginacionResultado<TrabajadorListaDto>>> GetAll(int pagina = 1, int tamanoPagina = 20)
    {
        var query = _context.Trabajadores
            .OrderBy(t => t.Id)
            .Select(t => new TrabajadorListaDto
            {
                Id = t.Id,
                Rut = t.Rut,
                PrimerNombre = t.PrimerNombre,
                PrimerApellido = t.PrimerApellido,
                Correo = t.Correo,
                Cargo = t.Cargo,
                Activo = t.Activo
            });

        var resultado = await query.PaginarAsync(pagina, tamanoPagina);
        return Ok(resultado);
    }

    [HttpGet("{id}")]
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
            Cargo = t.Cargo,
            LugarTrabajoId = t.LugarTrabajoId,
            EsCuentaGenerica = t.EsCuentaGenerica,
            FechaIncorporacion = t.FechaIncorporacion,
            DireccionDomicilio = t.DireccionDomicilio,
            JefeDirecto = t.JefeDirecto,
            HomologarAccesosDesde = t.HomologarAccesosDesde,
            TieneTelefonoCorporativo = t.TieneTelefonoCorporativo,
            SolicitaTelefono = t.SolicitaTelefono,
            Activo = t.Activo
        };
    }

 [HttpPost]
public async Task<ActionResult<TrabajadorDetalleDto>> Create(TrabajadorCreateDto dto)
{
    var nuevo = new Trabajador
    {
            Rut = dto.Rut,
            PrimerNombre = dto.PrimerNombre,
            SegundoNombre = dto.SegundoNombre,
            PrimerApellido = dto.PrimerApellido,
            SegundoApellido = dto.SegundoApellido,
            FechaNacimiento = dto.FechaNacimiento,
            Sexo = dto.Sexo,
            Correo = dto.Correo,
            DireccionCorporativaId = dto.DireccionCorporativaId,
            AreaId = dto.AreaId,
            Cargo = dto.Cargo,
            LugarTrabajoId = dto.LugarTrabajoId,
            EsCuentaGenerica = dto.EsCuentaGenerica,
            FechaIncorporacion = dto.FechaIncorporacion,
            DireccionDomicilio = dto.DireccionDomicilio,
            JefeDirecto = dto.JefeDirecto,
            HomologarAccesosDesde = dto.HomologarAccesosDesde,
            TieneTelefonoCorporativo = dto.TieneTelefonoCorporativo,
            SolicitaTelefono = dto.SolicitaTelefono,
            Activo = true
        };

        var errores = TrabajadorValidator.Validar(nuevo);
        if (errores.Any())
        {
            return BadRequest(new { errores });
        }

        _context.Trabajadores.Add(nuevo);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = nuevo.Id }, dto);
    }
}