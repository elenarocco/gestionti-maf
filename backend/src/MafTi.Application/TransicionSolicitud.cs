namespace MafTi.Application;

// Reglas de cambio de estado de una solicitud.
//   Bloqueo:              Pendiente --(RRHH aprueba)--> En proceso --(TI completa)--> Completada
//   Ingreso/Modificación: Pendiente --(TI inicia)----> En proceso --(TI completa)--> Completada
//   Cualquiera:           Pendiente o En proceso --(rechazo con motivo)--> Rechazada (final)
// Cada método devuelve el error a mostrar, o null si la transición es válida.
public static class TransicionSolicitud
{
    public const string Pendiente = "Pendiente";
    public const string EnProceso = "En proceso";
    public const string Completada = "Completada";
    public const string Rechazada = "Rechazada";

    public const int LargoMaximoMotivo = 200;

    public static string? ValidarAprobar(string tipo, string estado)
    {
        if (tipo != "Bloqueo") return "Solo las solicitudes de Bloqueo se aprueban; las demás se inician.";
        if (estado != Pendiente) return $"Solo se puede aprobar una solicitud Pendiente (está {estado}).";
        return null;
    }

    public static string? ValidarIniciar(string tipo, string estado)
    {
        if (tipo == "Bloqueo") return "Un Bloqueo pasa a En proceso cuando RRHH lo aprueba.";
        if (tipo != "Ingreso" && tipo != "Modificación") return $"Las solicitudes de tipo {tipo} aún no tienen flujo de estados.";
        if (estado != Pendiente) return $"Solo se puede iniciar una solicitud Pendiente (está {estado}).";
        return null;
    }

    public static string? ValidarRechazar(string estado, string? motivo)
    {
        if (estado != Pendiente && estado != EnProceso)
            return $"Solo se puede rechazar una solicitud Pendiente o En proceso (está {estado}).";
        var m = motivo?.Trim() ?? "";
        if (m.Length == 0) return "El motivo de rechazo es obligatorio.";
        if (m.Length > LargoMaximoMotivo) return $"El motivo de rechazo no puede superar los {LargoMaximoMotivo} caracteres.";
        return null;
    }

    public static string? ValidarCompletar(string estado)
    {
        if (estado != EnProceso) return $"Solo se puede completar una solicitud En proceso (está {estado}).";
        return null;
    }
}
