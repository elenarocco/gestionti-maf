using MafTi.Application;
using static MafTi.Application.ReglasUsuarios;

namespace MafTi.Tests;

// Reglas de administración de usuarios: un acceso por persona, no bloquearse a sí mismo,
// nunca quedar sin administrador y área obligatoria para Jefatura.
public class ReglasUsuariosTests
{
    [Fact]
    public void Se_da_acceso_a_un_trabajador_activo_sin_acceso()
    {
        Assert.Null(ErrorAlDarAcceso(trabajadorActivo: true, yaTieneAcceso: false, "Admin TI", areaId: null));
        Assert.Null(ErrorAlDarAcceso(true, false, RolJefatura, areaId: 7));
    }

    [Fact]
    public void No_se_da_acceso_a_un_inactivo_ni_dos_veces_a_la_misma_persona()
    {
        Assert.NotNull(ErrorAlDarAcceso(trabajadorActivo: false, yaTieneAcceso: false, "RRHH", null));
        Assert.NotNull(ErrorAlDarAcceso(trabajadorActivo: true, yaTieneAcceso: true, "RRHH", null));
    }

    [Fact]
    public void La_Jefatura_necesita_area_al_dar_acceso_y_al_cambiar()
    {
        Assert.NotNull(ErrorAlDarAcceso(true, false, RolJefatura, areaId: null));
        Assert.NotNull(ErrorAlCambiar(esUnoMismo: false, quitaAdministracion: false, administradoresActivos: 2, RolJefatura, areaId: null));
        Assert.Null(ErrorAlCambiar(false, false, 2, RolJefatura, areaId: 7));
    }

    [Fact]
    public void Nadie_cambia_su_propio_rol_ni_se_quita_su_propio_acceso()
    {
        Assert.NotNull(ErrorAlCambiar(esUnoMismo: true, quitaAdministracion: false, administradoresActivos: 3, "Admin TI", null));
        Assert.NotNull(ErrorAlDesactivar(esUnoMismo: true, esAdministrador: false, administradoresActivos: 3));
    }

    [Fact]
    public void La_plataforma_nunca_queda_sin_administrador()
    {
        Assert.NotNull(ErrorAlCambiar(esUnoMismo: false, quitaAdministracion: true, administradoresActivos: 1, "Auditoría", null));
        Assert.NotNull(ErrorAlDesactivar(esUnoMismo: false, esAdministrador: true, administradoresActivos: 1));
        Assert.Null(ErrorAlCambiar(false, quitaAdministracion: true, administradoresActivos: 2, "Auditoría", null));
        Assert.Null(ErrorAlDesactivar(false, esAdministrador: true, administradoresActivos: 2));
    }

    [Fact]
    public void Solo_se_reactiva_si_el_trabajador_sigue_activo()
    {
        Assert.Null(ErrorAlReactivar(trabajadorActivo: true));
        Assert.NotNull(ErrorAlReactivar(trabajadorActivo: false));
    }
}
