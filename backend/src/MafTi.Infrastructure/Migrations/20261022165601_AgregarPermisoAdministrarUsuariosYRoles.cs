using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MafTi.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AgregarPermisoAdministrarUsuariosYRoles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Administrar roles, permisos y usuarios del sistema queda solo para Super Admin:
            // quien puede hacerlo puede darse cualquier permiso. Idempotente.
            migrationBuilder.Sql(@"
                INSERT INTO ""Permisos"" (""Codigo"", ""Descripcion"")
                SELECT 'AdministrarUsuariosYRoles', 'Administrar roles, permisos y usuarios del sistema'
                WHERE NOT EXISTS (SELECT 1 FROM ""Permisos"" WHERE ""Codigo"" = 'AdministrarUsuariosYRoles');

                INSERT INTO ""RolPermisos"" (""RolId"", ""PermisoId"")
                SELECT r.""Id"", p.""Id""
                FROM ""Roles"" r JOIN ""Permisos"" p ON p.""Codigo"" = 'AdministrarUsuariosYRoles'
                WHERE r.""Nombre"" = 'Super Admin'
                  AND NOT EXISTS (SELECT 1 FROM ""RolPermisos"" rp WHERE rp.""RolId"" = r.""Id"" AND rp.""PermisoId"" = p.""Id"");");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DELETE FROM ""RolPermisos"" rp USING ""Permisos"" p
                WHERE rp.""PermisoId"" = p.""Id"" AND p.""Codigo"" = 'AdministrarUsuariosYRoles';
                DELETE FROM ""Permisos"" WHERE ""Codigo"" = 'AdministrarUsuariosYRoles';");
        }
    }
}
