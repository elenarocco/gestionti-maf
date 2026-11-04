using MafTi.Application;
using static MafTi.Application.SolicitudValores;

namespace MafTi.Tests;

public class CalculadoraSostenibilidadTests
{
    private static CalculadoraSostenibilidad.Parametros Iniciales() => new(2, 2, 2, 2, 1, 4.7m, 823m, 0.6m);

    [Fact]
    public void Calcula_hojas_y_CO2e_con_los_valores_iniciales()
    {
        var cantidades = new Dictionary<string, int> { [TipoIngreso] = 10, [TipoBloqueo] = 5 };

        var r = CalculadoraSostenibilidad.Calcular(cantidades, Iniciales());

        Assert.Equal(15, r.TotalSolicitudes);
        Assert.Equal(30, r.HojasEvitadas);                  // 15 × 2 hojas × 1 copia
        Assert.Equal(141.0m, r.Co2PapelEvitadoGramos);      // 30 × 4,7
        Assert.Equal(7.4m, r.Co2SistemaGramos);             // 15 × 0,823 MB × 0,6 = 7,407
        Assert.Equal(133.6m, r.AhorroNetoGramos);           // 141 − 7,407 = 133,593
    }

    [Fact]
    public void Siempre_devuelve_los_cuatro_tipos_en_orden_aunque_no_haya_solicitudes()
    {
        var r = CalculadoraSostenibilidad.Calcular(new Dictionary<string, int>(), Iniciales());

        Assert.Equal(new[] { TipoIngreso, TipoModificacion, TipoBloqueo, TipoVpn }, r.PorTipo.Select(t => t.Tipo));
        Assert.All(r.PorTipo, t => Assert.Equal(0, t.Cantidad));
        Assert.Equal(0m, r.AhorroNetoGramos);
    }

    [Fact]
    public void Usa_las_hojas_de_cada_tipo_y_las_copias_impresas()
    {
        var p = Iniciales() with { HojasIngreso = 3, HojasVpn = 1, CopiasImpresas = 2 };
        var cantidades = new Dictionary<string, int> { [TipoIngreso] = 4, [TipoVpn] = 2 };

        var r = CalculadoraSostenibilidad.Calcular(cantidades, p);

        Assert.Equal(24, r.PorTipo.Single(t => t.Tipo == TipoIngreso).HojasEvitadas);   // 4 × 3 × 2
        Assert.Equal(4, r.PorTipo.Single(t => t.Tipo == TipoVpn).HojasEvitadas);        // 2 × 1 × 2
        Assert.Equal(28, r.HojasEvitadas);
    }

    [Fact]
    public void El_ahorro_neto_puede_ser_negativo_si_el_sistema_emite_mas_que_el_papel()
    {
        var p = Iniciales() with { HojasIngreso = 0, KbTransferidosPorSolicitud = 5000m };

        var r = CalculadoraSostenibilidad.Calcular(new Dictionary<string, int> { [TipoIngreso] = 1 }, p);

        Assert.True(r.AhorroNetoGramos < 0);
    }

    [Fact]
    public void Los_valores_iniciales_son_validos()
    {
        Assert.Empty(CalculadoraSostenibilidad.Validar(Iniciales()));
    }

    [Fact]
    public void Rechaza_valores_fuera_de_rango_con_un_error_por_campo()
    {
        var p = Iniciales() with { HojasBloqueo = -1, CopiasImpresas = 0, GramosCO2PorHoja = 0m, KbTransferidosPorSolicitud = -5m, GramosCO2PorMB = 50m };

        var errores = CalculadoraSostenibilidad.Validar(p);

        Assert.Equal(new[] { "hojasBloqueo", "copiasImpresas", "gramosCO2PorHoja", "kbTransferidosPorSolicitud", "gramosCO2PorMB" }, errores.Keys);
    }

    [Fact]
    public void El_mes_tipico_de_ejemplo_con_1_hoja_por_formulario_da_27_hojas()
    {
        var p = Iniciales() with { HojasIngreso = 1, HojasModificacion = 1, HojasBloqueo = 1, HojasVpn = 1 };

        var r = CalculadoraSostenibilidad.Calcular(CalculadoraSostenibilidad.CantidadesEjemplo, p);

        Assert.Equal(27, r.TotalSolicitudes);          // 7 + 15 + 3 + 2
        Assert.Equal(27, r.HojasEvitadas);
        Assert.Equal(126.9m, r.Co2PapelEvitadoGramos);   // 27 × 4,7
        Assert.Equal(13.3m, r.Co2SistemaGramos);         // 27 × 0,823 × 0,6 = 13,33
        Assert.Equal(113.6m, r.AhorroNetoGramos);
    }
}
