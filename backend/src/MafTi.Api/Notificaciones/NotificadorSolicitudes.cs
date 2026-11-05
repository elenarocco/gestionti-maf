using MafTi.Application;
using MafTi.Application.Notificaciones;
using MafTi.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using static MafTi.Application.SolicitudValores;

namespace MafTi.Api.Notificaciones;

// Arma los correos de cada evento de una solicitud y los deja en la cola (el envío ocurre después, en segundo plano).
// Nunca lanza excepciones: si algo falla al armar un aviso, se registra en el log y la solicitud sigue su curso.
//   - Solicitud nueva          → TI (Notificaciones:CorreoTi)
//   - Rechazada (con motivo)   → quien la creó
//   - Completada               → quien la creó
//   - VPN aprobada por TI      → quien la creó, para que complete la ficha del proveedor
public class NotificadorSolicitudes
{
    private readonly AppDbContext _context;
    private readonly ColaCorreos _cola;
    private readonly OpcionesNotificaciones _opciones;
    private readonly ILogger<NotificadorSolicitudes> _logger;

    public NotificadorSolicitudes(AppDbContext context, ColaCorreos cola, IOptions<OpcionesNotificaciones> opciones,
        ILogger<NotificadorSolicitudes> logger)
    {
        _context = context;
        _cola = cola;
        _opciones = opciones.Value;
        _logger = logger;
    }

    public Task SolicitudCreadaAsync(int solicitudId) => AvisarAsync(solicitudId, "solicitud nueva", (d, url) =>
        (new[] { _opciones.CorreoTi }, PlantillasCorreo.NuevaSolicitud(d.Datos, url)));

    public Task SolicitudRechazadaAsync(int solicitudId) => AvisarAsync(solicitudId, "rechazo", (d, url) =>
        (new[] { d.CorreoCreador }, PlantillasCorreo.Rechazada(d.Datos, d.MotivoRechazo ?? "", d.Datos.Tipo != TipoVpn, url)));

    public Task SolicitudCompletadaAsync(int solicitudId, string? detalle) => AvisarAsync(solicitudId, "solicitud completada", (d, url) =>
        (new[] { d.CorreoCreador }, PlantillasCorreo.Completada(d.Datos, detalle, url)));

    public Task VpnAprobadaAsync(int solicitudId) => AvisarAsync(solicitudId, "VPN aprobada", (d, url) =>
        (new[] { d.CorreoCreador }, PlantillasCorreo.VpnAprobada(d.Datos, url)));

    private record Cargada(PlantillasCorreo.DatosSolicitud Datos, string CorreoCreador, string? MotivoRechazo);

    private async Task AvisarAsync(int solicitudId, string evento,
        Func<Cargada, string, (string[] Para, PlantillasCorreo.Correo Correo)> armar)
    {
        try
        {
            var cargada = await CargarAsync(solicitudId);
            if (cargada == null)
            {
                _logger.LogWarning("Aviso de {Evento}: la solicitud {Id} no existe.", evento, solicitudId);
                return;
            }

            var (para, correo) = armar(cargada, _opciones.UrlFront.TrimEnd('/'));
            var destinatarios = para.Where(d => !string.IsNullOrWhiteSpace(d)).Select(d => d.Trim()).Distinct().ToArray();
            if (destinatarios.Length == 0)
            {
                _logger.LogWarning("Aviso de {Evento} de la solicitud {Id}: no hay destinatario configurado.", evento, solicitudId);
                return;
            }

            var asunto = correo.Asunto;
            if (!string.IsNullOrWhiteSpace(_opciones.RedirigirA))
            {
                asunto = $"[Prueba, para: {string.Join(", ", destinatarios)}] {asunto}";
                destinatarios = new[] { _opciones.RedirigirA.Trim() };
            }

            _cola.Encolar(new CorreoPendiente(destinatarios, asunto, correo.Html));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "No se pudo preparar el aviso de {Evento} de la solicitud {Id}.", evento, solicitudId);
        }
    }

    private async Task<Cargada?> CargarAsync(int solicitudId)
    {
        var s = await _context.Solicitudes
            .Where(x => x.Id == solicitudId)
            .Select(x => new
            {
                x.Id, x.Tipo, x.FechaCreacion, x.FechaVencimientoSLA, x.MotivoRechazo,
                CreadoPor = x.CreadoPor!.Trabajador!.PrimerNombre + " " + x.CreadoPor.Trabajador.PrimerApellido,
                CorreoCreador = x.CreadoPor.Trabajador.Correo,
                // Sin trabajador: un Ingreso aún no completado (sus datos están en la solicitud) o una VPN (el proveedor).
                Persona = x.TrabajadorId != null
                    ? x.Trabajador!.PrimerNombre + " " + x.Trabajador.PrimerApellido
                    : x.Tipo == TipoIngreso
                        ? _context.SolicitudesIngreso.Where(i => i.SolicitudId == x.Id)
                            .Select(i => i.PrimerNombre + " " + i.PrimerApellido).FirstOrDefault()
                        : _context.SolicitudesVpn.Where(v => v.SolicitudId == x.Id && v.Nombres != null)
                            .Select(v => v.Nombres + " " + v.Apellidos).FirstOrDefault()
            })
            .FirstOrDefaultAsync();
        if (s == null) return null;

        var zona = HoraChile.Zona();
        DateTime AChile(DateTime utc) => TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), zona);

        return new Cargada(
            new PlantillasCorreo.DatosSolicitud(s.Id, s.Tipo, s.Persona, s.CreadoPor, AChile(s.FechaCreacion), AChile(s.FechaVencimientoSLA)),
            s.CorreoCreador,
            s.MotivoRechazo);
    }
}
