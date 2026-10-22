using System.Text;
using MafTi.Application;
using MafTi.Domain;
using MafTi.Infrastructure;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace MafTi.Api.Seguridad;

// Login de desarrollo: SOLO se registra cuando el entorno es Development (ver Program.cs).
// Emite un JWT firmado con una clave local (AuthDesarrollo:Clave en user-secrets)
// con los mismos claims que se usan con Entra ID: correo, rol, userId y areaId.
public static class AuthDesarrollo
{
    public const string Esquema = "DesarrolloJwt";
    private const string Emisor = "maf-ti-desarrollo";
    private const string Audiencia = "maf-ti-desarrollo";
    private static readonly TimeSpan Duracion = TimeSpan.FromHours(8);

    private static SymmetricSecurityKey Clave(IConfiguration config)
    {
        var clave = config["AuthDesarrollo:Clave"];
        if (string.IsNullOrWhiteSpace(clave) || clave.Length < 32)
            throw new InvalidOperationException(
                "Falta AuthDesarrollo:Clave (mínimo 32 caracteres). Configúrala con: dotnet user-secrets set \"AuthDesarrollo:Clave\" \"...\". Ver backend/CONFIGURACION.md.");
        return new SymmetricSecurityKey(Encoding.UTF8.GetBytes(clave));
    }

    public static AuthenticationBuilder AgregarEsquema(this AuthenticationBuilder auth, IConfiguration config) =>
        auth.AddJwtBearer(Esquema, o =>
        {
            o.MapInboundClaims = false;
            o.TokenValidationParameters = new TokenValidationParameters
            {
                ValidIssuer = Emisor,
                ValidAudience = Audiencia,
                IssuerSigningKey = Clave(config),
                ClockSkew = TimeSpan.FromMinutes(1)
            };
        });

    // Decide el esquema mirando el emisor del token sin validarlo (la validación la hace el esquema elegido).
    public static bool EsTokenDeDesarrollo(HttpContext context)
    {
        var header = context.Request.Headers.Authorization.ToString();
        if (!header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)) return false;
        try
        {
            return new JsonWebToken(header["Bearer ".Length..].Trim()).Issuer == Emisor;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    public static void MapearEndpoints(WebApplication app)
    {
        var grupo = app.MapGroup("/api/auth/dev");

        // Un usuario activo por rol para el selector de la pantalla de login.
        grupo.MapGet("/usuarios", async (AppDbContext db) =>
        {
            var usuarios = await db.UsuariosSistema
                .Where(u => u.Activo && u.Trabajador!.Activo)
                .OrderBy(u => u.RolId).ThenBy(u => u.Id)
                .Select(u => new
                {
                    usuarioId = u.Id,
                    nombre = u.Trabajador!.PrimerNombre + " " + u.Trabajador.PrimerApellido,
                    correo = u.Trabajador.Correo,
                    rol = u.Rol!.Nombre,
                    area = u.Area != null ? u.Area.Nombre : null
                })
                .ToListAsync();
            return Results.Ok(usuarios);
        });

        grupo.MapPost("/token", async (TokenDesarrolloDto dto, AppDbContext db, IConfiguration config) =>
        {
            var u = await db.UsuariosSistema
                .Include(x => x.Trabajador).Include(x => x.Rol)
                .FirstOrDefaultAsync(x => x.Id == dto.UsuarioId && x.Activo && x.Trabajador!.Activo);
            if (u == null) return Results.BadRequest(new { error = "El usuario no existe o está inactivo." });

            var ahora = DateTime.UtcNow;
            var claims = new Dictionary<string, object>
            {
                [ClaimsSesion.Correo] = u.Trabajador!.Correo,
                ["preferred_username"] = u.Trabajador.Correo,
                ["name"] = $"{u.Trabajador.PrimerNombre} {u.Trabajador.PrimerApellido}",
                [ClaimsSesion.UserId] = u.Id.ToString(),
                [ClaimsSesion.Rol] = u.Rol!.Nombre
            };
            if (u.AreaId.HasValue) claims[ClaimsSesion.AreaId] = u.AreaId.Value.ToString();

            var token = new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
            {
                Issuer = Emisor,
                Audience = Audiencia,
                Claims = claims,
                IssuedAt = ahora,
                NotBefore = ahora,
                Expires = ahora.Add(Duracion),
                SigningCredentials = new SigningCredentials(Clave(config), SecurityAlgorithms.HmacSha256)
            });
            return Results.Ok(new { token, expira = ahora.Add(Duracion) });
        });
    }

