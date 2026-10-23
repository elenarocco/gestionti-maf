using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MafTi.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AgregarDescripcionesPermisos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Descripciones legibles para la tabla de roles y permisos. Solo completa las que están vacías.
            migrationBuilder.Sql(@"
                UPDATE ""Permisos"" SET ""Descripcion"" = 'Administrar los catálogos (sistemas y carpetas de red)' WHERE ""Codigo"" = 'AdministrarCatalogos' AND ""Descripcion"" IS NULL;
                UPDATE ""Permisos"" SET ""Descripcion"" = 'Completar la ficha VPN de sus propias solicitudes aprobadas' WHERE ""Codigo"" = 'CompletarFichaVPN' AND ""Descripcion"" IS NULL;
                UPDATE ""Permisos"" SET ""Descripcion"" = 'Ver las solicitudes que creó' WHERE ""Codigo"" = 'ConsultarSolicitudesPropias' AND ""Descripcion"" IS NULL;
                UPDATE ""Permisos"" SET ""Descripcion"" = 'Ver todas las solicitudes' WHERE ""Codigo"" = 'ConsultarTodasLasSolicitudes' AND ""Descripcion"" IS NULL;
                UPDATE ""Permisos"" SET ""Descripcion"" = 'Ver trabajadores y sus accesos (Jefatura: solo su área)' WHERE ""Codigo"" = 'ConsultarTrabajadoresYAccesos' AND ""Descripcion"" IS NULL;
                UPDATE ""Permisos"" SET ""Descripcion"" = 'Crear solicitudes de Bloqueo' WHERE ""Codigo"" = 'CrearSolicitudBloqueo' AND ""Descripcion"" IS NULL;
                UPDATE ""Permisos"" SET ""Descripcion"" = 'Crear solicitudes de Ingreso' WHERE ""Codigo"" = 'CrearSolicitudIngreso' AND ""Descripcion"" IS NULL;
                UPDATE ""Permisos"" SET ""Descripcion"" = 'Crear solicitudes de Modificación' WHERE ""Codigo"" = 'CrearSolicitudModificacion' AND ""Descripcion"" IS NULL;
                UPDATE ""Permisos"" SET ""Descripcion"" = 'Descargar reportes en Excel y PDF' WHERE ""Codigo"" = 'ExportarReportes' AND ""Descripcion"" IS NULL;
                UPDATE ""Permisos"" SET ""Descripcion"" = 'Marcar solicitudes como completadas' WHERE ""Codigo"" = 'MarcarCompletada' AND ""Descripcion"" IS NULL;
                UPDATE ""Permisos"" SET ""Descripcion"" = 'Rechazar solicitudes' WHERE ""Codigo"" = 'RechazarSolicitud' AND ""Descripcion"" IS NULL;
                UPDATE ""Permisos"" SET ""Descripcion"" = 'Revisar y aprobar el acceso VPN' WHERE ""Codigo"" = 'RevisarAccesoVPN' AND ""Descripcion"" IS NULL;
                UPDATE ""Permisos"" SET ""Descripcion"" = 'Revisar y aprobar Bloqueos' WHERE ""Codigo"" = 'RevisarAprobarBloqueo' AND ""Descripcion"" IS NULL;
                UPDATE ""Permisos"" SET ""Descripcion"" = 'Solicitar acceso VPN para un proveedor' WHERE ""Codigo"" = 'SolicitarAccesoVPN' AND ""Descripcion"" IS NULL;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Las descripciones no afectan permisos: no se revierten.
        }
    }
}
