using MafTi.Domain;

namespace MafTi.Application;

public static class TrabajadorValidator
{
    public static List<string> Validar(Trabajador trabajador)
    {
        var errores = new List<string>();

        var rutLimpio = LimpiarRut(trabajador.Rut);

        if (rutLimpio.Length < 2)
        {
            errores.Add("El RUT es demasiado corto.");
        }
        else
        {
            var cuerpo = rutLimpio.Substring(0, rutLimpio.Length - 1);
            var dv = rutLimpio.Substring(rutLimpio.Length - 1, 1);

            if (cuerpo.Length > 8)
            {
                errores.Add("El RUT no puede tener más de 8 dígitos en el cuerpo.");
            }
            else if (!cuerpo.All(char.IsDigit))
            {
                errores.Add("El RUT solo puede contener números (más el dígito verificador).");
            }
            else if (!EsDigitoVerificadorValido(cuerpo, dv))
            {
                errores.Add("El dígito verificador del RUT no es correcto.");
            }
        }

        var hoy = DateOnly.FromDateTime(DateTime.UtcNow);
        if (trabajador.FechaNacimiento > hoy)
        {
            errores.Add("La fecha de nacimiento no puede ser futura.");
        }
        else
        {
            var edad = hoy.Year - trabajador.FechaNacimiento.Year;
            if (trabajador.FechaNacimiento > hoy.AddYears(-edad)) edad--;

            if (edad < 18 || edad > 75)
            {
                errores.Add("La edad debe estar entre 18 y 75 años.");
            }
        }

        return errores;
    }

    public static string LimpiarRut(string rut)
    {
        return rut.Replace(".", "").Replace("-", "").Trim().ToUpper();
    }

    public static string FormatearParaGuardar(string rut)
    {
        var limpio = LimpiarRut(rut);
        if (limpio.Length < 2) return limpio;
        var cuerpo = limpio.Substring(0, limpio.Length - 1);
        var dv = limpio.Substring(limpio.Length - 1, 1);
        return $"{cuerpo}-{dv}";
    }

    private static bool EsDigitoVerificadorValido(string cuerpo, string dv)
    {
        int suma = 0;
        int multiplicador = 2;

        for (int i = cuerpo.Length - 1; i >= 0; i--)
        {
            suma += (cuerpo[i] - '0') * multiplicador;
            multiplicador++;
            if (multiplicador > 7) multiplicador = 2;
        }

        int resto = 11 - (suma % 11);
        string dvEsperado = resto switch
        {
            11 => "0",
            10 => "K",
            _ => resto.ToString()
        };

        return dv == dvEsperado;
    }
}