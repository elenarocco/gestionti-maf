using MafTi.Application;

namespace MafTi.Tests;

public class ReglasAccesosTests
{
    [Fact]
    public void Agrega_solo_lo_que_no_tiene_y_quita_solo_lo_que_tiene()
    {
        var c = ReglasAccesos.Calcular(activos: [1, 2], pedidosAgregar: [2, 3], pedidosQuitar: [1, 9]);
        Assert.Equal([3], c.Agregar);
        Assert.Equal([1], c.Quitar);
    }

    [Fact]
    public void Un_trabajador_nuevo_recibe_todo_lo_pedido_sin_duplicados()
    {
        var c = ReglasAccesos.Calcular(activos: [], pedidosAgregar: [5, 5, 6], pedidosQuitar: []);
        Assert.Equal([5, 6], c.Agregar);
        Assert.Empty(c.Quitar);
    }

    [Fact]
    public void Aplicar_dos_veces_lo_mismo_no_cambia_nada()
    {
        var c = ReglasAccesos.Calcular(activos: [3], pedidosAgregar: [3], pedidosQuitar: [7]);
        Assert.Empty(c.Agregar);
        Assert.Empty(c.Quitar);
    }
}
