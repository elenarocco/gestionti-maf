using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MafTi.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AgregarPadreACatalogo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PadreId",
                table: "Catalogos",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Catalogos_PadreId",
                table: "Catalogos",
                column: "PadreId");

            migrationBuilder.AddForeignKey(
                name: "FK_Catalogos_Catalogos_PadreId",
                table: "Catalogos",
                column: "PadreId",
                principalTable: "Catalogos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Catalogos_Catalogos_PadreId",
                table: "Catalogos");

            migrationBuilder.DropIndex(
                name: "IX_Catalogos_PadreId",
                table: "Catalogos");

            migrationBuilder.DropColumn(
                name: "PadreId",
                table: "Catalogos");
        }
    }
}
