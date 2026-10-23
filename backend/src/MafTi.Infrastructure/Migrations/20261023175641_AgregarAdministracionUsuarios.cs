using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace MafTi.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AgregarAdministracionUsuarios : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UsuariosSistema_TrabajadorId",
                table: "UsuariosSistema");

            migrationBuilder.CreateTable(
                name: "CambiosUsuarioSistema",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UsuarioSistemaId = table.Column<int>(type: "integer", nullable: false),
                    RealizadoPorId = table.Column<int>(type: "integer", nullable: false),
                    Fecha = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Accion = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Detalle = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CambiosUsuarioSistema", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CambiosUsuarioSistema_UsuariosSistema_RealizadoPorId",
                        column: x => x.RealizadoPorId,
                        principalTable: "UsuariosSistema",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CambiosUsuarioSistema_UsuariosSistema_UsuarioSistemaId",
                        column: x => x.UsuarioSistemaId,
                        principalTable: "UsuariosSistema",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UsuariosSistema_TrabajadorId",
                table: "UsuariosSistema",
                column: "TrabajadorId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CambiosUsuarioSistema_RealizadoPorId",
                table: "CambiosUsuarioSistema",
                column: "RealizadoPorId");

            migrationBuilder.CreateIndex(
                name: "IX_CambiosUsuarioSistema_UsuarioSistemaId",
                table: "CambiosUsuarioSistema",
                column: "UsuarioSistemaId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CambiosUsuarioSistema");

            migrationBuilder.DropIndex(
                name: "IX_UsuariosSistema_TrabajadorId",
                table: "UsuariosSistema");

            migrationBuilder.CreateIndex(
                name: "IX_UsuariosSistema_TrabajadorId",
                table: "UsuariosSistema",
                column: "TrabajadorId");
        }
    }
}
