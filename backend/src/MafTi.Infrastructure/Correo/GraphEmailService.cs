using Azure.Identity;
using MafTi.Application.Notificaciones;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using Microsoft.Graph.Users.Item.SendMail;

namespace MafTi.Infrastructure.Correo;

// Envía con Microsoft Graph usando el permiso de aplicación Mail.Send (client credentials):
// POST /users/{Graph:Buzon}/sendMail. En Exchange Online, TI debe restringir la app a ese buzón
// (RBAC for Applications o Application Access Policy); si no, Mail.Send permite enviar como cualquiera.
public class GraphEmailService : IEmailService
{
    private readonly GraphServiceClient _graph;
    private readonly string _buzon;
    private readonly ILogger<GraphEmailService> _logger;

    public GraphEmailService(IOptions<OpcionesGraph> opciones, ILogger<GraphEmailService> logger)
        : this(CrearCliente(opciones.Value), opciones.Value.Buzon, logger) { }

    // Para tests: permite pasar un cliente con un HttpClient simulado.
    public GraphEmailService(GraphServiceClient graph, string buzon, ILogger<GraphEmailService> logger)
    {
        _graph = graph;
        _buzon = buzon;
        _logger = logger;
    }

    private static GraphServiceClient CrearCliente(OpcionesGraph o)
    {
        var credencial = new ClientSecretCredential(o.TenantId, o.ClientId, o.ClientSecret);
        return new GraphServiceClient(credencial, new[] { "https://graph.microsoft.com/.default" });
    }

    public async Task EnviarAsync(IReadOnlyCollection<string> destinatarios, string asunto, string cuerpoHtml, CancellationToken ct = default)
    {
        if (destinatarios.Count == 0) return;

        var cuerpo = new SendMailPostRequestBody
        {
            Message = new Message
            {
                Subject = asunto,
                Body = new ItemBody { ContentType = BodyType.Html, Content = cuerpoHtml },
                ToRecipients = destinatarios
                    .Select(d => new Recipient { EmailAddress = new EmailAddress { Address = d } })
                    .ToList()
            },
            SaveToSentItems = true
        };

        await _graph.Users[_buzon].SendMail.PostAsync(cuerpo, cancellationToken: ct);
        _logger.LogInformation("Correo enviado a {Destinatarios}: {Asunto}", string.Join(", ", destinatarios), asunto);
    }
}
