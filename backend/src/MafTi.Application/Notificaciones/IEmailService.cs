namespace MafTi.Application.Notificaciones;

// Envío de correos. En producción lo implementa GraphEmailService (Microsoft Graph); con
// Graph:Habilitado = false, NoOpEmailService solo lo registra en el log.
public interface IEmailService
{
    Task EnviarAsync(IReadOnlyCollection<string> destinatarios, string asunto, string cuerpoHtml, CancellationToken ct = default);
}
