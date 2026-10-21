using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;
using MafTi.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace MafTi.Api.Seguridad;

// Exige que el rol del usuario de la sesión tenga el permiso indicado (tabla RolPermisos).
// La sesión la resuelve UsuarioActualMiddleware desde el token.
public class RequierePermisoAttribute : Attribute, IAsyncActionFilter
{
    private readonly string _codigoPermiso;

    public RequierePermisoAttribute(string codigoPermiso)
    {
        _codigoPermiso = codigoPermiso;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var usuario = context.HttpContext.RequestServices.GetRequiredService<UsuarioActual>();

        if (!usuario.Autenticado)
        {
            context.Result = new UnauthorizedObjectResult(new { error = "Debes iniciar sesión." });
            return;
        }

        var db = context.HttpContext.RequestServices.GetRequiredService<AppDbContext>();

        bool tienePermiso = await db.RolPermisos
            .AnyAsync(rp => rp.RolId == usuario.RolId && rp.Permiso!.Codigo == _codigoPermiso);

        if (!tienePermiso)
        {
            context.Result = new ObjectResult(new { error = $"Tu rol '{usuario.Rol}' no tiene el permiso '{_codigoPermiso}'." })
            {
                StatusCode = 403
            };
            return;
        }

        await next();
    }
}
