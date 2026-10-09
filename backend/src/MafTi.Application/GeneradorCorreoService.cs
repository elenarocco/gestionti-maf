namespace MafTi.Application;

public class SugerenciaCorreo
{
    public string? Correo { get; set; }
    public bool Disponible { get; set; }
    public bool EsReincorporacion { get; set; }
}

public static class GeneradorCorreoService
{
    public static async Task<SugerenciaCorreo> Generar(
        string rut,
        string primerNombre,
        string primerApellido,
        string? segundoApellido,
        Func<string, Task<string?>> buscarCorreoDeInactivoPorRutAsync,
        Func<string, Task<bool>> correoYaExisteAsync)
    {
        // Excepción por reincorporación: si este RUT ya existió (inactivo), se reutiliza su correo anterior
        // Excepción por reincorporación: solo se revisa si se informó un RUT
        if (!string.IsNullOrWhiteSpace(rut))
        {
            var correoAnterior = await buscarCorreoDeInactivoPorRutAsync(rut);
            if (correoAnterior != null)
            {
                return new SugerenciaCorreo { Correo = correoAnterior, Disponible = true, EsReincorporacion = true };
            }
        }

        var primeraLetra = NormalizadorTexto.SinTildes(primerNombre.Trim().Substring(0, 1));
        var apellidoPaterno = NormalizadorTexto.SinTildes(primerApellido.Trim()).Replace(" ", "");

        // Nivel 1: primera letra + apellido paterno completo
        var nivel1 = $"{primeraLetra}{apellidoPaterno}@mafchile.com";
        if (!await correoYaExisteAsync(nivel1))
        {
            return new SugerenciaCorreo { Correo = nivel1, Disponible = true };
        }

        // Nivel 2: + primera letra del apellido materno, si existe
        if (!string.IsNullOrWhiteSpace(segundoApellido))
        {
            var letraMaterno = NormalizadorTexto.SinTildes(segundoApellido.Trim().Substring(0, 1));
            var nivel2 = $"{primeraLetra}{apellidoPaterno}{letraMaterno}@mafchile.com";
            if (!await correoYaExisteAsync(nivel2))
            {
                return new SugerenciaCorreo { Correo = nivel2, Disponible = true };
            }
        }

        // Nivel 3 (nombre completo / 3 letras + apellido): queda pendiente de definir.
        // No se auto-genera; se entrega al usuario para edición manual.
        return new SugerenciaCorreo { Correo = null, Disponible = false };
    }
}