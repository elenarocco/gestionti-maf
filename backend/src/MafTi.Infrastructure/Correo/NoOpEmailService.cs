using MafTi.Application.Notificaciones;
using Microsoft.Extensions.Logging;

namespace MafTi.Infrastructure.Correo;

// Con Graph:Habilitado = false: no envía nada, solo deja en el log lo que se habría enviado.
public class NoOpEmailService : IEmailService
{
    private readonly ILogger<NoOpEmailService> _logger;

    public NoOpEmailService(ILogger<NoOpEmailService> logger) => _logger = logger;

    public Task EnviarAsync(IReadOnlyCollection<string> destinatarios, string asunto, string cuerpoHtml, CancellationToken ct = default)
    {
        _logger.LogInformation("Correo NO enviado (Graph:Habilitado = false). Para: {Destinatarios}. Asunto: {Asunto}",
            string.Join(", ", destinatarios), asunto);
        return Task.CompletedTask;
    }
}
