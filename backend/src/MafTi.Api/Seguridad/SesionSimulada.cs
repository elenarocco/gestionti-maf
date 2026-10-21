using MafTi.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace MafTi.Api.Seguridad;

// TEMPORAL: hasta tener login real, el rol y el usuario llegan en los headers
// X-Rol-Simulado y X-Usuario-Simulado (id de UsuarioSistema).
public static class SesionSimulada
{
    public static string Rol(HttpRequest request) => request.Headers["X-Rol-Simulado"].ToString();

    public static int? UsuarioId(HttpRequest request) =>
        int.TryParse(request.Headers["X-Usuario-Simulado"].ToString(), out var id) ? id : null;

    public static Task<bool> TienePermisoAsync(AppDbContext db, string rol, string codigo) =>
        db.RolPermisos.AnyAsync(rp => rp.Rol!.Nombre == rol && rp.Permiso!.Codigo == codigo);
}
