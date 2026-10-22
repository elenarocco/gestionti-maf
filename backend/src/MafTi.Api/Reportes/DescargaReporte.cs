using MafTi.Api.Seguridad;
using MafTi.Domain;
using MafTi.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace MafTi.Api.Reportes;

public enum FormatoReporte { Excel, Pdf }

// Genera el archivo pedido y deja registrada la descarga (RegistrosDescarga) antes de entregarlo.
public static class DescargaReporte
{
    public const string Permiso = "ExportarReportes";

    public static FormatoReporte? LeerFormato(string? formato) => formato?.Trim().ToLowerInvariant() switch
    {
        "excel" or "xlsx" => FormatoReporte.Excel,
        "pdf" => FormatoReporte.Pdf,
        _ => null
    };

    public static ObjectResult FormatoInvalido(ControllerBase c) =>
        c.BadRequest(new { error = "Formato no válido: usa excel o pdf." });

    public static ObjectResult DemasiadasFilas(ControllerBase c) =>
        c.BadRequest(new { error = $"El reporte supera las {Reporte.MaxFilas:N0} filas. Agrega filtros para acotarlo." });

    public static async Task<FileContentResult> EntregarAsync(
        ControllerBase c, AppDbContext db, UsuarioActual usuario, Reporte r, FormatoReporte formato)
    {
        var (bytes, tipo, extension) = formato == FormatoReporte.Excel
            ? (GeneradorExcel.Generar(r), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "xlsx")
            : (GeneradorPdf.Generar(r), "application/pdf", "pdf");

        db.RegistrosDescarga.Add(new RegistroDescarga
        {
            UsuarioSistemaId = usuario.Id,
            Fecha = r.GeneradoUtc,
            Reporte = r.Titulo,
            Formato = formato == FormatoReporte.Excel ? "Excel" : "PDF",
            Filtros = r.Filtros.Length > 500 ? r.Filtros[..500] : r.Filtros,
            Filas = r.Filas.Count
        });
        await db.SaveChangesAsync();

        var fecha = Reporte.AHoraChile(r.GeneradoUtc).ToString("yyyy-MM-dd_HHmm");
        return c.File(bytes, tipo, $"{r.NombreArchivo}_{fecha}.{extension}");
    }
}
