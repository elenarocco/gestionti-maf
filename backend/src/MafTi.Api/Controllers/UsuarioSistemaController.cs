using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MafTi.Application;
using MafTi.Infrastructure;
using MafTi.Domain;
using MafTi.Api.Dtos;
using MafTi.Api.Seguridad;
using MafTi.Api.Common;

namespace MafTi.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
// Quien puede dar acceso a usuarios puede darse cualquier rol: solo Gestión General (AdministrarUsuariosYRoles).
// Las reglas (un acceso por persona, no bloquearse a sí mismo, nunca quedar sin administrador, área de
// Jefatura) están en ReglasUsuarios; cada cambio queda en la bitácora CambiosUsuarioSistema.
[RequierePermiso(PermisoAdministrar)]
public class UsuarioSistemaController : ControllerBase
{
    private const string PermisoAdministrar = "AdministrarUsuariosYRoles";

    private readonly AppDbContext _context;
    private readonly UsuarioActual _usuario;

    public UsuarioSistemaController(AppDbContext context, UsuarioActual usuario)
    {
        _context = context;
        _usuario = usuario;
    }

    [HttpGet]
    public async Task<ActionResult<PaginacionResultado<UsuarioSistemaListaDto>>> GetAll(
        int pagina = 1, int tamanoPagina = 20, string? q = null, int? rolId = null, bool? activo = null)
    {
        var query = _context.UsuariosSistema.AsQueryable();
        if (!string.IsNullOrWhiteSpace(q))
        {
            var patron = $"%{q.Trim()}%";
            query = query.Where(u =>
                EF.Functions.ILike(u.Trabajador!.PrimerNombre + " " + u.Trabajador.PrimerApellido, patron) ||
                EF.Functions.ILike(u.Trabajador.Correo, patron) ||
                EF.Functions.ILike(u.Trabajador.Rut, $"%{q.Replace(".", "").Trim()}%"));
        }
        if (rolId.HasValue) query = query.Where(u => u.RolId == rolId);
        if (activo.HasValue) query = query.Where(u => u.Activo == activo);

        var resultado = await query
            .OrderByDescending(u => u.Activo).ThenBy(u => u.Trabajador!.PrimerApellido).ThenBy(u => u.Id)
            .Select(u => new UsuarioSistemaListaDto
            {
                Id = u.Id,
                TrabajadorId = u.TrabajadorId,
                TrabajadorNombre = u.Trabajador!.PrimerNombre + " " + u.Trabajador.PrimerApellido,
                TrabajadorCorreo = u.Trabajador.Correo,
                TrabajadorRut = u.Trabajador.Rut,
                TrabajadorActivo = u.Trabajador.Activo,
                RolId = u.RolId,
                RolNombre = u.Rol!.Nombre,
                AreaId = u.AreaId,
                AreaNombre = u.Area != null ? u.Area.Nombre : null,
                Activo = u.Activo,
                EsUstedMismo = u.Id == _usuario.Id
            })
            .PaginarAsync(pagina, tamanoPagina);

        return Ok(resultado);
    }

    // Trabajadores activos sin acceso, para elegir a quién dar acceso.
    [HttpGet("candidatos")]
    public async Task<ActionResult<List<CandidatoAccesoDto>>> Candidatos(string? q = null)
    {
        var query = _context.Trabajadores.Where(t => t.Activo && !_context.UsuariosSistema.Any(u => u.TrabajadorId == t.Id));
        if (!string.IsNullOrWhiteSpace(q))
        {
            var patron = $"%{q.Trim()}%";
            query = query.Where(t =>
                EF.Functions.ILike(t.PrimerNombre + " " + t.PrimerApellido, patron) ||
                EF.Functions.ILike(t.Correo, patron) ||
                EF.Functions.ILike(t.Rut, $"%{q.Replace(".", "").Trim()}%"));
        }

        return await query
            .OrderBy(t => t.PrimerApellido).ThenBy(t => t.PrimerNombre)
            .Take(20)
            .Select(t => new CandidatoAccesoDto
            {
                TrabajadorId = t.Id,
                Nombre = t.PrimerNombre + " " + t.PrimerApellido,
                Rut = t.Rut,
                Correo = t.Correo,
                AreaId = t.AreaId,
                AreaNombre = t.Area!.Nombre
            })
            .ToListAsync();
    }

    [HttpPost]
    public async Task<IActionResult> Create(UsuarioSistemaCreateDto dto)
    {
        var trabajador = await _context.Trabajadores.FindAsync(dto.TrabajadorId);
        if (trabajador == null) return NotFound(new { error = "El trabajador no existe." });
        var rol = await _context.Roles.FindAsync(dto.RolId);
        if (rol == null) return BadRequest(new { error = "El rol no existe." });
        if (dto.AreaId.HasValue && !await EsAreaAsync(dto.AreaId.Value)) return BadRequest(new { error = "El área no existe." });

        var yaTieneAcceso = await _context.UsuariosSistema.AnyAsync(u => u.TrabajadorId == dto.TrabajadorId);
        var error = ReglasUsuarios.ErrorAlDarAcceso(trabajador.Activo, yaTieneAcceso, rol.Nombre, dto.AreaId);
        if (error != null) return BadRequest(new { error });

        var nuevo = new UsuarioSistema { TrabajadorId = dto.TrabajadorId, RolId = dto.RolId, AreaId = dto.AreaId, Activo = true };
        _context.UsuariosSistema.Add(nuevo);
        Registrar(nuevo, "Acceso otorgado", $"Rol: {rol.Nombre} · Área: {await NombreAreaAsync(dto.AreaId)}");
        await _context.SaveChangesAsync();

        return Ok(new { id = nuevo.Id });
    }

