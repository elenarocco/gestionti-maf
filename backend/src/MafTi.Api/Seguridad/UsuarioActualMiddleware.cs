using System.Security.Claims;
using MafTi.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace MafTi.Api.Seguridad;

// Toda ruta /api exige sesión: un token válido cuyo correo corresponda a un trabajador activo
// con un UsuarioSistema activo. De ahí salen userId, rol y areaId.
// Excepción: /api/auth/dev/*, que solo existe en Development (login de desarrollo).
public class UsuarioActualMiddleware
{
    private readonly RequestDelegate _next;

    public UsuarioActualMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, AppDbContext db, UsuarioActual usuarioActual)
    {
        var ruta = context.Request.Path;
        if (!ruta.StartsWithSegments("/api") || ruta.StartsWithSegments("/api/auth/dev"))
        {
            await _next(context);
            return;
        }

        if (context.User.Identity?.IsAuthenticated != true)
        {
            await Responder(context, StatusCodes.Status401Unauthorized, "Debes iniciar sesión.");
            return;
        }

        var correo = ClaimsSesion.CorreoDe(context.User);
        if (correo == null)
        {
            await Responder(context, StatusCodes.Status403Forbidden, "El token no trae un correo para identificar al usuario.");
            return;
        }

        var correoNormalizado = correo.Trim().ToLower();
        var usuario = await db.UsuariosSistema
            .Include(u => u.Trabajador)
            .Include(u => u.Rol)
            .Where(u => u.Trabajador!.Correo.ToLower() == correoNormalizado)
            .OrderByDescending(u => u.Activo).ThenBy(u => u.Id)
            .FirstOrDefaultAsync();

        if (usuario == null)
        {
            await Responder(context, StatusCodes.Status403Forbidden,
                $"La cuenta {correo} no está registrada como usuario del sistema. Pide acceso a TI.");
            return;
        }
        if (!usuario.Activo || usuario.Trabajador?.Activo != true)
        {
            await Responder(context, StatusCodes.Status403Forbidden,
                $"El usuario {correo} está inactivo en el sistema. Pide a TI que lo reactive.");
            return;
        }

        var nombre = $"{usuario.Trabajador.PrimerNombre} {usuario.Trabajador.PrimerApellido}";
        usuarioActual.Establecer(usuario.Id, usuario.Trabajador.Correo, nombre, usuario.RolId, usuario.Rol!.Nombre, usuario.AreaId);

        // Mismos claims sin importar el origen del token (Entra o desarrollo).
        var claims = new List<Claim>
        {
            new(ClaimsSesion.Correo, usuarioActual.Correo),
            new(ClaimsSesion.UserId, usuarioActual.Id.ToString()),
            new(ClaimsSesion.Rol, usuarioActual.Rol)
        };
        if (usuarioActual.AreaId.HasValue) claims.Add(new(ClaimsSesion.AreaId, usuarioActual.AreaId.Value.ToString()));
        context.User.AddIdentity(new ClaimsIdentity(claims, "UsuarioSistema"));

        await _next(context);
    }

    private static Task Responder(HttpContext context, int status, string error)
    {
        context.Response.StatusCode = status;
        return context.Response.WriteAsJsonAsync(new { error });
    }
}
