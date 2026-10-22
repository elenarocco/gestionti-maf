using MafTi.Application;

namespace MafTi.Tests;

public class GeneradorCorreoServiceTests
{
    // Simula la base: correos ya usados y trabajadores inactivos (RUT -> correo anterior).
    private static Task<SugerenciaCorreo> Generar(string rut, string nombre, string apellido, string? materno,
        string[]? correosUsados = null, Dictionary<string, string>? inactivos = null)
    {
        var usados = correosUsados ?? Array.Empty<string>();
        var anteriores = inactivos ?? new Dictionary<string, string>();
        return GeneradorCorreoService.Generar(rut, nombre, apellido, materno,
            r => Task.FromResult(anteriores.GetValueOrDefault(r)),
            c => Task.FromResult(usados.Contains(c)));
    }

    // CP-06
    [Fact]
    public async Task CP06_sin_conflicto_sugiere_inicial_mas_apellido_paterno()
    {
        var r = await Generar("11111111-1", "Juan", "Pérez", "González");
        Assert.Equal("jperez@mafchile.com", r.Correo);
        Assert.True(r.Disponible);
    }

    // CP-07
    [Fact]
    public async Task CP07_si_existe_agrega_inicial_del_apellido_materno()
    {
        var r = await Generar("11111111-1", "Juan", "Pérez", "González", new[] { "jperez@mafchile.com" });
        Assert.Equal("jperezg@mafchile.com", r.Correo);
    }

    // CP-08
    [Theory]
    [InlineData(null)]          // sin apellido materno no hay segundo nivel
    [InlineData("González")]    // el segundo nivel también está usado
    public async Task CP08_sin_regla_que_resuelva_queda_para_edicion_manual(string? materno)
    {
        var r = await Generar("11111111-1", "Juan", "Pérez", materno,
            new[] { "jperez@mafchile.com", "jperezg@mafchile.com" });
        Assert.Null(r.Correo);
        Assert.False(r.Disponible);
    }

    // CP-09
    [Fact]
    public async Task CP09_otra_persona_con_el_mismo_nombre_no_hereda_el_correo()
    {
        var inactivos = new Dictionary<string, string> { ["18901234-9"] = "areyes@mafchile.com" };
        var r = await Generar("11111111-1", "Ana", "Reyes", "López", new[] { "areyes@mafchile.com" }, inactivos);
        Assert.Equal("areyesl@mafchile.com", r.Correo);
        Assert.False(r.EsReincorporacion);
    }

    [Fact]
    public async Task Reincorporacion_con_el_mismo_rut_reutiliza_el_correo_anterior()
    {
        var inactivos = new Dictionary<string, string> { ["18901234-9"] = "areyes@mafchile.com" };
        var r = await Generar("18901234-9", "Ana", "Reyes", "Díaz", new[] { "areyes@mafchile.com" }, inactivos);
        Assert.Equal("areyes@mafchile.com", r.Correo);
        Assert.True(r.EsReincorporacion);
    }

    [Theory]
    [InlineData("Ñandú", "Núñez", "nnunez@mafchile.com")]
    [InlineData("Óscar", "Del Río", "odelrio@mafchile.com")]
    public async Task Quita_tildes_enie_y_espacios(string nombre, string apellido, string esperado)
    {
        Assert.Equal(esperado, (await Generar("11111111-1", nombre, apellido, null)).Correo);
    }

    // D-10
    [Fact]
    public async Task Nombre_vacio_queda_para_edicion_manual_sin_excepcion()
    {
        var r = await Generar("11111111-1", "  ", "Pérez", null);
        Assert.Null(r.Correo);
    }
}
