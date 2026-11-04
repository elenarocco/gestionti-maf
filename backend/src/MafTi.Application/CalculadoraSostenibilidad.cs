using static MafTi.Application.SolicitudValores;

namespace MafTi.Application;

// Módulo Sostenibilidad (computación verde): compara el papel que se evita al digitalizar los
// formularios con la huella estimada del propio sistema. Todo el cálculo vive aquí; el frontend
// solo muestra el resultado. Son ESTIMACIONES con supuestos editables, no mediciones.
public static class CalculadoraSostenibilidad
{
    public const decimal KbPorMb = 1000m;   // Sustainable Web Design usa unidades decimales (1 GB = 1.000 MB)

    public record Parametros(
        int HojasIngreso, int HojasModificacion, int HojasBloqueo, int HojasVpn,
        int CopiasImpresas, decimal GramosCO2PorHoja, decimal KbTransferidosPorSolicitud, decimal GramosCO2PorMB);

    public record ResultadoTipo(string Tipo, int Cantidad, int HojasEvitadas, decimal Co2PapelEvitadoGramos);

    public record Resultado(
        IReadOnlyList<ResultadoTipo> PorTipo, int TotalSolicitudes, int HojasEvitadas,
        decimal Co2PapelEvitadoGramos, decimal Co2SistemaGramos, decimal AhorroNetoGramos);

    // Orden fijo para la pantalla y el gráfico (tipos tal como se guardan en la BD).
    public static readonly string[] Tipos = { TipoIngreso, TipoModificacion, TipoBloqueo, TipoVpn };

    // Cantidades ficticias para el modo "datos de ejemplo" (solo para probar la pantalla).
    public static readonly IReadOnlyDictionary<string, int> CantidadesEjemplo = new Dictionary<string, int>
    {
        { TipoIngreso, 14 }, { TipoModificacion, 9 }, { TipoBloqueo, 6 }, { TipoVpn, 3 }
    };

    // Fórmulas tal como se aplican, para mostrarlas en el bloque "Supuestos y fuentes".
    public static readonly string[] Formulas =
    {
        "Hojas evitadas = Σ (solicitudes completadas del tipo × hojas del formulario del tipo × copias impresas)",
        "CO2e del papel evitado = hojas evitadas × g CO2e por hoja",
        "CO2e del sistema = solicitudes completadas × (KB transferidos por solicitud ÷ 1.000) × g CO2e por MB",
        "Ahorro neto = CO2e del papel evitado − CO2e del sistema"
    };

    // De dónde sale cada supuesto, para mostrarlo en pantalla. Mantener alineado con los comentarios
    // de la migración AgregarSostenibilidad cuando se ajusten los valores.
    public static readonly (string Parametro, string Descripcion)[] Fuentes =
    {
        ("Hojas por formulario", "1 hoja por formulario en los cuatro tipos: confirmado por la jefatura; cada formulario en papel era una sola hoja."),
        ("Copias impresas", "1 copia: el mismo formulario circulaba en mano para reunir las firmas de las jefaturas."),
        ("g CO2e por hoja", "Valor inicial 4,7 g CO2e por hoja A4, estimación de ciclo de vida del papel (fabricación y desecho). Pendiente de confirmar la fuente."),
        ("KB transferidos por solicitud", "Medido el 04-11-2026: 823 KB para el flujo completo de una solicitud (RRHH inicia sesión y la crea; Admin TI inicia sesión, la revisa en la Bandeja de TI, la aprueba y la completa), con caché vacía. No incluye el inicio de sesión de Microsoft."),
        ("g CO2e por MB", "Valor inicial 0,6 g CO2e por MB transferido, basado en el modelo Sustainable Web Design (energía de red, centro de datos y dispositivos por dato transferido)."),
    };

    public static int HojasPorFormulario(Parametros p, string tipo) => tipo switch
    {
        TipoIngreso => p.HojasIngreso,
        TipoModificacion => p.HojasModificacion,
        TipoBloqueo => p.HojasBloqueo,
        TipoVpn => p.HojasVpn,
        _ => 0
    };

    // cantidades: solicitudes completadas por tipo (tipo de BD). Los tipos que falten cuentan 0.
    public static Resultado Calcular(IReadOnlyDictionary<string, int> cantidades, Parametros p)
    {
        var porTipo = Tipos.Select(tipo =>
        {
            var cantidad = cantidades.GetValueOrDefault(tipo);
            var hojas = cantidad * HojasPorFormulario(p, tipo) * p.CopiasImpresas;
            return new ResultadoTipo(tipo, cantidad, hojas, Redondear(hojas * p.GramosCO2PorHoja));
        }).ToList();

        var total = porTipo.Sum(t => t.Cantidad);
        var hojasEvitadas = porTipo.Sum(t => t.HojasEvitadas);
        var co2Papel = hojasEvitadas * p.GramosCO2PorHoja;
        var co2Sistema = total * (p.KbTransferidosPorSolicitud / KbPorMb) * p.GramosCO2PorMB;

        return new Resultado(porTipo, total, hojasEvitadas,
            Redondear(co2Papel), Redondear(co2Sistema), Redondear(co2Papel - co2Sistema));
    }

    // Errores por campo (clave en camelCase, como en el JSON), un mensaje por campo.
    public static Dictionary<string, string> Validar(Parametros p)
    {
        var errores = new Dictionary<string, string>();

        Hojas(errores, "hojasIngreso", p.HojasIngreso, "Ingreso");
        Hojas(errores, "hojasModificacion", p.HojasModificacion, "Modificación");
        Hojas(errores, "hojasBloqueo", p.HojasBloqueo, "Bloqueo");
        Hojas(errores, "hojasVpn", p.HojasVpn, "VPN");

        if (p.CopiasImpresas < 1 || p.CopiasImpresas > 10)
            errores["copiasImpresas"] = "Las copias impresas deben estar entre 1 y 10.";
        if (p.GramosCO2PorHoja <= 0 || p.GramosCO2PorHoja > 100)
            errores["gramosCO2PorHoja"] = "Los g CO2e por hoja deben ser mayores que 0 y como máximo 100.";
        if (p.KbTransferidosPorSolicitud <= 0 || p.KbTransferidosPorSolicitud > 1_000_000)
            errores["kbTransferidosPorSolicitud"] = "Los KB transferidos por solicitud deben ser mayores que 0 y como máximo 1.000.000.";
        if (p.GramosCO2PorMB <= 0 || p.GramosCO2PorMB > 10)
            errores["gramosCO2PorMB"] = "Los g CO2e por MB deben ser mayores que 0 y como máximo 10.";

        return errores;
    }

    private static void Hojas(Dictionary<string, string> errores, string campo, int valor, string tipo)
    {
        if (valor < 0 || valor > 50)
            errores[campo] = $"Las hojas del formulario de {tipo} deben estar entre 0 y 50.";
    }

    private static decimal Redondear(decimal gramos) => Math.Round(gramos, 1, MidpointRounding.AwayFromZero);
}
