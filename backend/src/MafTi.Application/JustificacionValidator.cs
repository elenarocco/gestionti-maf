using System.Text.RegularExpressions;

namespace MafTi.Application;

public static class JustificacionValidator
{
    public const int LargoMaximo = 200;

    public static List<string> Validar(string? justificacion)
    {
        var errores = new List<string>();

        var j = justificacion?.Trim() ?? "";
        if (j.Length == 0)
            errores.Add("La justificación es obligatoria.");
        else if (j.Length > LargoMaximo)
            errores.Add($"La justificación no puede superar los {LargoMaximo} caracteres.");
        else if (!Regex.IsMatch(j, @"^[\p{L}\s,.]+$"))
            errores.Add("La justificación solo puede contener letras, espacios, comas y puntos.");

        return errores;
    }
}
