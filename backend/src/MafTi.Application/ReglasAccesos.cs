namespace MafTi.Application;

// Qué accesos (sistemas y carpetas) se agregan o quitan al completar un Ingreso o una Modificación.
// Solo se agrega lo que el trabajador no tiene activo y solo se quita lo que sí tiene: así completar dos veces
// la misma lista, o pedir algo que ya tiene, no duplica ni falla.
public static class ReglasAccesos
{
    public record Cambios(IReadOnlyList<int> Agregar, IReadOnlyList<int> Quitar);

    public static Cambios Calcular(IEnumerable<int> activos, IEnumerable<int> pedidosAgregar, IEnumerable<int> pedidosQuitar)
    {
        var actuales = activos.ToHashSet();
        var agregar = pedidosAgregar.Distinct().Where(id => !actuales.Contains(id)).ToList();
        var quitar = pedidosQuitar.Distinct().Where(actuales.Contains).Except(agregar).ToList();
        return new Cambios(agregar, quitar);
    }
}
