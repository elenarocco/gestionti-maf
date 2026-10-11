using MafTi.Application;
using static MafTi.Application.TransicionSolicitud;

namespace MafTi.Tests;

public class TransicionSolicitudTests
{
    // CP-25
    [Theory]
    [InlineData(Pendiente)]
    [InlineData(Completada)]
    [InlineData(Rechazada)]
    public void CP25_solo_se_completa_desde_En_proceso(string estado)
    {
        Assert.NotNull(ValidarCompletar(estado));
    }

    // CP-26
    [Fact]
    public void CP26_En_proceso_se_puede_completar()
    {
        Assert.Null(ValidarCompletar(EnProceso));
    }

    // CP-27 y CP-18
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void CP27_rechazar_exige_motivo(string? motivo)
    {
        Assert.Equal("El motivo de rechazo es obligatorio.", ValidarRechazar(Pendiente, motivo));
    }

    [Theory]
    [InlineData(Pendiente)]
    [InlineData(EnProceso)]
    public void Se_rechaza_desde_Pendiente_o_En_proceso(string estado)
    {
        Assert.Null(ValidarRechazar(estado, "Faltan datos"));
    }

    // CP-19: Rechazada es final
    [Theory]
    [InlineData(Completada)]
    [InlineData(Rechazada)]
    public void No_se_rechaza_una_solicitud_cerrada(string estado)
    {
        Assert.NotNull(ValidarRechazar(estado, "Faltan datos"));
    }

    [Fact]
    public void Motivo_demasiado_largo()
    {
        Assert.NotNull(ValidarRechazar(Pendiente, new string('a', LargoMaximoMotivo + 1)));
    }

    // CP-17: RRHH aprueba el Bloqueo
    [Fact]
    public void CP17_Bloqueo_Pendiente_se_aprueba()
    {
        Assert.Null(ValidarAprobar("Bloqueo", Pendiente));
    }

    [Theory]
    [InlineData("Ingreso", Pendiente)]
    [InlineData("Bloqueo", EnProceso)]
    public void Aprobar_solo_aplica_a_Bloqueos_Pendientes(string tipo, string estado)
    {
        Assert.NotNull(ValidarAprobar(tipo, estado));
    }

    [Theory]
    [InlineData("Ingreso")]
    [InlineData("Modificación")]
    public void Ingreso_y_Modificacion_Pendientes_se_inician(string tipo)
    {
        Assert.Null(ValidarIniciar(tipo, Pendiente));
    }

    [Theory]
    [InlineData("Bloqueo", Pendiente)]   // el Bloqueo se aprueba, no se inicia
    [InlineData("VPN", Pendiente)]       // VPN aún sin flujo
    [InlineData("Ingreso", EnProceso)]
    public void Iniciar_no_aplica(string tipo, string estado)
    {
        Assert.NotNull(ValidarIniciar(tipo, estado));
    }
}