    // Cambiar rol y/o área.
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Cambiar(int id, UsuarioSistemaCambioDto dto)
    {
        var usuario = await _context.UsuariosSistema.Include(u => u.Rol).FirstOrDefaultAsync(u => u.Id == id);
        if (usuario == null) return NotFound(new { error = "El usuario no existe." });
        var rolNuevo = await _context.Roles.FindAsync(dto.RolId);
        if (rolNuevo == null) return BadRequest(new { error = "El rol no existe." });
        if (dto.AreaId.HasValue && !await EsAreaAsync(dto.AreaId.Value)) return BadRequest(new { error = "El área no existe." });
        if (usuario.RolId == dto.RolId && usuario.AreaId == dto.AreaId) return NoContent();

        var quitaAdministracion = usuario.Activo
            && await RolAdministraAsync(usuario.RolId) && !await RolAdministraAsync(dto.RolId);
        var error = ReglasUsuarios.ErrorAlCambiar(usuario.Id == _usuario.Id, quitaAdministracion,
            await AdministradoresActivosAsync(), rolNuevo.Nombre, dto.AreaId);
        if (error != null) return BadRequest(new { error });

        var cambios = new List<string>();
        if (usuario.RolId != dto.RolId) cambios.Add($"Rol: {usuario.Rol!.Nombre} → {rolNuevo.Nombre}");
        if (usuario.AreaId != dto.AreaId)
            cambios.Add($"Área: {await NombreAreaAsync(usuario.AreaId)} → {await NombreAreaAsync(dto.AreaId)}");

        usuario.RolId = dto.RolId;
        usuario.AreaId = dto.AreaId;
        Registrar(usuario, "Acceso modificado", string.Join(" · ", cambios));
        await _context.SaveChangesAsync();
        return NoContent();
    }

    // Quitar el acceso. No se borra: el historial de solicitudes y descargas queda ligado a la persona.
    [HttpPut("{id:int}/desactivar")]
    public async Task<IActionResult> Desactivar(int id)
    {
        var usuario = await _context.UsuariosSistema.FindAsync(id);
        if (usuario == null) return NotFound(new { error = "El usuario no existe." });
        if (!usuario.Activo) return NoContent();

        var error = ReglasUsuarios.ErrorAlDesactivar(usuario.Id == _usuario.Id,
            await RolAdministraAsync(usuario.RolId), await AdministradoresActivosAsync());
        if (error != null) return BadRequest(new { error });

        usuario.Activo = false;
        Registrar(usuario, "Acceso desactivado", "");
        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpPut("{id:int}/reactivar")]
    public async Task<IActionResult> Reactivar(int id)
    {
        var usuario = await _context.UsuariosSistema.Include(u => u.Trabajador).FirstOrDefaultAsync(u => u.Id == id);
        if (usuario == null) return NotFound(new { error = "El usuario no existe." });
        if (usuario.Activo) return NoContent();

        var error = ReglasUsuarios.ErrorAlReactivar(usuario.Trabajador!.Activo);
        if (error != null) return BadRequest(new { error });

        usuario.Activo = true;
        Registrar(usuario, "Acceso reactivado", "");
        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpGet("{id:int}/historial")]
    public async Task<ActionResult<List<CambioUsuarioDto>>> Historial(int id) =>
        await _context.CambiosUsuarioSistema
            .Where(c => c.UsuarioSistemaId == id)
            .OrderByDescending(c => c.Fecha)
            .Select(c => new CambioUsuarioDto
            {
                Fecha = c.Fecha,
                Accion = c.Accion,
                Detalle = c.Detalle,
                RealizadoPorNombre = c.RealizadoPor!.Trabajador!.PrimerNombre + " " + c.RealizadoPor.Trabajador.PrimerApellido
            })
            .ToListAsync();

    private Task<bool> RolAdministraAsync(int rolId) =>
        _context.RolPermisos.AnyAsync(rp => rp.RolId == rolId && rp.Permiso!.Codigo == PermisoAdministrar);

    // Usuarios activos (con trabajador activo) cuyo rol puede administrar usuarios.
    private Task<int> AdministradoresActivosAsync() =>
        _context.UsuariosSistema.CountAsync(u => u.Activo && u.Trabajador!.Activo &&
            _context.RolPermisos.Any(rp => rp.RolId == u.RolId && rp.Permiso!.Codigo == PermisoAdministrar));

    private Task<bool> EsAreaAsync(int id) => _context.Catalogos.AnyAsync(c => c.Id == id && c.Tipo == "Area");

    private async Task<string> NombreAreaAsync(int? id) =>
        id == null ? "—" : await _context.Catalogos.Where(c => c.Id == id).Select(c => c.Nombre).FirstOrDefaultAsync() ?? "—";

    // Se agrega a la misma operación que el cambio: se guardan juntos o no se guarda ninguno.
    private void Registrar(UsuarioSistema usuario, string accion, string detalle) =>
        _context.CambiosUsuarioSistema.Add(new CambioUsuarioSistema
        {
            UsuarioSistema = usuario, RealizadoPorId = _usuario.Id, Accion = accion, Detalle = detalle
        });
}
