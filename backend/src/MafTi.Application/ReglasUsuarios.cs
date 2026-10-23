namespace MafTi.Application;

// Reglas para dar, cambiar o quitar el acceso de un usuario a la plataforma. Devuelven el mensaje de error
// o null si el cambio se puede hacer. El controlador junta los datos; aquí solo se decide.
public static class ReglasUsuarios
{
    public const string RolJefatura = "Jefatura";

    // Dar acceso a un trabajador.
    public static string? ErrorAlDarAcceso(bool trabajadorActivo, bool yaTieneAcceso, string rolNombre, int? areaId)
    {
        if (!trabajadorActivo) return "El trabajador está inactivo: no se le puede dar acceso.";
        if (yaTieneAcceso) return "El trabajador ya tiene acceso. Cambia su rol o reactívalo desde la lista.";
        return ErrorDeArea(rolNombre, areaId);
    }

    // Cambiar el rol o el área. quitaAdministracion = el usuario pasa de un rol que administra usuarios a uno que no.
    public static string? ErrorAlCambiar(bool esUnoMismo, bool quitaAdministracion, int administradoresActivos,
        string rolNombre, int? areaId)
    {
        if (esUnoMismo) return "No puedes cambiar tu propio rol ni tu área: pídeselo a otro administrador.";
        if (quitaAdministracion && administradoresActivos <= 1)
            return "Es el único administrador activo: la plataforma quedaría sin nadie que pueda administrar usuarios.";
        return ErrorDeArea(rolNombre, areaId);
    }

    // Quitar el acceso (desactivar).
    public static string? ErrorAlDesactivar(bool esUnoMismo, bool esAdministrador, int administradoresActivos)
    {
        if (esUnoMismo) return "No puedes quitarte tu propio acceso.";
        if (esAdministrador && administradoresActivos <= 1)
            return "Es el único administrador activo: la plataforma quedaría sin nadie que pueda administrar usuarios.";
        return null;
    }

    // Reactivar un acceso: el trabajador tiene que seguir activo.
    public static string? ErrorAlReactivar(bool trabajadorActivo) =>
        trabajadorActivo ? null : "El trabajador está inactivo: no se puede reactivar su acceso.";

    // La Jefatura solo ve su área: sin área no vería a nadie.
    private static string? ErrorDeArea(string rolNombre, int? areaId) =>
        rolNombre == RolJefatura && areaId is null ? "El rol Jefatura necesita un área." : null;
}
