using System.Text.RegularExpressions;

namespace MafTi.Application;

public static class SolicitudVpnValidator
{
    public const int LargoMaximoMotivo = 200;
    public const int LargoMaximoJustificacionRechazo = 500;

    public record Ficha(
        DateOnly? FechaIncorporacion, DateOnly? FechaCaducidad, string? Rut,
        string? Cargo, string? Nombres, string? Apellidos, string? Departamento, string? UbicacionFisica,
        bool? EquipoCorporativo, string? CompaniaExterna, string? CuentaCorreo, string? Motivo);

    // Errores por campo (clave en camelCase, como en el JSON), un mensaje por campo.
    public static Dictionary<string, string> ValidarFicha(Ficha f)
    {
        var errores = new Dictionary<string, string>();

        if (f.FechaIncorporacion == null)
            errores["fechaIncorporacion"] = "La fecha de incorporación es obligatoria.";
        if (f.FechaCaducidad == null)
            errores["fechaCaducidad"] = "La fecha de caducidad es obligatoria.";
        else if (f.FechaIncorporacion != null && f.FechaCaducidad <= f.FechaIncorporacion)
            errores["fechaCaducidad"] = "La fecha de caducidad debe ser posterior a la fecha de incorporación.";

        if (string.IsNullOrWhiteSpace(f.Rut))
            errores["rut"] = "El RUT es obligatorio.";
        else if (TrabajadorValidator.ValidarFormatoRut(f.Rut) is string errorRut)
            errores["rut"] = errorRut;

        Texto(errores, "cargo", f.Cargo, "El cargo es obligatorio.", "El cargo", 100);
        Texto(errores, "nombres", f.Nombres, "Los nombres son obligatorios.", "Los nombres", 100);
        Texto(errores, "apellidos", f.Apellidos, "Los apellidos son obligatorios.", "Los apellidos", 100);
        Texto(errores, "departamento", f.Departamento, "El departamento es obligatorio.", "El departamento", 100);
        Texto(errores, "ubicacionFisica", f.UbicacionFisica, "La ubicación física es obligatoria.", "La ubicación física", 150);

        if (f.EquipoCorporativo == null)
            errores["equipoCorporativo"] = "Debe indicar si el equipo pertenece a la compañía.";
        else if (f.EquipoCorporativo == false)
            Texto(errores, "companiaExterna", f.CompaniaExterna,
                "La compañía externa es obligatoria cuando el equipo no pertenece a la compañía.", "La compañía externa", 150);

        var correo = f.CuentaCorreo?.Trim() ?? "";
        if (correo.Length == 0)
            errores["cuentaCorreo"] = "La cuenta de correo es obligatoria.";
        else if (correo.Length > 150)
            errores["cuentaCorreo"] = "La cuenta de correo no puede superar los 150 caracteres.";
        else if (!Regex.IsMatch(correo, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
            errores["cuentaCorreo"] = "La cuenta de correo no tiene un formato válido.";

        // Misma regla de texto que la justificación de Bloqueo y Modificación.
        var motivo = f.Motivo?.Trim() ?? "";
        if (motivo.Length == 0)
            errores["motivo"] = "El motivo es obligatorio.";
        else if (motivo.Length > LargoMaximoMotivo)
            errores["motivo"] = $"El motivo no puede superar los {LargoMaximoMotivo} caracteres.";
        else if (!Regex.IsMatch(motivo, @"^[\p{L}\s,.]+$"))
            errores["motivo"] = "El motivo solo puede contener letras, espacios, comas y puntos.";

        return errores;
    }

    // null si es válida.
    public static string? ValidarJustificacionRechazo(string? justificacion)
    {
        var j = justificacion?.Trim() ?? "";
        if (j.Length == 0) return "La justificación del rechazo es obligatoria";
        if (j.Length > LargoMaximoJustificacionRechazo)
            return $"La justificación del rechazo no puede superar los {LargoMaximoJustificacionRechazo} caracteres";
        return null;
    }

    private static void Texto(Dictionary<string, string> errores, string clave, string? valor,
        string mensajeObligatorio, string campo, int largoMaximo)
    {
        var v = valor?.Trim() ?? "";
        if (v.Length == 0)
            errores[clave] = mensajeObligatorio;
        else if (v.Length > largoMaximo)
            errores[clave] = $"{campo} no puede superar los {largoMaximo} caracteres.";
    }
}
