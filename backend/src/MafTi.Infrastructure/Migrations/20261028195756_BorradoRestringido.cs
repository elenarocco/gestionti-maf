using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MafTi.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class BorradoRestringido : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Accesos_Catalogos_CatalogoId",
                table: "Accesos");

            migrationBuilder.DropForeignKey(
                name: "FK_Accesos_Solicitudes_SolicitudId",
                table: "Accesos");

            migrationBuilder.DropForeignKey(
                name: "FK_Accesos_Trabajadores_TrabajadorId",
                table: "Accesos");

            migrationBuilder.DropForeignKey(
                name: "FK_HistorialSolicitudes_Solicitudes_SolicitudId",
                table: "HistorialSolicitudes");

            migrationBuilder.DropForeignKey(
                name: "FK_HistorialSolicitudes_UsuariosSistema_RealizadoPorId",
                table: "HistorialSolicitudes");

            migrationBuilder.DropForeignKey(
                name: "FK_RolPermisos_Permisos_PermisoId",
                table: "RolPermisos");

            migrationBuilder.DropForeignKey(
                name: "FK_RolPermisos_Roles_RolId",
                table: "RolPermisos");

            migrationBuilder.DropForeignKey(
                name: "FK_SolicitudDetalles_Catalogos_CatalogoId",
                table: "SolicitudDetalles");

            migrationBuilder.DropForeignKey(
                name: "FK_SolicitudDetalles_Solicitudes_SolicitudId",
                table: "SolicitudDetalles");

            migrationBuilder.DropForeignKey(
                name: "FK_Solicitudes_Solicitudes_SolicitudOrigenId",
                table: "Solicitudes");

            migrationBuilder.DropForeignKey(
                name: "FK_Solicitudes_Trabajadores_TrabajadorId",
                table: "Solicitudes");

            migrationBuilder.DropForeignKey(
                name: "FK_Solicitudes_UsuariosSistema_CreadoPorId",
                table: "Solicitudes");

            migrationBuilder.DropForeignKey(
                name: "FK_Trabajadores_Catalogos_AreaId",
                table: "Trabajadores");

            migrationBuilder.DropForeignKey(
                name: "FK_Trabajadores_Catalogos_DireccionCorporativaId",
                table: "Trabajadores");

            migrationBuilder.DropForeignKey(
                name: "FK_Trabajadores_Catalogos_LugarTrabajoId",
                table: "Trabajadores");

            migrationBuilder.DropForeignKey(
                name: "FK_UsuariosSistema_Catalogos_AreaId",
                table: "UsuariosSistema");

            migrationBuilder.DropForeignKey(
                name: "FK_UsuariosSistema_Roles_RolId",
                table: "UsuariosSistema");

            migrationBuilder.DropForeignKey(
                name: "FK_UsuariosSistema_Trabajadores_TrabajadorId",
                table: "UsuariosSistema");

            migrationBuilder.AddForeignKey(
                name: "FK_Accesos_Catalogos_CatalogoId",
                table: "Accesos",
                column: "CatalogoId",
                principalTable: "Catalogos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Accesos_Solicitudes_SolicitudId",
                table: "Accesos",
                column: "SolicitudId",
                principalTable: "Solicitudes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Accesos_Trabajadores_TrabajadorId",
                table: "Accesos",
                column: "TrabajadorId",
                principalTable: "Trabajadores",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_HistorialSolicitudes_Solicitudes_SolicitudId",
                table: "HistorialSolicitudes",
                column: "SolicitudId",
                principalTable: "Solicitudes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_HistorialSolicitudes_UsuariosSistema_RealizadoPorId",
                table: "HistorialSolicitudes",
                column: "RealizadoPorId",
                principalTable: "UsuariosSistema",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_RolPermisos_Permisos_PermisoId",
                table: "RolPermisos",
                column: "PermisoId",
                principalTable: "Permisos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_RolPermisos_Roles_RolId",
                table: "RolPermisos",
                column: "RolId",
                principalTable: "Roles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SolicitudDetalles_Catalogos_CatalogoId",
                table: "SolicitudDetalles",
                column: "CatalogoId",
                principalTable: "Catalogos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SolicitudDetalles_Solicitudes_SolicitudId",
                table: "SolicitudDetalles",
                column: "SolicitudId",
                principalTable: "Solicitudes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Solicitudes_Solicitudes_SolicitudOrigenId",
                table: "Solicitudes",
                column: "SolicitudOrigenId",
                principalTable: "Solicitudes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Solicitudes_Trabajadores_TrabajadorId",
                table: "Solicitudes",
                column: "TrabajadorId",
                principalTable: "Trabajadores",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Solicitudes_UsuariosSistema_CreadoPorId",
                table: "Solicitudes",
                column: "CreadoPorId",
                principalTable: "UsuariosSistema",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Trabajadores_Catalogos_AreaId",
                table: "Trabajadores",
                column: "AreaId",
                principalTable: "Catalogos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Trabajadores_Catalogos_DireccionCorporativaId",
                table: "Trabajadores",
                column: "DireccionCorporativaId",
                principalTable: "Catalogos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Trabajadores_Catalogos_LugarTrabajoId",
                table: "Trabajadores",
                column: "LugarTrabajoId",
                principalTable: "Catalogos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_UsuariosSistema_Catalogos_AreaId",
                table: "UsuariosSistema",
                column: "AreaId",
                principalTable: "Catalogos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_UsuariosSistema_Roles_RolId",
                table: "UsuariosSistema",
                column: "RolId",
                principalTable: "Roles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_UsuariosSistema_Trabajadores_TrabajadorId",
                table: "UsuariosSistema",
                column: "TrabajadorId",
                principalTable: "Trabajadores",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Accesos_Catalogos_CatalogoId",
                table: "Accesos");

            migrationBuilder.DropForeignKey(
                name: "FK_Accesos_Solicitudes_SolicitudId",
                table: "Accesos");

            migrationBuilder.DropForeignKey(
                name: "FK_Accesos_Trabajadores_TrabajadorId",
                table: "Accesos");

            migrationBuilder.DropForeignKey(
                name: "FK_HistorialSolicitudes_Solicitudes_SolicitudId",
                table: "HistorialSolicitudes");

            migrationBuilder.DropForeignKey(
                name: "FK_HistorialSolicitudes_UsuariosSistema_RealizadoPorId",
                table: "HistorialSolicitudes");

            migrationBuilder.DropForeignKey(
                name: "FK_RolPermisos_Permisos_PermisoId",
                table: "RolPermisos");

            migrationBuilder.DropForeignKey(
                name: "FK_RolPermisos_Roles_RolId",
                table: "RolPermisos");

            migrationBuilder.DropForeignKey(
                name: "FK_SolicitudDetalles_Catalogos_CatalogoId",
                table: "SolicitudDetalles");

            migrationBuilder.DropForeignKey(
                name: "FK_SolicitudDetalles_Solicitudes_SolicitudId",
                table: "SolicitudDetalles");

            migrationBuilder.DropForeignKey(
                name: "FK_Solicitudes_Solicitudes_SolicitudOrigenId",
                table: "Solicitudes");

            migrationBuilder.DropForeignKey(
                name: "FK_Solicitudes_Trabajadores_TrabajadorId",
                table: "Solicitudes");

            migrationBuilder.DropForeignKey(
                name: "FK_Solicitudes_UsuariosSistema_CreadoPorId",
                table: "Solicitudes");

            migrationBuilder.DropForeignKey(
                name: "FK_Trabajadores_Catalogos_AreaId",
                table: "Trabajadores");

            migrationBuilder.DropForeignKey(
                name: "FK_Trabajadores_Catalogos_DireccionCorporativaId",
                table: "Trabajadores");

            migrationBuilder.DropForeignKey(
                name: "FK_Trabajadores_Catalogos_LugarTrabajoId",
                table: "Trabajadores");

            migrationBuilder.DropForeignKey(
                name: "FK_UsuariosSistema_Catalogos_AreaId",
                table: "UsuariosSistema");

            migrationBuilder.DropForeignKey(
                name: "FK_UsuariosSistema_Roles_RolId",
                table: "UsuariosSistema");

            migrationBuilder.DropForeignKey(
                name: "FK_UsuariosSistema_Trabajadores_TrabajadorId",
                table: "UsuariosSistema");

            migrationBuilder.AddForeignKey(
                name: "FK_Accesos_Catalogos_CatalogoId",
                table: "Accesos",
                column: "CatalogoId",
                principalTable: "Catalogos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Accesos_Solicitudes_SolicitudId",
                table: "Accesos",
                column: "SolicitudId",
                principalTable: "Solicitudes",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Accesos_Trabajadores_TrabajadorId",
                table: "Accesos",
                column: "TrabajadorId",
                principalTable: "Trabajadores",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_HistorialSolicitudes_Solicitudes_SolicitudId",
                table: "HistorialSolicitudes",
                column: "SolicitudId",
                principalTable: "Solicitudes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_HistorialSolicitudes_UsuariosSistema_RealizadoPorId",
                table: "HistorialSolicitudes",
                column: "RealizadoPorId",
                principalTable: "UsuariosSistema",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_RolPermisos_Permisos_PermisoId",
                table: "RolPermisos",
                column: "PermisoId",
                principalTable: "Permisos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_RolPermisos_Roles_RolId",
                table: "RolPermisos",
                column: "RolId",
                principalTable: "Roles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_SolicitudDetalles_Catalogos_CatalogoId",
                table: "SolicitudDetalles",
                column: "CatalogoId",
                principalTable: "Catalogos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_SolicitudDetalles_Solicitudes_SolicitudId",
                table: "SolicitudDetalles",
                column: "SolicitudId",
                principalTable: "Solicitudes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Solicitudes_Solicitudes_SolicitudOrigenId",
                table: "Solicitudes",
                column: "SolicitudOrigenId",
                principalTable: "Solicitudes",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Solicitudes_Trabajadores_TrabajadorId",
                table: "Solicitudes",
                column: "TrabajadorId",
                principalTable: "Trabajadores",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Solicitudes_UsuariosSistema_CreadoPorId",
                table: "Solicitudes",
                column: "CreadoPorId",
                principalTable: "UsuariosSistema",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Trabajadores_Catalogos_AreaId",
                table: "Trabajadores",
                column: "AreaId",
                principalTable: "Catalogos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Trabajadores_Catalogos_DireccionCorporativaId",
                table: "Trabajadores",
                column: "DireccionCorporativaId",
                principalTable: "Catalogos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Trabajadores_Catalogos_LugarTrabajoId",
                table: "Trabajadores",
                column: "LugarTrabajoId",
                principalTable: "Catalogos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UsuariosSistema_Catalogos_AreaId",
                table: "UsuariosSistema",
                column: "AreaId",
                principalTable: "Catalogos",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_UsuariosSistema_Roles_RolId",
                table: "UsuariosSistema",
                column: "RolId",
                principalTable: "Roles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UsuariosSistema_Trabajadores_TrabajadorId",
                table: "UsuariosSistema",
                column: "TrabajadorId",
                principalTable: "Trabajadores",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
