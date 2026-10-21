using static MafTi.Application.SolicitudValores;

namespace MafTi.Application;

// Reglas de transición de estado de las solicitudes en la Bandeja de TI y en el flujo VPN.
// El controller consulta aquí qué acciones aplican; el frontend solo pinta las que llegan.
public static class FlujoSolicitud
{
    public const string Tomar = "tomar";
    public const string Completar = "completar";
    public const string Rechazar = "rechazar";
    public const string ConfirmarEnvio = "confirmarEnvio";
    public const string RechazarFicha = "rechazarFicha";

    public record Situacion(string Tipo, string Estado, bool FormularioHabilitado = false, bool FichaCompletada = false);

    // Acciones de Admin TI según tipo y estado (valores de BD).
    public static List<string> AccionesTi(Situacion s)
    {
        var acciones = new List<string>();

        if (s.Tipo == TipoVpn)
        {
            if (s.Estado == Pendiente)
            {
                // Fase 1: TI aprueba el acceso (habilita la ficha) o lo rechaza. Ya habilitada, aún puede rechazarla.
                if (!s.FormularioHabilitado) acciones.Add(Tomar);
                acciones.Add(Rechazar);
            }
            else if (s.Estado == EnProceso)
            {
                // Fase 3: la jefatura envió la ficha.
                acciones.Add(ConfirmarEnvio);
                acciones.Add(RechazarFicha);
            }
            return acciones;
        }

        if (s.Estado == Pendiente)
        {
            // Bloqueo lo aprueba RRHH, no TI; TI solo puede rechazarlo.
            if (s.Tipo != TipoBloqueo) acciones.Add(Tomar);
            acciones.Add(Rechazar);
        }
        else if (s.Estado == EnProceso)
        {
            acciones.Add(Completar);
            acciones.Add(Rechazar);
        }
        return acciones;
    }

    // RRHH aprueba un Bloqueo pendiente: pasa a EnProceso para que TI lo ejecute.
    public static bool PuedeAprobarRrhh(Situacion s) => s.Tipo == TipoBloqueo && s.Estado == Pendiente;

    // Jefatura creadora completa la ficha VPN solo si TI la habilitó y sigue Pendiente.
    public static bool PuedeCompletarFicha(Situacion s) =>
        s.Tipo == TipoVpn && s.Estado == Pendiente && s.FormularioHabilitado;

    // Estado al que pasa la solicitud con cada acción de TI (null = no cambia).
    public static string? EstadoTras(string accion, Situacion s) => accion switch
    {
        Tomar => s.Tipo == TipoVpn ? null : EnProceso,   // VPN fase 1: sigue Pendiente con la ficha habilitada
        Completar => Completada,
        ConfirmarEnvio => Completada,
        Rechazar or RechazarFicha => Rechazada,
        _ => null
    };

    // Texto que queda en HistorialSolicitud.
    public static string AccionHistorial(string accion, Situacion s) => accion switch
    {
        Tomar => s.Tipo == TipoVpn ? "Acceso VPN aprobado" : "Aprobada por TI",
        Completar => "Completada",
        ConfirmarEnvio => "Ficha VPN enviada al proveedor",
        Rechazar => "Rechazada",
        RechazarFicha => "Ficha VPN rechazada",
        _ => accion
    };
}
