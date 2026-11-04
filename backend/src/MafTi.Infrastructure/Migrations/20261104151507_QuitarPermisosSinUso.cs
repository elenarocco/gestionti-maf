using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MafTi.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class QuitarPermisosSinUso : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Estos cuatro permisos existían en la tabla pero ningún código los revisaba: todas las acciones de la
            // Bandeja de TI dependen de GestionarBandejaTI. Asignarlos o quitarlos no cambiaba nada, solo confundía
            // en la pantalla Usuarios y roles. Primero se quitan de los roles (RolPermisos no permite borrar en
            // cascada) y luego se borran. Idempotente.
            migrationBuilder.Sql(@"
                DELETE FROM ""RolPermisos"" rp USING ""Permisos"" p
                WHERE rp.""PermisoId"" = p.""Id""
                  AND p.""Codigo"" IN ('MarcarCompletada', 'RechazarSolicitud', 'RevisarAccesoVPN', 'RevisarAprobarBloqueo');

                DELETE FROM ""Permisos""
                WHERE ""Codigo"" IN ('MarcarCompletada', 'RechazarSolicitud', 'RevisarAccesoVPN', 'RevisarAprobarBloqueo');");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Vuelven los permisos con su descripción, sin asignar a ningún rol (no tenían efecto).
            migrationBuilder.Sql(@"
                INSERT INTO ""Permisos"" (""Codigo"", ""Descripcion"")
                SELECT v.codigo, v.descripcion FROM (VALUES
                    ('MarcarCompletada', 'Marcar solicitudes como completadas'),
                    ('RechazarSolicitud', 'Rechazar solicitudes'),
                    ('RevisarAccesoVPN', 'Revisar y aprobar el acceso VPN'),
                    ('RevisarAprobarBloqueo', 'Revisar y aprobar Bloqueos')
                ) AS v(codigo, descripcion)
                WHERE NOT EXISTS (SELECT 1 FROM ""Permisos"" p WHERE p.""Codigo"" = v.codigo);");
        }
    }
}