    public record TokenDesarrolloDto(int UsuarioId);

    // Asegura un trabajador + usuario activo por cada rol que no tenga ninguno (correo dev.<rol>@mafchile.com).
    // Idempotente. Solo se llama en Development.
    public static async Task SembrarUsuariosAsync(AppDbContext db, ILogger logger)
    {
        var rolesSinUsuario = await db.Roles
            .Where(r => !db.UsuariosSistema.Any(u => u.RolId == r.Id && u.Activo && u.Trabajador!.Activo))
            .OrderBy(r => r.Id)
            .ToListAsync();
        if (rolesSinUsuario.Count == 0) return;

        // Datos laborales mínimos tomados de la BD (un área con su dirección corporativa).
        var area = await db.Catalogos.Where(c => c.Tipo == "Area" && c.PadreId != null && c.Activo).OrderBy(c => c.Id).FirstOrDefaultAsync();
        var cargoId = await db.Catalogos.Where(c => c.Tipo == "Cargo" && c.Activo).OrderBy(c => c.Id).Select(c => (int?)c.Id).FirstOrDefaultAsync();
        var lugarId = await db.Catalogos.Where(c => c.Tipo == "LugarTrabajo" && c.Activo).OrderBy(c => c.Id).Select(c => (int?)c.Id).FirstOrDefaultAsync();
        if (area == null || cargoId == null || lugarId == null)
        {
            logger.LogWarning("Login de desarrollo: faltan catálogos (Área, Cargo o LugarTrabajo) para crear usuarios de prueba.");
            return;
        }

        foreach (var rol in rolesSinUsuario)
        {
            var slug = NormalizadorTexto.SinTildes(rol.Nombre).ToLower().Replace(" ", "");
            var correo = $"dev.{slug}@mafchile.com";

            var trabajador = await db.Trabajadores.FirstOrDefaultAsync(t => t.Correo == correo);
            if (trabajador == null)
            {
                var cuerpo = (99000000 + rol.Id).ToString();
                trabajador = new Trabajador
                {
                    Rut = TrabajadorValidator.FormatearParaGuardar(cuerpo + DigitoVerificador(cuerpo)),
                    PrimerNombre = "Dev",
                    PrimerApellido = rol.Nombre,
                    FechaNacimiento = new DateOnly(1990, 1, 1),
                    Sexo = "Prefiere no informar",
                    Correo = correo,
                    DireccionCorporativaId = area.PadreId!.Value,
                    AreaId = area.Id,
                    CargoId = cargoId.Value,
                    LugarTrabajoId = lugarId.Value,
                    FechaIncorporacion = new DateOnly(2026, 1, 1),
                    Activo = true
                };
                db.Trabajadores.Add(trabajador);
                await db.SaveChangesAsync();
            }
            else trabajador.Activo = true;

            db.UsuariosSistema.Add(new UsuarioSistema { TrabajadorId = trabajador.Id, RolId = rol.Id, AreaId = area.Id, Activo = true });
            await db.SaveChangesAsync();
            logger.LogInformation("Login de desarrollo: creado el usuario de prueba {Correo} con rol {Rol}.", correo, rol.Nombre);
        }
    }

    private static string DigitoVerificador(string cuerpo)
    {
        int suma = 0, multiplicador = 2;
        for (int i = cuerpo.Length - 1; i >= 0; i--)
        {
            suma += (cuerpo[i] - '0') * multiplicador;
            multiplicador = multiplicador == 7 ? 2 : multiplicador + 1;
        }
        var resto = 11 - (suma % 11);
        return resto switch { 11 => "0", 10 => "K", _ => resto.ToString() };
    }
}
