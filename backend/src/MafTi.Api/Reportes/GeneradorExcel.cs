using ClosedXML.Excel;

namespace MafTi.Api.Reportes;

// Excel con título, subtítulo (quién, cuándo, filtros) y la tabla con filtros y encabezado fijo.
// Los textos se guardan como texto, nunca como fórmula: un valor que empiece con "=" no se ejecuta.
public static class GeneradorExcel
{
    public static byte[] Generar(Reporte r)
    {
        using var libro = new XLWorkbook();
        var hoja = libro.Worksheets.Add(r.Titulo.Length > 31 ? r.Titulo[..31] : r.Titulo);

        hoja.Cell(1, 1).Value = r.Titulo;
        hoja.Cell(1, 1).Style.Font.SetBold().Font.SetFontSize(14);
        hoja.Cell(2, 1).Value = r.Subtitulo;
        hoja.Cell(2, 1).Style.Font.SetFontColor(XLColor.Gray);

        const int filaEncabezado = 4;
        for (var c = 0; c < r.Columnas.Count; c++)
            hoja.Cell(filaEncabezado, c + 1).Value = r.Columnas[c].Titulo;

        for (var f = 0; f < r.Filas.Count; f++)
            for (var c = 0; c < r.Columnas.Count; c++)
                Escribir(hoja.Cell(filaEncabezado + 1 + f, c + 1), r.Filas[f][c]);

        var rango = hoja.Range(filaEncabezado, 1, filaEncabezado + Math.Max(r.Filas.Count, 1), r.Columnas.Count);
        var tabla = rango.CreateTable("Datos");
        tabla.Theme = XLTableTheme.TableStyleLight9;

        hoja.SheetView.FreezeRows(filaEncabezado);
        hoja.Columns(1, r.Columnas.Count).AdjustToContents(filaEncabezado, filaEncabezado + Math.Min(r.Filas.Count, 500));
        foreach (var col in hoja.Columns(1, r.Columnas.Count))
            col.Width = Math.Min(col.Width, 60);

        using var ms = new MemoryStream();
        libro.SaveAs(ms);
        return ms.ToArray();
    }

    private static void Escribir(IXLCell celda, object? valor)
    {
        switch (valor)
        {
            case null: break;
            case int n: celda.Value = n; break;
            case DateTime d:
                celda.Value = Reporte.AHoraChile(d);
                celda.Style.DateFormat.Format = "dd-mm-yyyy hh:mm";
                break;
            case DateOnly d:
                celda.Value = d.ToDateTime(TimeOnly.MinValue);
                celda.Style.DateFormat.Format = "dd-mm-yyyy";
                break;
            default: celda.Value = Reporte.Texto(valor); break;
        }
    }
}
