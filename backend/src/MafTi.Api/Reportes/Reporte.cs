using MafTi.Application;

namespace MafTi.Api.Reportes;

// Un reporte es una tabla: el controlador decide QUÉ datos van (con los mismos filtros y permisos que la
// pantalla) y los generadores de Excel y PDF solo deciden CÓMO se ven.
public record ColumnaReporte(string Titulo, double Ancho = 1);   // Ancho relativo, para el PDF

public class Reporte
{
    // Máximo de filas por descarga: evita que una descarga gigante tumbe la API.
    public const int MaxFilas = 10_000;

    public required string Titulo { get; init; }
    public required string NombreArchivo { get; init; }   // sin extensión
    public required IReadOnlyList<ColumnaReporte> Columnas { get; init; }
    // Cada fila trae un valor por columna: string, int, DateTime (UTC), DateOnly o null.
    public required IReadOnlyList<object?[]> Filas { get; init; }
    public required string Filtros { get; init; }
    public required string GeneradoPor { get; init; }
    public DateTime GeneradoUtc { get; init; } = DateTime.UtcNow;

    public string Subtitulo =>
        $"Generado el {Texto(GeneradoUtc)} por {GeneradoPor} · {Filas.Count} fila{(Filas.Count == 1 ? "" : "s")} · Filtros: {Filtros}";

    // Las fechas con hora se guardan en UTC y se muestran en hora de Chile.
    public static DateTime AHoraChile(DateTime utc) =>
        TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), HoraChile.Zona());

    public static string Texto(object? valor) => valor switch
    {
        null => "",
        DateTime d => AHoraChile(d).ToString("dd-MM-yyyy HH:mm"),
        DateOnly d => d.ToString("dd-MM-yyyy"),
        bool b => b ? "Sí" : "No",
        _ => valor.ToString() ?? ""
    };
}
