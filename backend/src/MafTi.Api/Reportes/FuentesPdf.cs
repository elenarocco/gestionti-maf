using PdfSharp.Fonts;

namespace MafTi.Api.Reportes;

// PDFsharp necesita que le digan de dónde sacar la fuente. Se usa una fuente sans-serif del sistema:
// Arial en Windows; DejaVu Sans o Liberation Sans en Linux (paquete fonts-dejavu o fonts-liberation).
public class FuentesPdf : IFontResolver
{
    public const string Familia = "Reporte";

    private static readonly string[] Normal =
    [
        @"C:\Windows\Fonts\arial.ttf",
        "/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf",
        "/usr/share/fonts/dejavu/DejaVuSans.ttf",
        "/usr/share/fonts/truetype/liberation/LiberationSans-Regular.ttf",
        "/usr/share/fonts/liberation-sans/LiberationSans-Regular.ttf"
    ];

    private static readonly string[] Negrita =
    [
        @"C:\Windows\Fonts\arialbd.ttf",
        "/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf",
        "/usr/share/fonts/dejavu/DejaVuSans-Bold.ttf",
        "/usr/share/fonts/truetype/liberation/LiberationSans-Bold.ttf",
        "/usr/share/fonts/liberation-sans/LiberationSans-Bold.ttf"
    ];

    public FontResolverInfo ResolveTypeface(string familyName, bool bold, bool italic) =>
        new(bold ? "Reporte-Negrita" : "Reporte-Normal");

    public byte[] GetFont(string faceName)
    {
        var candidatas = faceName == "Reporte-Negrita" ? Negrita : Normal;
        var ruta = candidatas.FirstOrDefault(File.Exists)
            ?? throw new InvalidOperationException(
                "No se encontró una fuente para generar el PDF. En Linux instala el paquete fonts-dejavu. Ver backend/CONFIGURACION.md.");
        return File.ReadAllBytes(ruta);
    }
}
