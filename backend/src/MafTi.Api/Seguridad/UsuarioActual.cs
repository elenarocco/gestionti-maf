using MafTi.Domain;
using MafTi.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace MafTi.Api.Seguridad;

// TEMPORAL hasta el login con AD: el usuario actual es el primer usuario activo
// del rol que llega en la cabecera X-Rol-Simulado.
public static class UsuarioActual
{
    public static string Rol(HttpRequest request) => request.Headers["X-Rol-Simulado"].ToString();

    public static Task<UsuarioSistema?> ObtenerAsync(AppDbContext db, HttpRequest request)
    {
        var rol = Rol(request);
        return db.UsuariosSistema
            .Where(u => u.Activo && u.Rol!.Nombre == rol)
            .OrderBy(u => u.Id)
            .FirstOrDefaultAsync();
    }
}
