using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace MafTi.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AgregarRolYCorregirUsuarioSistema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CorreoAD",
                table: "UsuariosSistema");

            migrationBuilder.DropColumn(
                name: "Rol",
                table: "UsuariosSistema");

            migrationBuilder.AddColumn<int>(
                name: "RolId",
                table: "UsuariosSistema",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TrabajadorId",
                table: "UsuariosSistema",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "Roles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nombre = table.Column<string>(type: "text", nullable: false),
                    Descripcion = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UsuariosSistema_RolId",
                table: "UsuariosSistema",
                column: "RolId");

            migrationBuilder.CreateIndex(
                name: "IX_UsuariosSistema_TrabajadorId",
                table: "UsuariosSistema",
                column: "TrabajadorId");

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UsuariosSistema_Roles_RolId",
                table: "UsuariosSistema");

            migrationBuilder.DropForeignKey(
                name: "FK_UsuariosSistema_Trabajadores_TrabajadorId",
                table: "UsuariosSistema");

            migrationBuilder.DropTable(
                name: "Roles");

            migrationBuilder.DropIndex(
                name: "IX_UsuariosSistema_RolId",
                table: "UsuariosSistema");

            migrationBuilder.DropIndex(
                name: "IX_UsuariosSistema_TrabajadorId",
                table: "UsuariosSistema");

            migrationBuilder.DropColumn(
                name: "RolId",
                table: "UsuariosSistema");

            migrationBuilder.DropColumn(
                name: "TrabajadorId",
                table: "UsuariosSistema");

            migrationBuilder.AddColumn<string>(
                name: "CorreoAD",
                table: "UsuariosSistema",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Rol",
                table: "UsuariosSistema",
                type: "text",
                nullable: false,
                defaultValue: "");
        }
    }
}
