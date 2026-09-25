using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MafTi.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AgregarRelacionesTrabajadorCatalogo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Trabajadores_AreaId",
                table: "Trabajadores",
                column: "AreaId");

            migrationBuilder.CreateIndex(
                name: "IX_Trabajadores_DireccionCorporativaId",
                table: "Trabajadores",
                column: "DireccionCorporativaId");

            migrationBuilder.CreateIndex(
                name: "IX_Trabajadores_LugarTrabajoId",
                table: "Trabajadores",
                column: "LugarTrabajoId");

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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Trabajadores_Catalogos_AreaId",
                table: "Trabajadores");

            migrationBuilder.DropForeignKey(
                name: "FK_Trabajadores_Catalogos_DireccionCorporativaId",
                table: "Trabajadores");

            migrationBuilder.DropForeignKey(
                name: "FK_Trabajadores_Catalogos_LugarTrabajoId",
                table: "Trabajadores");

            migrationBuilder.DropIndex(
                name: "IX_Trabajadores_AreaId",
                table: "Trabajadores");

            migrationBuilder.DropIndex(
                name: "IX_Trabajadores_DireccionCorporativaId",
                table: "Trabajadores");

            migrationBuilder.DropIndex(
                name: "IX_Trabajadores_LugarTrabajoId",
                table: "Trabajadores");
        }
    }
}
