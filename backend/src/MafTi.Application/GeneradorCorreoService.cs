namespace MafTi.Application;

public static class GeneradorCorreoService
{
    public static async Task<string?> Generar(string PrimerNombre, string PrimerApellido, Func<string, Task<bool>> correoYaExisteAsync)
    {
        var primeraLetra = PrimerNombre.Trim().Substring(0, 1).ToLower();
        var apellido = PrimerApellido.Trim().ToLower().Replace(" ","");
        var sugerido = $"{primeraLetra}{apellido}@mafchile.com";

        var existe = await correoYaExisteAsync(sugerido);
        return existe ? null : sugerido;
    }

}