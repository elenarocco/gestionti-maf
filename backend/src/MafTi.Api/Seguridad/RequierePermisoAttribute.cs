using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;
using MafTi.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace MafTi.Api.Seguridad;

// Exige que el rol del usuario de la sesión tenga el permiso indicado (tabla RolPermisos).
// La sesión la resuelve UsuarioActualMiddleware desde el token.
// Se puede repetir: con dos atributos el rol debe tener ambos permisos.
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public class RequierePermisoAttribute : Attribute, IAsyncActionFilter
{
    private readonly string[] _codigos;

    // Con varios códigos basta con que el rol tenga alguno.
    public RequierePermisoAttribute(params string[] codigos)
    {
        _codigos = codigos;
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
            .AnyAsync(rp => rp.RolId == usuario.RolId && _codigos.Contains(rp.Permiso!.Codigo));

        if (!tienePermiso)
        {
            context.Result = new ObjectResult(new { error = $"Tu rol '{usuario.Rol}' no tiene el permiso '{string.Join("' o '", _codigos)}'." })
            {
                StatusCode = 403
            };
            return;
        }

        await next();
    }
}
