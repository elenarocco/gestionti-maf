using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MafTi.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CargoComoCatalogo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CargoId",
                table: "Trabajadores",
                type: "integer",
                nullable: true);

            // 1) Crea un cargo en el catálogo por cada cargo distinto
            //    (junta variantes de mayúsculas y limpia espacios, incluidos los invisibles)
            migrationBuilder.Sql(@"
                INSERT INTO ""Catalogos"" (""Tipo"", ""Nombre"", ""Activo"")
                SELECT 'Cargo', MIN(TRIM(REGEXP_REPLACE(""Cargo"", '[\s\u00A0]+', ' ', 'g'))), true
                FROM ""Trabajadores""
                WHERE TRIM(REGEXP_REPLACE(""Cargo"", '[\s\u00A0]+', ' ', 'g')) <> ''
                GROUP BY LOWER(TRIM(REGEXP_REPLACE(""Cargo"", '[\s\u00A0]+', ' ', 'g')));");

            // 2) Conecta cada trabajador con su cargo
            migrationBuilder.Sql(@"
                UPDATE ""Trabajadores"" t
                SET ""CargoId"" = c.""Id""
                FROM ""Catalogos"" c
                WHERE c.""Tipo"" = 'Cargo'
                  AND LOWER(c.""Nombre"") = LOWER(TRIM(REGEXP_REPLACE(t.""Cargo"", '[\s\u00A0]+', ' ', 'g')));");

            migrationBuilder.AlterColumn<int>(
                name: "CargoId",
                table: "Trabajadores",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Trabajadores_CargoId",
                table: "Trabajadores",
                column: "CargoId");

            migrationBuilder.AddForeignKey(
                name: "FK_Trabajadores_Catalogos_CargoId",
                table: "Trabajadores",
                column: "CargoId",
                principalTable: "Catalogos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.DropColumn(
                name: "Cargo",
                table: "Trabajadores");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Trabajadores_Catalogos_CargoId",
                table: "Trabajadores");

            migrationBuilder.DropIndex(
                name: "IX_Trabajadores_CargoId",
                table: "Trabajadores");

            migrationBuilder.AddColumn<string>(
                name: "Cargo",
                table: "Trabajadores",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql(@"
                UPDATE ""Trabajadores"" t
                SET ""Cargo"" = c.""Nombre""
                FROM ""Catalogos"" c
                WHERE c.""Id"" = t.""CargoId"";");

            migrationBuilder.DropColumn(
                name: "CargoId",
                table: "Trabajadores");
        }
    }
}