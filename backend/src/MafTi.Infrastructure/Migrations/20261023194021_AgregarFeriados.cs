using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace MafTi.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AgregarFeriados : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Feriados",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Fecha = table.Column<DateOnly>(type: "date", nullable: false),
                    Nombre = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Feriados", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Feriados_Fecha",
                table: "Feriados",
                column: "Fecha",
                unique: true);

            // Feriados nacionales de Chile 2026 y 2027 (fuente: date.nager.at, revisados contra la ley).
            // Los regionales (p. ej. Arica) no se incluyen. Idempotente: no duplica fechas ya cargadas.
            migrationBuilder.Sql(@"
                INSERT INTO ""Feriados"" (""Fecha"", ""Nombre"")
                SELECT v.fecha::date, v.nombre FROM (VALUES
                    ('2026-01-01', 'Año Nuevo'),
                    ('2026-04-03', 'Viernes Santo'),
                    ('2026-04-04', 'Sábado Santo'),
                    ('2026-05-01', 'Día del Trabajo'),
                    ('2026-05-21', 'Día de las Glorias Navales'),
                    ('2026-06-21', 'Día Nacional de los Pueblos Indígenas'),
                    ('2026-06-29', 'San Pedro y San Pablo'),
                    ('2026-07-16', 'Virgen del Carmen'),
                    ('2026-08-15', 'Asunción de la Virgen'),
                    ('2026-09-18', 'Fiestas Patrias'),
                    ('2026-09-19', 'Día de las Glorias del Ejército'),
                    ('2026-10-12', 'Encuentro de Dos Mundos'),
                    ('2026-10-31', 'Día de las Iglesias Evangélicas y Protestantes'),
                    ('2026-11-01', 'Día de Todos los Santos'),
                    ('2026-12-08', 'Inmaculada Concepción'),
                    ('2026-12-25', 'Navidad'),
                    ('2027-01-01', 'Año Nuevo'),
                    ('2027-03-26', 'Viernes Santo'),
                    ('2027-03-27', 'Sábado Santo'),
                    ('2027-05-01', 'Día del Trabajo'),
                    ('2027-05-21', 'Día de las Glorias Navales'),
                    ('2027-06-21', 'Día Nacional de los Pueblos Indígenas'),
                    ('2027-06-28', 'San Pedro y San Pablo'),
                    ('2027-07-16', 'Virgen del Carmen'),
                    ('2027-08-15', 'Asunción de la Virgen'),
                    ('2027-09-18', 'Fiestas Patrias'),
                    ('2027-09-19', 'Día de las Glorias del Ejército'),
                    ('2027-10-11', 'Encuentro de Dos Mundos'),
                    ('2027-10-31', 'Día de las Iglesias Evangélicas y Protestantes'),
                    ('2027-11-01', 'Día de Todos los Santos'),
                    ('2027-12-08', 'Inmaculada Concepción'),
                    ('2027-12-25', 'Navidad')
                ) AS v(fecha, nombre)
                WHERE NOT EXISTS (SELECT 1 FROM ""Feriados"" f WHERE f.""Fecha"" = v.fecha::date);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Feriados");
        }
    }
}
