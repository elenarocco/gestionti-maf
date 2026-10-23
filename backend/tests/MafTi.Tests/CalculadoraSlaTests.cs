using MafTi.Application;
using static MafTi.Application.SolicitudValores;

namespace MafTi.Tests;

// SLA por tipo, en hora de Chile y solo en días hábiles (lunes a viernes sin feriados).
// Fechas de referencia (2026): viernes 23-10, sábado 24-10, lunes 26-10, lunes 07-12, martes 08-12 feriado.
public class CalculadoraSlaTests
{
    private static readonly HashSet<DateOnly> Feriados = [new(2026, 12, 8), new(2026, 12, 25)];

    private static DateTime Utc(int mes, int dia, int hora, int minuto = 0) =>
        TimeZoneInfo.ConvertTimeToUtc(new DateTime(2026, mes, dia, hora, minuto, 0, DateTimeKind.Unspecified), HoraChile.Zona());

    private static DateTime Local(DateTime utc) => TimeZoneInfo.ConvertTimeFromUtc(utc, HoraChile.Zona());

    private static DateTime Vence(string tipo, DateTime creacionUtc) => Local(CalculadoraSla.Calcular(tipo, creacionUtc, Feriados).VenceUtc);

    [Fact]
    public void Ingreso_antes_de_las_12_cuenta_desde_ese_dia()
    {
        Assert.Equal(new DateTime(2026, 10, 28, 23, 59, 59), Vence(TipoIngreso, Utc(10, 26, 10)));   // lun → mié
    }

    [Fact]
    public void Ingreso_despues_de_las_12_cuenta_desde_el_dia_habil_siguiente()
    {
        Assert.Equal(new DateTime(2026, 10, 29, 23, 59, 59), Vence(TipoIngreso, Utc(10, 26, 14)));   // lun 14:00 → jue
        Assert.Equal(new DateTime(2026, 10, 29, 23, 59, 59), Vence(TipoIngreso, Utc(10, 26, 12)));   // a las 12:00 en punto ya es "después"
    }

    [Fact]
    public void Ingreso_salta_fines_de_semana_y_feriados()
    {
        // Lunes 07-12 a las 15:00: martes 08 es feriado → cuenta mié 09, jue 10 y vie 11.
        Assert.Equal(new DateTime(2026, 12, 11, 23, 59, 59), Vence(TipoIngreso, Utc(12, 7, 15)));
        // Viernes 23-10 a las 15:00 → lun 26, mar 27, mié 28.
        Assert.Equal(new DateTime(2026, 10, 28, 23, 59, 59), Vence(TipoIngreso, Utc(10, 23, 15)));
    }

    [Fact]
    public void Bloqueo_vence_el_mismo_dia_habil()
    {
        Assert.Equal(new DateTime(2026, 10, 23, 23, 59, 59), Vence(TipoBloqueo, Utc(10, 23, 18)));   // viernes 18:00
    }

    [Fact]
    public void Bloqueo_en_fin_de_semana_o_feriado_vence_el_siguiente_dia_habil()
    {
        Assert.Equal(new DateTime(2026, 10, 26, 23, 59, 59), Vence(TipoBloqueo, Utc(10, 24, 10)));   // sábado → lunes
        Assert.Equal(new DateTime(2026, 12, 9, 23, 59, 59), Vence(TipoBloqueo, Utc(12, 8, 10)));     // feriado → miércoles
    }

    [Theory]
    [InlineData(TipoModificacion)]
    [InlineData(TipoVpn)]
    public void Modificacion_y_VPN_48_horas_habiles(string tipo)
    {
        Assert.Equal(new DateTime(2026, 10, 27, 15, 0, 0), Vence(tipo, Utc(10, 23, 15)));   // vie 15:00 → mar 15:00
        Assert.Equal(new DateTime(2026, 10, 28, 9, 30, 0), Vence(tipo, Utc(10, 26, 9, 30))); // lun 09:30 → mié 09:30
        Assert.Equal(new DateTime(2026, 12, 10, 15, 0, 0), Vence(tipo, Utc(12, 7, 15)));     // lun 15:00, mar feriado → jue 15:00
    }

    [Fact]
    public void Modificacion_creada_en_fin_de_semana_parte_el_lunes_a_las_00()
    {
        var p = CalculadoraSla.Calcular(TipoModificacion, Utc(10, 24, 10), Feriados);          // sábado
        Assert.Equal(new DateTime(2026, 10, 26, 0, 0, 0), Local(p.InicioUtc));
        Assert.Equal(new DateTime(2026, 10, 27, 23, 59, 59), Local(p.VenceUtc));               // lun 00:00 + 48 h
    }

    [Fact]
    public void El_mensaje_dice_desde_cuando_cuenta_y_cuando_vence()
    {
        var p = CalculadoraSla.Calcular(TipoIngreso, Utc(10, 26, 14), Feriados);
        Assert.Contains("3 días hábiles", p.Mensaje);
        Assert.Contains("27-10-2026", p.Mensaje);   // empieza el martes
        Assert.Contains("29-10-2026", p.Mensaje);   // vence el jueves
    }
}
