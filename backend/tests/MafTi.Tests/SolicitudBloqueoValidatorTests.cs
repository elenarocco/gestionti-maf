using MafTi.Application;

namespace MafTi.Tests;

public class SolicitudBloqueoValidatorTests
{
    // Octubre: Chile está en UTC-3.
    private static readonly DateTime SabadoMediodia = new(2026, 10, 10, 15, 0, 0, DateTimeKind.Utc);   // sáb 12:00 Chile
    private static readonly DateTime MartesMediodia = new(2026, 10, 13, 15, 0, 0, DateTimeKind.Utc);   // mar 12:00 Chile
    private static readonly DateTime ViernesNocheChile = new(2026, 10, 10, 2, 30, 0, DateTimeKind.Utc); // vie 23:30 Chile, ya sábado en UTC

    private const string FinDeSemana = "Las solicitudes de bloqueo solo se pueden enviar de lunes a viernes.";

    // CP-15
    [Fact]
    public void CP15_no_permite_crear_bloqueos_en_fin_de_semana()
    {
        Assert.Contains(FinDeSemana, SolicitudBloqueoValidator.Validar(false, null, null, "Termino de contrato", SabadoMediodia));
    }

    [Fact]
    public void Dia_habil_se_permite()
    {
        Assert.Empty(SolicitudBloqueoValidator.Validar(false, null, null, "Termino de contrato", MartesMediodia));
    }

    [Fact]
    public void El_dia_se_evalua_en_hora_de_Chile_y_no_en_UTC()
    {
        Assert.DoesNotContain(FinDeSemana, SolicitudBloqueoValidator.Validar(false, null, null, "Termino de contrato", ViernesNocheChile));
    }

    // CP-14
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void CP14_justificacion_obligatoria(string justificacion)
    {
        Assert.Contains("La justificación es obligatoria.", SolicitudBloqueoValidator.Validar(false, null, null, justificacion, MartesMediodia));
    }

    [Fact]
    public void Temporal_exige_fecha_de_inicio_y_termino()
    {
        Assert.Contains("El bloqueo temporal requiere fecha y hora de inicio y de término.",
            SolicitudBloqueoValidator.Validar(true, MartesMediodia.AddHours(1), null, "Vacaciones", MartesMediodia));
    }

    [Fact]
    public void Temporal_con_termino_antes_del_inicio()
    {
        Assert.Contains("La fecha de término debe ser posterior a la de inicio.",
            SolicitudBloqueoValidator.Validar(true, MartesMediodia.AddDays(2), MartesMediodia.AddDays(1), "Vacaciones", MartesMediodia));
    }

    [Fact]
    public void Temporal_con_inicio_en_el_pasado()
    {
        Assert.Contains("La fecha de inicio no puede estar en el pasado.",
            SolicitudBloqueoValidator.Validar(true, MartesMediodia.AddDays(-1), MartesMediodia.AddDays(1), "Vacaciones", MartesMediodia));
    }
}
