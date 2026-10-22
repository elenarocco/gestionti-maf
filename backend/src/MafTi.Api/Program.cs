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
auth.AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAd"));
if (esDesarrollo) auth.AgregarEsquema(builder.Configuration);

builder.Services.AddScoped<UsuarioActual>();

builder.Services.AddOpenApi();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors(options =>
{
    options.AddPolicy("PermitirAngular", policy =>
    {
        policy.WithOrigins("http://localhost:4200")
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Trabajadores.AsNoTracking().Take(1).ToListAsync();

    if (esDesarrollo)
        await AuthDesarrollo.SembrarUsuariosAsync(db, scope.ServiceProvider.GetRequiredService<ILogger<Program>>());
}
app.UseCors("PermitirAngular");

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseMiddleware<UsuarioActualMiddleware>();
app.MapControllers();

if (esDesarrollo) AuthDesarrollo.MapearEndpoints(app);

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast = Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast");

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
