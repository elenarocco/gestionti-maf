using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MafTi.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AjustarRolesYVpn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1) El rol con todos los privilegios deja de llamarse "Super Admin": un nombre neutro no anuncia cuál es
            //    la cuenta más valiosa. La lógica usa permisos, no el nombre.
            // 2) Ese rol tiene TODOS los permisos (le faltaban crear Bloqueo y la Bandeja de TI).
            // 3) La VPN la pide y completa solo Admin TI (antes Jefatura). Idempotente.
            migrationBuilder.Sql(@"
                UPDATE ""Roles"" SET ""Nombre"" = 'Gestión General' WHERE ""Nombre"" = 'Super Admin';

                INSERT INTO ""RolPermisos"" (""RolId"", ""PermisoId"")
                SELECT r.""Id"", p.""Id"" FROM ""Roles"" r CROSS JOIN ""Permisos"" p
                WHERE r.""Nombre"" = 'Gestión General'
                  AND NOT EXISTS (SELECT 1 FROM ""RolPermisos"" rp WHERE rp.""RolId"" = r.""Id"" AND rp.""PermisoId"" = p.""Id"");

                DELETE FROM ""RolPermisos"" rp USING ""Roles"" r, ""Permisos"" p
                WHERE rp.""RolId"" = r.""Id"" AND rp.""PermisoId"" = p.""Id""
                  AND r.""Nombre"" = 'Jefatura' AND p.""Codigo"" IN ('SolicitarAccesoVPN', 'CompletarFichaVPN');

                INSERT INTO ""RolPermisos"" (""RolId"", ""PermisoId"")
                SELECT r.""Id"", p.""Id"" FROM ""Roles"" r JOIN ""Permisos"" p ON p.""Codigo"" IN ('SolicitarAccesoVPN', 'CompletarFichaVPN')
                WHERE r.""Nombre"" = 'Admin TI'
                  AND NOT EXISTS (SELECT 1 FROM ""RolPermisos"" rp WHERE rp.""RolId"" = r.""Id"" AND rp.""PermisoId"" = p.""Id"");

                UPDATE ""Permisos"" SET ""Descripcion"" = 'Solicitar acceso VPN para un proveedor (queda aprobada al crearla)'
                WHERE ""Codigo"" = 'SolicitarAccesoVPN';
                UPDATE ""Permisos"" SET ""Descripcion"" = 'Completar la ficha VPN de solicitudes anteriores'
                WHERE ""Codigo"" = 'CompletarFichaVPN';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"UPDATE ""Roles"" SET ""Nombre"" = 'Super Admin' WHERE ""Nombre"" = 'Gestión General';");
        }
    }
}
