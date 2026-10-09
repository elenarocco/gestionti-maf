using System.Globalization;
using System.Text;

namespace MafTi.Application;

public static class NormalizadorTexto
{
    // Quita tildes y diéresis (á→a, ü→u), convierte ñ/Ñ en n/N y deja el resultado en minúsculas.
    // Se usa para armar y validar correos; los nombres guardados conservan sus tildes.
    public static string SinTildes(string? texto)
    {
        if (string.IsNullOrEmpty(texto)) return string.Empty;

        var descompuesto = texto.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(descompuesto.Length);
        foreach (var c in descompuesto)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                sb.Append(c);
        }
        return sb.ToString().Normalize(NormalizationForm.FormC).ToLowerInvariant();
    }
}
