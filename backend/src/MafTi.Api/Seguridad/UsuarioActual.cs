using System.Security.Claims;

namespace MafTi.Api.Seguridad;

// Usuario de la petición, resuelto por UsuarioActualMiddleware a partir del correo del token
// (Entra ID o, solo en Development, el token de desarrollo). Se inyecta en controllers y filtros.
public class UsuarioActual
{
    public bool Autenticado { get; private set; }
    public int Id { get; private set; }            // UsuarioSistema.Id
    public string Correo { get; private set; } = string.Empty;
    public string Nombre { get; private set; } = string.Empty;
    public int RolId { get; private set; }
    public string Rol { get; private set; } = string.Empty;
    public int? AreaId { get; private set; }

    public const string RolJefatura = "Jefatura";
    private const int SinArea = 0;   // ningún área tiene Id 0: una Jefatura sin área asignada no ve datos de nadie

    // Área a la que está limitado el usuario: solo Jefatura ve únicamente su área (trabajadores,
    // solicitudes y dashboard). null = sin restricción (TI, RRHH, Auditoría, Super Admin).
    public int? AreaRestringida => Rol == RolJefatura ? AreaId ?? SinArea : null;

    public void Establecer(int id, string correo, string nombre, int rolId, string rol, int? areaId)
    {
        Autenticado = true;
        Id = id;
        Correo = correo;
        Nombre = nombre;
        RolId = rolId;
        Rol = rol;
        AreaId = areaId;
    }
}

// Nombres de claims. Entra trae el correo; userId, rol y areaId se agregan desde la BD
// (el token de desarrollo los trae también, con los mismos nombres).
public static class ClaimsSesion
{
    public const string Correo = "correo";
    public const string UserId = "userId";
    public const string Rol = "rol";
    public const string AreaId = "areaId";

    // Entra ID v2 entrega el correo en preferred_username (UPN) y a veces en email.
    private static readonly string[] ClaimsDeCorreo =
        { Correo, "preferred_username", "email", ClaimTypes.Email, "upn", ClaimTypes.Upn };

    public static string? CorreoDe(ClaimsPrincipal usuario) =>
        ClaimsDeCorreo.Select(t => usuario.FindFirst(t)?.Value).FirstOrDefault(v => !string.IsNullOrWhiteSpace(v));
}
