using MigraDoc.DocumentObjectModel;
using MigraDoc.DocumentObjectModel.Tables;
using MigraDoc.Rendering;
using PdfSharp.Fonts;

namespace MafTi.Api.Reportes;

// PDF horizontal (A4) con título, subtítulo, la tabla (el encabezado se repite en cada página) y
// "Página X de Y" al pie.
public static class GeneradorPdf
{
    private const string Fuente = FuentesPdf.Familia;

    static GeneradorPdf()
    {
        GlobalFontSettings.FontResolver ??= new FuentesPdf();
    }

    public static byte[] Generar(Reporte r)
    {
        var doc = new Document();
        doc.Info.Title = r.Titulo;
        doc.Styles[StyleNames.Normal]!.Font.Name = Fuente;
        doc.Styles[StyleNames.Normal]!.Font.Size = 8;

        var seccion = doc.AddSection();
        // A4 horizontal con medidas explícitas (MigraDoc no gira la página solo con Orientation).
        seccion.PageSetup = doc.DefaultPageSetup.Clone();
        seccion.PageSetup.PageWidth = Unit.FromCentimeter(29.7);
        seccion.PageSetup.PageHeight = Unit.FromCentimeter(21);
        seccion.PageSetup.LeftMargin = seccion.PageSetup.RightMargin = Unit.FromCentimeter(1.5);
        seccion.PageSetup.TopMargin = seccion.PageSetup.BottomMargin = Unit.FromCentimeter(1.5);

        var titulo = seccion.AddParagraph(r.Titulo);
        titulo.Format.Font.Size = 14;
        titulo.Format.Font.Bold = true;
        var subtitulo = seccion.AddParagraph(r.Subtitulo);
        subtitulo.Format.Font.Color = Colors.Gray;
        subtitulo.Format.SpaceAfter = Unit.FromPoint(10);

        // A4 horizontal = 29,7 cm; se reparte el ancho útil según el ancho relativo de cada columna.
        var anchoUtil = 29.7 - 3.0;
        var totalRelativo = r.Columnas.Sum(c => c.Ancho);
        var tabla = seccion.AddTable();
        tabla.Borders.Color = Colors.LightGray;
        tabla.Borders.Width = 0.5;
        foreach (var c in r.Columnas)
            tabla.AddColumn(Unit.FromCentimeter(anchoUtil * c.Ancho / totalRelativo));

        var encabezado = tabla.AddRow();
        encabezado.HeadingFormat = true;
        encabezado.Format.Font.Bold = true;
        encabezado.Shading.Color = new Color(240, 239, 236);
        for (var c = 0; c < r.Columnas.Count; c++)
            encabezado.Cells[c].AddParagraph(r.Columnas[c].Titulo);

        foreach (var fila in r.Filas)
        {
            var row = tabla.AddRow();
            for (var c = 0; c < r.Columnas.Count; c++)
                row.Cells[c].AddParagraph(Reporte.Texto(fila[c]));
        }
        foreach (Row row in tabla.Rows)
        {
            row.TopPadding = row.BottomPadding = Unit.FromPoint(2);
            row.VerticalAlignment = VerticalAlignment.Center;
        }

        var pie = seccion.Footers.Primary.AddParagraph();
        pie.Format.Alignment = ParagraphAlignment.Right;
        pie.Format.Font.Color = Colors.Gray;
        pie.AddText("Página ");
        pie.AddPageField();
        pie.AddText(" de ");
        pie.AddNumPagesField();

        var renderer = new PdfDocumentRenderer { Document = doc };
        renderer.RenderDocument();
        using var ms = new MemoryStream();
        renderer.PdfDocument.Save(ms, false);
        return ms.ToArray();
    }
}
