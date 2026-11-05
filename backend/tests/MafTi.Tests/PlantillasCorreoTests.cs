using MafTi.Application.Notificaciones;

namespace MafTi.Tests;

public class PlantillasCorreoTests
{
    private static PlantillasCorreo.DatosSolicitud Datos(string tipo = "Ingreso", string? persona = "Camila González") => new(
        42, tipo, persona, "Paula Ramírez", new DateTime(2026, 11, 5, 10, 30, 0), new DateTime(2026, 11, 10, 23, 59, 0));

    [Fact]
    public void Nueva_solicitud_lleva_los_datos_y_el_enlace_a_la_bandeja()
    {
        var c = PlantillasCorreo.NuevaSolicitud(Datos(), "http://localhost:4200");

        Assert.Equal("Nueva solicitud de Ingreso N° 42 — Camila González", c.Asunto);
        Assert.Contains("Paula Ramírez", c.Html);
        Assert.Contains("10-11-2026 23:59", c.Html);
        Assert.Contains("href=\"http://localhost:4200/bandeja-ti/42\"", c.Html);
    }

    [Fact]
    public void El_rechazo_incluye_el_motivo_y_si_se_puede_corregir()
    {
        var c = PlantillasCorreo.Rechazada(Datos(), "Falta el jefe directo.", sePuedeCorregir: true, "https://gestionti.mafchile.com");

        Assert.Equal("Tu solicitud de Ingreso N° 42 fue rechazada", c.Asunto);
        Assert.Contains("Falta el jefe directo.", c.Html);
        Assert.Contains("Puedes corregirla", c.Html);
        Assert.Contains("href=\"https://gestionti.mafchile.com/solicitudes/42\"", c.Html);
    }

    [Fact]
    public void Una_VPN_rechazada_no_ofrece_corregir()
    {
        var c = PlantillasCorreo.Rechazada(Datos("VPN", null), "Proveedor sin contrato.", sePuedeCorregir: false, "");
        Assert.DoesNotContain("Puedes corregirla", c.Html);
    }

    [Fact]
    public void La_VPN_aprobada_enlaza_a_la_ficha_del_proveedor()
    {
        var c = PlantillasCorreo.VpnAprobada(Datos("VPN", null), "http://localhost:4200");
        Assert.Contains("href=\"http://localhost:4200/solicitudes/vpn/42/ficha\"", c.Html);
    }

    [Fact]
    public void El_texto_de_la_base_se_codifica_en_el_HTML()
    {
        var c = PlantillasCorreo.Completada(Datos(persona: "<script>alert(1)</script>"), "Trabajador \"activo\" & listo", "");

        Assert.DoesNotContain("<script>", c.Html);
        Assert.Contains("&lt;script&gt;", c.Html);
        Assert.Contains("&quot;activo&quot; &amp; listo", c.Html);
    }
}
