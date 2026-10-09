using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace MafTi.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AgregarSolicitudModificacion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Accion",
                table: "SolicitudDetalles",
                type: "character varying(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "Agregar");

            migrationBuilder.CreateTable(
                name: "SolicitudesModificacion",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SolicitudId = table.Column<int>(type: "integer", nullable: false),
                    Justificacion = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    PrimerNombre = table.Column<string>(type: "text", nullable: false),
                    SegundoNombre = table.Column<string>(type: "text", nullable: true),
                    PrimerApellido = table.Column<string>(type: "text", nullable: false),
                    SegundoApellido = table.Column<string>(type: "text", nullable: true),
                    FechaNacimiento = table.Column<DateOnly>(type: "date", nullable: false),
                    Sexo = table.Column<string>(type: "text", nullable: false),
                    Correo = table.Column<string>(type: "text", nullable: false),
                    DireccionCorporativaId = table.Column<int>(type: "integer", nullable: false),
                    AreaId = table.Column<int>(type: "integer", nullable: false),
                    CargoId = table.Column<int>(type: "integer", nullable: false),
                    LugarTrabajoId = table.Column<int>(type: "integer", nullable: false),
                    FechaIncorporacion = table.Column<DateOnly>(type: "date", nullable: false),
                    DireccionDomicilio = table.Column<string>(type: "text", nullable: true),
                    JefeDirecto = table.Column<string>(type: "text", nullable: true),
                    HomologarAccesosDesde = table.Column<string>(type: "text", nullable: true),
                    TieneTelefonoCorporativo = table.Column<bool>(type: "boolean", nullable: false),
                    SolicitaTelefono = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SolicitudesModificacion", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SolicitudesModificacion_Solicitudes_SolicitudId",
                        column: x => x.SolicitudId,
                        principalTable: "Solicitudes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SolicitudesModificacion_SolicitudId",
                table: "SolicitudesModificacion",
                column: "SolicitudId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SolicitudesModificacion");

            migrationBuilder.DropColumn(
                name: "Accion",
                table: "SolicitudDetalles");
        }
    }
}
