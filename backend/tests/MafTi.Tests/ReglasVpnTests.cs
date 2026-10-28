using MafTi.Application;
using static MafTi.Application.ReglasVpn;

namespace MafTi.Tests;

public class ReglasVpnTests
{
    private static readonly DateOnly Hoy = new(2026, 10, 28);

    [Fact]
    public void Sin_fecha_de_termino_esta_vigente()
    {
        Assert.Equal(new Situacion(Vigente, null), Calcular(null, Hoy));
    }

    [Fact]
    public void Con_mas_de_15_dias_esta_vigente()
    {
        Assert.Equal(new Situacion(Vigente, 16), Calcular(Hoy.AddDays(16), Hoy));
    }

    [Fact]
    public void Con_15_dias_o_menos_esta_por_vencer_incluido_el_dia_que_vence()
    {
        Assert.Equal(new Situacion(PorVencer, 15), Calcular(Hoy.AddDays(15), Hoy));
        Assert.Equal(new Situacion(PorVencer, 0), Calcular(Hoy, Hoy));
    }

    [Fact]
    public void Si_la_fecha_ya_paso_esta_vencido()
    {
        Assert.Equal(new Situacion(Vencido, -1), Calcular(Hoy.AddDays(-1), Hoy));
    }
}
