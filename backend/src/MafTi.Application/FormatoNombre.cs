using System.Text.RegularExpressions;

namespace MafTi.Application;

public static class FormatoNombre
{
    private static readonly HashSet<string> Particulas = new() { "de", "del", "la", "las", "los", "y", "e" };

    public static string? Normalizar(string? valor)
    {
        if (string.IsNullOrWhiteSpace(valor)) return valor;

        var limpio = Regex.Replace(valor.Trim().ToLowerInvariant(), @"\s+", " ");
        var primera = true;

        var resultado = Regex.Replace(limpio, @"\S+", m =>
        {
            var palabra = m.Value;
            var esPrimera = primera;
            primera = false;
            if (!esPrimera && Particulas.Contains(palabra)) return palabra;
            return char.ToUpperInvariant(palabra[0]) + palabra.Substring(1);
        });

        return Regex.Replace(resultado, @"(['\-])(\p{L})",
            m => m.Groups[1].Value + m.Groups[2].Value.ToUpperInvariant());
    }
}