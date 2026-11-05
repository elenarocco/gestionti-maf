namespace MafTi.Api.Notificaciones;

// Sección "Notificaciones" (no secreta: va en appsettings.json y se puede cambiar por ambiente).
public class OpcionesNotificaciones
{
    public const string Seccion = "Notificaciones";

    public string CorreoTi { get; set; } = "";     // recibe el aviso de cada solicitud nueva
    public string UrlFront { get; set; } = "";     // para los enlaces de los correos, sin "/" final

    // Solo para pruebas: si tiene un valor, TODOS los correos van a esa dirección (el asunto indica a quién
    // iban). Evita escribirle a TI o a usuarios reales mientras se prueba. Vacío en producción.
    public string RedirigirA { get; set; } = "";
}
