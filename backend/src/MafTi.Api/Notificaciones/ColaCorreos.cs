using System.Threading.Channels;
using MafTi.Application.Notificaciones;

namespace MafTi.Api.Notificaciones;

public record CorreoPendiente(IReadOnlyCollection<string> Destinatarios, string Asunto, string Html);

// Cola en memoria: el request deja el correo y responde de inmediato; EnvioCorreosService lo envía después.
// Si la API se reinicia con correos en la cola, esos correos se pierden (decisión consciente: sin tabla extra).
public class ColaCorreos
{
    private readonly Channel<CorreoPendiente> _canal = Channel.CreateUnbounded<CorreoPendiente>(
        new UnboundedChannelOptions { SingleReader = true });

    public void Encolar(CorreoPendiente correo) => _canal.Writer.TryWrite(correo);

    public IAsyncEnumerable<CorreoPendiente> LeerTodosAsync(CancellationToken ct) => _canal.Reader.ReadAllAsync(ct);
}

// Proceso en segundo plano que envía lo que llega a la cola. Un error de envío se registra y no se reintenta:
// nunca afecta a la solicitud, que ya quedó guardada.
public class EnvioCorreosService : BackgroundService
{
    private readonly ColaCorreos _cola;
    private readonly IEmailService _email;
    private readonly ILogger<EnvioCorreosService> _logger;

    public EnvioCorreosService(ColaCorreos cola, IEmailService email, ILogger<EnvioCorreosService> logger)
    {
        _cola = cola;
        _email = email;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var correo in _cola.LeerTodosAsync(stoppingToken))
        {
            try
            {
                await _email.EnviarAsync(correo.Destinatarios, correo.Asunto, correo.Html, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "No se pudo enviar el correo a {Destinatarios}: {Asunto}",
                    string.Join(", ", correo.Destinatarios), correo.Asunto);
            }
        }
    }
}
