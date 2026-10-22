using MafTi.Application;
using MafTi.Domain;

namespace MafTi.Tests;

public class TrabajadorValidatorTests
{
    private static Trabajador Valido() => new()
    {
        Rut = "12345678-5",
        PrimerNombre = "Juan",
        PrimerApellido = "Pérez",
        FechaNacimiento = new DateOnly(1990, 1, 1),
        Correo = "jperez@mafchile.com",
        DireccionCorporativaId = 1,
        AreaId = 20
    };

    // Por defecto: correo libre, RUT sin trabajador activo y área perteneciente a la dirección.
    private static Task<List<string>> Validar(Trabajador t,
        bool correoExiste = false, bool rutExiste = false, bool areaCalza = true) =>
        TrabajadorValidator.ValidarAsync(t,
            _ => Task.FromResult(correoExiste),
            _ => Task.FromResult(rutExiste),
            (_, _) => Task.FromResult(areaCalza));

    [Fact]
    public async Task Trabajador_valido_no_tiene_errores()
    {
        Assert.Empty(await Validar(Valido()));
    }

    // CP-02
    [Theory]
    [InlineData("12345678-9")]
    [InlineData("12.345.678-0")]
    public async Task CP02_rut_con_digito_verificador_incorrecto(string rut)
    {
        var t = Valido();
        t.Rut = rut;
        Assert.Contains("El dígito verificador del RUT no es correcto.", await Validar(t));
    }

    // CP-03: el formato con puntos y la K minúscula se aceptan
    [Theory]
    [InlineData("12.345.678-5")]
    [InlineData("11223344-k")]
    public async Task CP03_rut_valido_en_distintos_formatos(string rut)
    {
        var t = Valido();
        t.Rut = rut;
        Assert.DoesNotContain(await Validar(t), e => e.Contains("RUT"));
    }

    [Fact]
    public void Rut_se_guarda_sin_puntos_con_guion_y_K_mayuscula()
    {
        Assert.Equal("11223344-K", TrabajadorValidator.FormatearParaGuardar("11.223.344-k"));
    }

    [Fact]
    public async Task Rut_de_trabajador_activo_existente_se_rechaza()
    {
        Assert.Contains("Ya existe un trabajador activo con ese RUT.", await Validar(Valido(), rutExiste: true));
    }

    // CP-04
    [Fact]
    public async Task CP04_fecha_de_nacimiento_futura()
    {
        var t = Valido();
        t.FechaNacimiento = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(1);
        Assert.Contains("La fecha de nacimiento no puede ser futura.", await Validar(t));
    }

    // CP-05
    [Theory]
    [InlineData(17)]
    [InlineData(76)]
    public async Task CP05_edad_fuera_de_18_a_75(int edad)
    {
        var t = Valido();
        t.FechaNacimiento = DateOnly.FromDateTime(DateTime.UtcNow).AddYears(-edad);
        Assert.Contains("La edad debe estar entre 18 y 75 años.", await Validar(t));
    }

    [Theory]
    [InlineData(18)]
    [InlineData(75)]
    public async Task Edad_en_los_limites_se_acepta(int edad)
    {
        var t = Valido();
        t.FechaNacimiento = DateOnly.FromDateTime(DateTime.UtcNow).AddYears(-edad);
        Assert.DoesNotContain("La edad debe estar entre 18 y 75 años.", await Validar(t));
    }

    [Fact]
    public async Task Area_que_no_pertenece_a_la_direccion()
    {
        Assert.Contains("El área seleccionada no pertenece a la dirección corporativa elegida.",
            await Validar(Valido(), areaCalza: false));
    }

    [Theory]
    [InlineData("jperez@gmail.com", "El correo debe ser del dominio institucional (@mafchile.com).")]
    [InlineData("jperez2@mafchile.com", "El correo solo puede contener letras, sin números ni símbolos.")]
    [InlineData("pjuan@mafchile.com", "El correo debe comenzar con la primera letra del nombre seguida del apellido paterno.")]
    public async Task Reglas_de_formato_del_correo(string correo, string error)
    {
        var t = Valido();
        t.Correo = correo;
        Assert.Contains(error, await Validar(t));
    }

    [Fact]
    public async Task Correo_ya_usado_se_rechaza()
    {
        Assert.Contains("Ese correo ya está en uso por otro trabajador.", await Validar(Valido(), correoExiste: true));
    }

    // D-10: antes lanzaba excepción (error 500)
    [Fact]
    public async Task Nombre_vacio_da_error_de_validacion_y_no_excepcion()
    {
        var t = Valido();
        t.PrimerNombre = "";
        Assert.Contains("El primer nombre es obligatorio.", await Validar(t));
    }
}
