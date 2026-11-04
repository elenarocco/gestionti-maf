using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Web;
using MafTi.Api.Seguridad;
using MafTi.Infrastructure;

var builder = WebApplication.CreateBuilder(args);
var esDesarrollo = builder.Environment.IsDevelopment();

// Los secretos no van en appsettings: en desarrollo vienen de user-secrets y en el servidor de
// variables de entorno (ConnectionStrings__Default). Ver backend/CONFIGURACION.md.
var conexion = builder.Configuration.GetConnectionString("Default");
if (string.IsNullOrWhiteSpace(conexion))
    throw new InvalidOperationException(
        "Falta la cadena de conexión ConnectionStrings:Default. En desarrollo: dotnet user-secrets set \"ConnectionStrings:Default\" \"...\"; " +
        "en el servidor: variable de entorno ConnectionStrings__Default. Ver backend/CONFIGURACION.md.");

builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(conexion));

// Autenticación: token de Microsoft Entra ID (sección AzureAd). En Development se acepta además
// el token del login de desarrollo; fuera de Development ese esquema y su clave no se registran.
const string EsquemaSesion = "Sesion";
var auth = builder.Services
    .AddAuthentication(o =>
    {
        o.DefaultScheme = EsquemaSesion;
        o.DefaultChallengeScheme = EsquemaSesion;
    })
    .AddPolicyScheme(EsquemaSesion, EsquemaSesion, o =>
        o.ForwardDefaultSelector = context =>
            esDesarrollo && AuthDesarrollo.EsTokenDeDesarrollo(context)
                ? AuthDesarrollo.Esquema
                : JwtBearerDefaults.AuthenticationScheme);
foreach (var clave in new[] { "TenantId", "ClientId", "Audience" })
    if (string.IsNullOrWhiteSpace(builder.Configuration[$"AzureAd:{clave}"]))
        throw new InvalidOperationException($"Falta AzureAd:{clave} (datos de la app en Entra ID, van en appsettings.json). Ver backend/CONFIGURACION.md.");
auth.AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAd"));
if (esDesarrollo) auth.AgregarEsquema(builder.Configuration);

builder.Services.AddScoped<UsuarioActual>();
builder.Services.AddScoped<MafTi.Api.Common.ServicioSla>();

builder.Services.AddOpenApi();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.AgregarSeguridadHttp();

// Comprime las respuestas JSON (Brotli o gzip, según lo que acepte el navegador): menos datos por la red.
// Es seguro aunque vaya por HTTPS: la sesión viaja en la cabecera Authorization, no en las respuestas,
// así que no hay un secreto que se pueda deducir del tamaño comprimido (ataque BREACH).
builder.Services.AddResponseCompression(o =>
{
    o.EnableForHttps = true;
    o.Providers.Add<Microsoft.AspNetCore.ResponseCompression.BrotliCompressionProvider>();
    o.Providers.Add<Microsoft.AspNetCore.ResponseCompression.GzipCompressionProvider>();
});
builder.Services.Configure<Microsoft.AspNetCore.ResponseCompression.BrotliCompressionProviderOptions>(o => o.Level = System.IO.Compression.CompressionLevel.Fastest);
builder.Services.Configure<Microsoft.AspNetCore.ResponseCompression.GzipCompressionProviderOptions>(o => o.Level = System.IO.Compression.CompressionLevel.Fastest);

var app = builder.Build();
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Trabajadores.AsNoTracking().Take(1).ToListAsync();

    if (esDesarrollo)
        await AuthDesarrollo.SembrarUsuariosAsync(db, scope.ServiceProvider.GetRequiredService<ILogger<Program>>());
}
app.UsarSeguridadHttp();
app.UseResponseCompression();

// Cualquier error no controlado se registra en el servidor y al cliente solo le llega un JSON genérico:
// nunca un stack trace ni detalles internos, sin importar el entorno.
app.UseExceptionHandler(builder => builder.Run(async context =>
{
    var ex = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerPathFeature>()?.Error;
    app.Logger.LogError(ex, "Error no controlado en {Ruta}", context.Request.Path);
    context.Response.StatusCode = StatusCodes.Status500InternalServerError;
    context.Response.ContentType = "application/json";
    await context.Response.WriteAsJsonAsync(new { error = "Ocurrió un error inesperado. Inténtalo de nuevo o avisa a TI." });
}));

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseMiddleware<UsuarioActualMiddleware>();
app.MapControllers();

if (esDesarrollo) AuthDesarrollo.MapearEndpoints(app);

app.Run();
