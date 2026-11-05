using System.Text.Encodings.Web;
using System.Text.Unicode;

namespace MafTi.Application.Notificaciones;

// Correos de notificación en español. Funciones puras: reciben los datos ya resueltos y devuelven asunto y HTML.
// Todo texto que viene de la base se codifica (HtmlEncode) antes de entrar al HTML.
public static class PlantillasCorreo
{
    public record Correo(string Asunto, string Html);

    // Datos de la solicitud que usan las plantillas. Las fechas van ya en hora de Chile.
    public record DatosSolicitud(
        int Id, string Tipo, string? Persona, string CreadoPor, DateTime FechaCreacionChile, DateTime VenceChile);

    public static Correo NuevaSolicitud(DatosSolicitud s, string urlFront) => new(
        $"Nueva solicitud de {s.Tipo} N° {s.Id}{ConPersona(s.Persona)}",
        Marco("Nueva solicitud para TI", $"""
            <p>Se creó una solicitud de <strong>{H(s.Tipo)}</strong> que espera atención en la Bandeja de TI.</p>
            {Tabla(s)}
            {Boton("Abrir en la Bandeja de TI", $"{urlFront}/bandeja-ti/{s.Id}")}
            """));

    public static Correo Rechazada(DatosSolicitud s, string motivo, bool sePuedeCorregir, string urlFront) => new(
        $"Tu solicitud de {s.Tipo} N° {s.Id} fue rechazada",
        Marco("Solicitud rechazada", $"""
            <p>TI rechazó tu solicitud de <strong>{H(s.Tipo)}</strong> N° {s.Id}{ConPersonaHtml(s.Persona)}.</p>
            <p style="margin:16px 0;padding:12px 16px;background:#fde8ec;border-radius:6px;color:#a80020">
              <strong>Motivo:</strong> {H(motivo)}
            </p>
            {(sePuedeCorregir ? "<p>Puedes corregirla desde el detalle de la solicitud: se abre el mismo formulario con los datos anteriores.</p>" : "")}
            {Boton("Ver la solicitud", $"{urlFront}/solicitudes/{s.Id}")}
            """));

    public static Correo Completada(DatosSolicitud s, string? detalle, string urlFront) => new(
        $"Tu solicitud de {s.Tipo} N° {s.Id} fue completada",
        Marco("Solicitud completada", $"""
            <p>TI completó tu solicitud de <strong>{H(s.Tipo)}</strong> N° {s.Id}{ConPersonaHtml(s.Persona)}.</p>
            {(string.IsNullOrWhiteSpace(detalle) ? "" : $"<p>{H(detalle)}</p>")}
            {Boton("Ver la solicitud", $"{urlFront}/solicitudes/{s.Id}")}
            """));

    public static Correo VpnAprobada(DatosSolicitud s, string urlFront) => new(
        $"Acceso VPN N° {s.Id} aprobado: completa la ficha del proveedor",
        Marco("Acceso VPN aprobado", $"""
            <p>TI aprobó la solicitud de acceso VPN N° {s.Id}. Para continuar, completa la ficha del proveedor.</p>
            {Boton("Completar la ficha", $"{urlFront}/solicitudes/vpn/{s.Id}/ficha")}
            """));

    public static Correo Prueba(DateTime ahoraChile) => new(
        "Correo de prueba de GestiónTI",
        Marco("Correo de prueba", $"""
            <p>Si recibes este correo, el envío con Microsoft Graph está bien configurado.</p>
            <p style="color:#6b7280">Enviado el {ahoraChile:dd-MM-yyyy} a las {ahoraChile:HH:mm} (hora de Chile).</p>
            """));

    // ---------- Piezas comunes ----------

    // Codifica solo lo que puede romper el HTML (<, >, &, comillas) y deja las tildes y la ñ legibles.
    private static readonly HtmlEncoder Codificador = HtmlEncoder.Create(UnicodeRanges.All);
    private static string H(string? texto) => Codificador.Encode(texto ?? "");

    private static string ConPersona(string? persona) => string.IsNullOrWhiteSpace(persona) ? "" : $" — {persona}";
    private static string ConPersonaHtml(string? persona) => string.IsNullOrWhiteSpace(persona) ? "" : $" ({H(persona)})";

    private static string Tabla(DatosSolicitud s) => $"""
        <table style="border-collapse:collapse;margin:16px 0;font-size:14px">
          {Fila("N°", s.Id.ToString())}
          {Fila("Tipo", s.Tipo)}
          {(string.IsNullOrWhiteSpace(s.Persona) ? "" : Fila("Trabajador / proveedor", s.Persona))}
          {Fila("Creada por", s.CreadoPor)}
          {Fila("Fecha", s.FechaCreacionChile.ToString("dd-MM-yyyy HH:mm"))}
          {Fila("Plazo (SLA)", s.VenceChile.ToString("dd-MM-yyyy HH:mm"))}
        </table>
        """;

    private static string Fila(string etiqueta, string valor) =>
        $"""<tr><td style="padding:4px 16px 4px 0;color:#6b7280">{H(etiqueta)}</td><td style="padding:4px 0;color:#111827">{H(valor)}</td></tr>""";

    private static string Boton(string texto, string url) =>
        $"""<p style="margin:24px 0"><a href="{H(url)}" style="background:#E4032E;color:#ffffff;text-decoration:none;padding:10px 18px;border-radius:6px;font-weight:600">{H(texto)}</a></p>""";

    private static string Marco(string titulo, string contenido) => $"""
        <div style="font-family:Segoe UI,Roboto,Arial,sans-serif;max-width:560px;color:#374151;font-size:14px;line-height:1.5">
          <div style="border-bottom:3px solid #E4032E;padding-bottom:8px;margin-bottom:16px">
            <strong style="font-size:16px;color:#111827">GestiónTI · MAF Chile</strong>
          </div>
          <h2 style="font-size:18px;color:#111827;margin:0 0 12px">{H(titulo)}</h2>
          {contenido}
          <p style="margin-top:32px;font-size:12px;color:#9ca3af">Correo automático del sistema GestiónTI. No respondas a este mensaje.</p>
        </div>
        """;
}
