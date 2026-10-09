using System.Text.RegularExpressions;

namespace MafTi.Application;

public static class CatalogoValidator
{
    // Para sumar otros catálogos a la pantalla en el futuro, se agregan aquí
    public static readonly string[] TiposAdministrables = { "Sistema", "Carpeta" };

    public static string Normalizar(string? nombre) =>
        Regex.Replace((nombre ?? "").Trim(), @"\s+", " ");

    public static async Task<List<string>> ValidarAsync(
        string tipo, string nombreNormalizado, Func<string, Task<bool>> nombreYaExisteAsync)
    {
        var errores = new List<string>();

        if (!TiposAdministrables.Contains(tipo))
        {
            errores.Add("Solo se pueden administrar sistemas y carpetas de red.");
            return errores;
        }

        if (nombreNormalizado.Length == 0)
        {
            errores.Add("El nombre es obligatorio.");
            return errores;
        }

        if (nombreNormalizado.Length > 150)
            errores.Add("El nombre no puede tener más de 150 caracteres.");

        if (tipo == "Carpeta" && (!nombreNormalizado.StartsWith(@"\\") || nombreNormalizado.Length <= 2))
            errores.Add(@"La ruta debe comenzar con \\ (ej. \\servidor\carpeta).");

        if (await nombreYaExisteAsync(nombreNormalizado))
            errores.Add("Ya existe un elemento con ese nombre.");

        return errores;
    }
}