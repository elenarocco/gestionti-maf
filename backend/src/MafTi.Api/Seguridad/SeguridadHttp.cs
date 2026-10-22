using Microsoft.AspNetCore.HttpOverrides;

namespace MafTi.Api.Seguridad;

// Configuración HTTP para publicar la API: CORS, dominios aceptados, HTTPS detrás de un proxy y
// cabeceras de seguridad. Lo que cambia entre entornos se lee de la configuración (ver CONFIGURACION.md).
public static class SeguridadHttp
{
    public const string PoliticaCors = "Frontend";

    public static void AgregarSeguridadHttp(this WebApplicationBuilder builder)
    {
        var config = builder.Configuration;
        var esDesarrollo = builder.Environment.IsDevelopment();

        // Orígenes (URL del frontend) que pueden llamar a la API desde el navegador.
        var origenes = config.GetSection("Cors:Origenes").Get<string[]>()?
            .Where(o => !string.IsNullOrWhiteSpace(o)).ToArray() ?? [];
        if (origenes.Length == 0)
            throw new InvalidOperationException(
                "Falta Cors:Origenes (la URL del frontend). En el servidor: variable de entorno Cors__Origenes__0=https://... Ver backend/CONFIGURACION.md.");

        // Fuera de desarrollo la API solo responde a los dominios declarados, nunca a cualquiera.
        var hosts = config["AllowedHosts"];
        if (!esDesarrollo && (string.IsNullOrWhiteSpace(hosts) || hosts.Trim() == "*"))
            throw new InvalidOperationException(
                "Falta AllowedHosts (el dominio de la API, por ejemplo api.gestionti.mafchile.cl). En el servidor: variable de entorno AllowedHosts=... Ver backend/CONFIGURACION.md.");

        builder.Services.AddCors(o => o.AddPolicy(PoliticaCors, p => p
            .WithOrigins(origenes)
            .WithMethods("GET", "POST", "PUT", "PATCH", "DELETE")
            .WithHeaders("Authorization", "Content-Type")
            // El frontend lee el nombre del archivo de los reportes descargados.
            .WithExposedHeaders("Content-Disposition")));

        // El servidor web (IIS o nginx) recibe el HTTPS y le pasa la petición a la API; estas cabeceras
        // le dicen a la API la IP real del cliente y que la petición original venía por HTTPS.
        // Por defecto solo se confía en un proxy en la misma máquina (localhost).
        builder.Services.Configure<ForwardedHeadersOptions>(o =>
            o.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto);

        // Kestrel no anuncia qué servidor es.
        builder.WebHost.ConfigureKestrel(o => o.AddServerHeader = false);
    }

    public static void UsarSeguridadHttp(this WebApplication app)
    {
        app.UseForwardedHeaders();

        if (!app.Environment.IsDevelopment())
        {
            app.UseHsts();               // el navegador recuerda que este dominio va solo por HTTPS
            app.UseHttpsRedirection();   // una petición por HTTP se redirige a HTTPS
        }

        var esDesarrollo = app.Environment.IsDevelopment();
        app.Use(async (context, next) =>
        {
            var h = context.Response.Headers;
            h["X-Content-Type-Options"] = "nosniff";          // el navegador no adivina el tipo de contenido
            h["X-Frame-Options"] = "DENY";                    // la API no se puede incrustar en otra página
            h["Referrer-Policy"] = "no-referrer";
            h["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
            // La API solo entrega JSON: no se ejecuta nada en el navegador. Swagger (solo en desarrollo) sí lo necesita.
            if (!(esDesarrollo && context.Request.Path.StartsWithSegments("/swagger")))
                h["Content-Security-Policy"] = "default-src 'none'; frame-ancestors 'none'";
            // Las respuestas traen datos de personas: que no queden guardadas en cachés del navegador ni de proxies.
            if (context.Request.Path.StartsWithSegments("/api"))
                h.CacheControl = "no-store";
            await next();
        });

        app.UseCors(PoliticaCors);
    }
}
