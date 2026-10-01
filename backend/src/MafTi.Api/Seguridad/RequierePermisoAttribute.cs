using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;
using MafTi.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace MafTi.Api.Seguridad;

public class RequierePermisoAttribute : Attribute, IAsyncActionFilter
{
    private readonly string _codigoPermiso;

    public RequierePermisoAttribute(string codigoPermiso)
    {
        _codigoPermiso = codigoPermiso;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var rolActual = context.HttpContext.Request.Headers["X-Rol-Simulado"].ToString();

        if (string.IsNullOrEmpty(rolActual))
        {
            context.Result = new UnauthorizedObjectResult("Falta el header X-Rol-Simulado (temporal, hasta tener login real).");
            return;
        }

        var db = context.HttpContext.RequestServices.GetRequiredService<AppDbContext>();

        bool tienePermiso = await db.RolPermisos
            .Include(rp => rp.Rol)
            .Include(rp => rp.Permiso)
            .AnyAsync(rp => rp.Rol!.Nombre == rolActual && rp.Permiso!.Codigo == _codigoPermiso);

        if (!tienePermiso)
        {
            context.Result = new ObjectResult(new { error = $"El rol '{rolActual}' no tiene el permiso '{_codigoPermiso}'." })
            {
                StatusCode = 403
            };
            return;
        }

        await next();
    }
}