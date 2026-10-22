using MafTi.Application;

namespace MafTi.Tests;

public class SolicitudVpnValidatorTests
{
    private static SolicitudVpnValidator.Ficha FichaValida() => new(
        new DateOnly(2026, 11, 1), new DateOnly(2027, 1, 31), "12.345.678-5",
        "Ingeniero de soporte", "María José", "Núñez", "Redes", "Oficina Las Condes",
        false, "Soporte Externo Ltda", "mnunez@soporteexterno.cl", "Soporte remoto a switches, red corporativa.");

    [Fact]
    public void Una_ficha_completa_y_correcta_no_tiene_errores()
    {
        Assert.Empty(SolicitudVpnValidator.ValidarFicha(FichaValida()));
    }

    [Fact]
    public void La_caducidad_debe_ser_posterior_a_la_incorporacion()
    {
        var errores = SolicitudVpnValidator.ValidarFicha(FichaValida() with { FechaCaducidad = new DateOnly(2026, 11, 1) });
        Assert.True(errores.ContainsKey("fechaCaducidad"));
    }

    [Fact]
    public void El_RUT_se_valida_con_el_digito_verificador()
    {
        var errores = SolicitudVpnValidator.ValidarFicha(FichaValida() with { Rut = "12.345.678-0" });
        Assert.Equal("El dígito verificador del RUT no es correcto.", errores["rut"]);
    }

    [Fact]
    public void Compania_externa_es_obligatoria_solo_si_el_equipo_no_es_de_la_compania()
    {
        Assert.True(SolicitudVpnValidator.ValidarFicha(FichaValida() with { CompaniaExterna = " " }).ContainsKey("companiaExterna"));
        Assert.Empty(SolicitudVpnValidator.ValidarFicha(FichaValida() with { EquipoCorporativo = true, CompaniaExterna = null }));
    }

    [Theory]
    [InlineData("no-es-correo")]
    [InlineData("sin@dominio")]
    public void La_cuenta_de_correo_debe_tener_formato_valido(string correo)
    {
        Assert.True(SolicitudVpnValidator.ValidarFicha(FichaValida() with { CuentaCorreo = correo }).ContainsKey("cuentaCorreo"));
    }

    [Fact]
    public void El_motivo_tiene_maximo_200_caracteres_y_solo_letras_espacios_comas_y_puntos()
    {
        Assert.True(SolicitudVpnValidator.ValidarFicha(FichaValida() with { Motivo = new string('a', 201) }).ContainsKey("motivo"));
        Assert.True(SolicitudVpnValidator.ValidarFicha(FichaValida() with { Motivo = "Soporte 24x7" }).ContainsKey("motivo"));
    }

    [Theory]
    [InlineData(null, "La justificación del rechazo es obligatoria")]
    [InlineData("   ", "La justificación del rechazo es obligatoria")]
    public void La_justificacion_del_rechazo_es_obligatoria(string? justificacion, string esperado)
    {
        Assert.Equal(esperado, SolicitudVpnValidator.ValidarJustificacionRechazo(justificacion));
    }

    [Fact]
    public void La_justificacion_del_rechazo_admite_hasta_500_caracteres()
    {
        Assert.Null(SolicitudVpnValidator.ValidarJustificacionRechazo(new string('a', 500)));
        Assert.NotNull(SolicitudVpnValidator.ValidarJustificacionRechazo(new string('a', 501)));
    }
}
