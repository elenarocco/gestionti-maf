using System.Net;
using System.Text.Json;
using MafTi.Infrastructure.Correo;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Graph;
using Microsoft.Kiota.Abstractions.Authentication;

namespace MafTi.Tests;

// Verifica la petición que GraphEmailService le hace a Microsoft Graph, con un servidor simulado (sin red ni credenciales).
public class GraphEmailServiceTests
{
    private class ServidorSimulado : HttpMessageHandler
    {
        public HttpRequestMessage? Peticion;
        public string? Cuerpo;
        public HttpStatusCode Respuesta = HttpStatusCode.Accepted;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            Peticion = request;
            Cuerpo = request.Content == null ? null : await request.Content.ReadAsStringAsync(ct);
            return new HttpResponseMessage(Respuesta) { Content = new StringContent(Respuesta == HttpStatusCode.Accepted ? "" : "{\"error\":{\"code\":\"ErrorAccessDenied\",\"message\":\"Access is denied.\"}}") };
        }
    }

    private static (GraphEmailService Servicio, ServidorSimulado Servidor) Crear()
    {
        var servidor = new ServidorSimulado();
        var graph = new GraphServiceClient(new HttpClient(servidor), new AnonymousAuthenticationProvider());
        return (new GraphEmailService(graph, "gestionti@mafchile.com", NullLogger<GraphEmailService>.Instance), servidor);
    }

    [Fact]
    public async Task Envia_POST_sendMail_desde_el_buzon_configurado_con_HTML_y_destinatarios()
    {
        var (servicio, servidor) = Crear();

        await servicio.EnviarAsync(new[] { "callcentermaf@mafchile.com", "otra@mafchile.com" }, "Asunto de prueba", "<p>Hola</p>");

        Assert.Equal(HttpMethod.Post, servidor.Peticion!.Method);
        Assert.Equal("/v1.0/users/gestionti@mafchile.com/sendMail", Uri.UnescapeDataString(servidor.Peticion.RequestUri!.AbsolutePath));

        // El SDK escribe "Message" y "SaveToSentItems" con mayúscula; Graph no distingue mayúsculas en estos parámetros.
        using var json = JsonDocument.Parse(servidor.Cuerpo!);
        var mensaje = Propiedad(json.RootElement, "message");
        Assert.Equal("Asunto de prueba", mensaje.GetProperty("subject").GetString());
        Assert.Equal("html", mensaje.GetProperty("body").GetProperty("contentType").GetString());
        Assert.Equal("<p>Hola</p>", mensaje.GetProperty("body").GetProperty("content").GetString());
        Assert.Equal(new[] { "callcentermaf@mafchile.com", "otra@mafchile.com" },
            mensaje.GetProperty("toRecipients").EnumerateArray().Select(r => r.GetProperty("emailAddress").GetProperty("address").GetString()));
        Assert.True(Propiedad(json.RootElement, "saveToSentItems").GetBoolean());
    }

    private static JsonElement Propiedad(JsonElement e, string nombre) =>
        e.EnumerateObject().First(p => string.Equals(p.Name, nombre, StringComparison.OrdinalIgnoreCase)).Value;

    [Fact]
    public async Task Sin_destinatarios_no_llama_a_Graph()
    {
        var (servicio, servidor) = Crear();
        await servicio.EnviarAsync(Array.Empty<string>(), "Nada", "<p></p>");
        Assert.Null(servidor.Peticion);
    }

    [Fact]
    public async Task Si_Graph_rechaza_el_envio_lanza_una_excepcion_para_que_se_registre()
    {
        var (servicio, servidor) = Crear();
        servidor.Respuesta = HttpStatusCode.Forbidden;
        await Assert.ThrowsAnyAsync<Exception>(() => servicio.EnviarAsync(new[] { "a@mafchile.com" }, "x", "<p>x</p>"));
    }

    [Fact]
    public void Indica_que_claves_faltan_para_poder_enviar()
    {
        var o = new OpcionesGraph { Habilitado = true, TenantId = "t", ClientId = "", ClientSecret = "s", Buzon = "" };
        Assert.Equal(new[] { "Graph:ClientId", "Graph:Buzon" }, o.Faltantes());
    }
}
