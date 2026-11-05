using MafTi.Application;
using MafTi.Application.Notificaciones;
using MafTi.Infrastructure.Correo;
using Microsoft.Extensions.Options;

namespace MafTi.Api.Notificaciones;

// Solo en Development: envía un correo de prueba a la dirección indicada, sin pasar por la cola,
// para ver en la respuesta si Graph está bien configurado.
//   POST /api/dev/correo-prueba?para=nombre@mafchile.com
public static class CorreoDesarrollo
{
    public static void MapearEndpoints(WebApplication app)
    {
        app.MapPost("/api/dev/correo-prueba", async (string? para, IEmailService email, IOptions<OpcionesGraph> graph, ILoggerFactory logs) =>
        {
            if (string.IsNullOrWhiteSpace(para) || !para.Contains('@'))
                return Results.BadRequest(new { error = "Indica la dirección de destino: ?para=nombre@mafchile.com" });

            if (!graph.Value.Habilitado)
                return Results.Ok(new
                {
                    enviado = false,
                    mensaje = "Graph:Habilitado = false: no se envió nada, solo quedó en el log. Activa Graph:Habilitado para enviar de verdad."
                });

            var ahoraChile = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, HoraChile.Zona());
            var correo = PlantillasCorreo.Prueba(ahoraChile);
            try
            {
                await email.EnviarAsync(new[] { para.Trim() }, correo.Asunto, correo.Html);
                return Results.Ok(new { enviado = true, para = para.Trim(), desde = graph.Value.Buzon });
            }
            catch (Exception ex)
            {
                logs.CreateLogger("CorreoDesarrollo").LogError(ex, "Falló el correo de prueba a {Para}", para);
                // Solo en Development: se muestra el motivo para poder corregir la configuración.
                return Results.Json(new { enviado = false, error = ex.Message }, statusCode: StatusCodes.Status502BadGateway);
            }
        });
    }
}
