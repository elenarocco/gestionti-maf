using MafTi.Application;
using static MafTi.Application.FlujoSolicitud;
using static MafTi.Application.SolicitudValores;

namespace MafTi.Tests;

// Reglas de estado de la Bandeja de TI y del flujo VPN (equivalen a TransicionSolicitudTests de feature/ariel).
public class FlujoSolicitudTests
{
    private static List<string> Ti(string tipo, string estado, bool habilitado = false) =>
        AccionesTi(new Situacion(tipo, estado, habilitado));

    [Theory]
    [InlineData(TipoIngreso)]
    [InlineData(TipoModificacion)]
    public void Ingreso_y_Modificacion_pendientes_se_toman_o_se_rechazan(string tipo)
    {
        Assert.Equal(new[] { Tomar, Rechazar }, Ti(tipo, Pendiente));
    }

    [Fact]
    public void Bloqueo_pendiente_TI_solo_puede_rechazarlo_porque_lo_aprueba_RRHH()
    {
        Assert.Equal(new[] { Rechazar }, Ti(TipoBloqueo, Pendiente));
        Assert.True(PuedeAprobarRrhh(new Situacion(TipoBloqueo, Pendiente)));
        Assert.False(PuedeAprobarRrhh(new Situacion(TipoBloqueo, EnProceso)));
        Assert.False(PuedeAprobarRrhh(new Situacion(TipoIngreso, Pendiente)));
    }

    [Theory]
    [InlineData(TipoIngreso)]
    [InlineData(TipoModificacion)]
    [InlineData(TipoBloqueo)]
    public void En_proceso_se_completa_o_se_rechaza(string tipo)
    {
        Assert.Equal(new[] { Completar, Rechazar }, Ti(tipo, EnProceso));
    }

    [Theory]
    [InlineData(TipoIngreso, Completada)]
    [InlineData(TipoBloqueo, Rechazada)]
    [InlineData(TipoVpn, Completada)]
    [InlineData(TipoVpn, Rechazada)]
    public void Estados_finales_no_tienen_acciones(string tipo, string estado)
    {
        Assert.Empty(Ti(tipo, estado));
    }

    [Fact]
    public void Vpn_fase_1_aprueba_el_acceso_y_sigue_pendiente_con_la_ficha_habilitada()
    {
        var pendiente = new Situacion(TipoVpn, Pendiente);
        Assert.Equal(new[] { Tomar, Rechazar }, AccionesTi(pendiente));
        Assert.Null(EstadoTras(Tomar, pendiente));                 // no cambia de estado
        Assert.Equal("Acceso VPN aprobado", AccionHistorial(Tomar, pendiente));
    }

    [Fact]
    public void Vpn_con_ficha_habilitada_TI_ya_no_la_aprueba_otra_vez_pero_puede_rechazarla()
    {
        Assert.Equal(new[] { Rechazar }, Ti(TipoVpn, Pendiente, habilitado: true));
    }

    [Fact]
    public void Vpn_fase_3_confirma_el_envio_o_rechaza_la_ficha()
    {
        var enProceso = new Situacion(TipoVpn, EnProceso, true, true);
        Assert.Equal(new[] { ConfirmarEnvio, RechazarFicha }, AccionesTi(enProceso));
        Assert.Equal(Completada, EstadoTras(ConfirmarEnvio, enProceso));
        Assert.Equal(Rechazada, EstadoTras(RechazarFicha, enProceso));
    }

    [Fact]
    public void La_ficha_VPN_solo_se_completa_si_TI_la_habilito_y_sigue_pendiente()
    {
        Assert.True(PuedeCompletarFicha(new Situacion(TipoVpn, Pendiente, FormularioHabilitado: true)));
        Assert.False(PuedeCompletarFicha(new Situacion(TipoVpn, Pendiente, FormularioHabilitado: false)));
        Assert.False(PuedeCompletarFicha(new Situacion(TipoVpn, EnProceso, FormularioHabilitado: true)));
        Assert.False(PuedeCompletarFicha(new Situacion(TipoIngreso, Pendiente, FormularioHabilitado: true)));
    }

    [Fact]
    public void Tomar_un_Ingreso_lo_pasa_a_en_proceso_y_completarlo_lo_cierra()
    {
        var ingreso = new Situacion(TipoIngreso, Pendiente);
        Assert.Equal(EnProceso, EstadoTras(Tomar, ingreso));
        Assert.Equal(Completada, EstadoTras(Completar, ingreso with { Estado = EnProceso }));
        Assert.Equal(Rechazada, EstadoTras(Rechazar, ingreso));
    }

    [Theory]
    [InlineData("EnProceso", EnProceso)]
    [InlineData("Modificacion", TipoModificacion)]
    [InlineData("vpn", TipoVpn)]
    public void Los_valores_de_la_API_se_traducen_a_los_de_la_BD(string api, string db)
    {
        Assert.Equal(db, EstadoADb(api) ?? TipoADb(api));
    }

    [Fact]
    public void El_SLA_solo_vence_mientras_la_solicitud_esta_abierta()
    {
        var ahora = new DateTime(2026, 10, 20, 12, 0, 0, DateTimeKind.Utc);
        var ayer = ahora.AddDays(-1);
        Assert.True(EstaVencida(Pendiente, ayer, ahora));
        Assert.True(EstaVencida(EnProceso, ayer, ahora));
        Assert.False(EstaVencida(Completada, ayer, ahora));
        Assert.False(EstaVencida(Pendiente, ahora.AddDays(1), ahora));
    }
}
