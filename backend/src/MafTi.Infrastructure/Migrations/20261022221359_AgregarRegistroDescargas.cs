using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace MafTi.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AgregarRegistroDescargas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RegistrosDescarga",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UsuarioSistemaId = table.Column<int>(type: "integer", nullable: false),
                    Fecha = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Reporte = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Formato = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Filtros = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Filas = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RegistrosDescarga", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RegistrosDescarga_UsuariosSistema_UsuarioSistemaId",
                        column: x => x.UsuarioSistemaId,
                        principalTable: "UsuariosSistema",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RegistrosDescarga_Fecha",
                table: "RegistrosDescarga",
                column: "Fecha");

            migrationBuilder.CreateIndex(
                name: "IX_RegistrosDescarga_UsuarioSistemaId",
                table: "RegistrosDescarga",
                column: "UsuarioSistemaId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RegistrosDescarga");
        }
    }
}
