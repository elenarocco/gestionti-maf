using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MafTi.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ActualizarTrabajador : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ApellidoMaterno",
                table: "Trabajadores");

            migrationBuilder.DropColumn(
                name: "FechaIngreso",
                table: "Trabajadores");

            migrationBuilder.RenameColumn(
                name: "Nombres",
                table: "Trabajadores",
                newName: "Sexo");

            migrationBuilder.RenameColumn(
                name: "Area",
                table: "Trabajadores",
                newName: "PrimerNombre");

            migrationBuilder.RenameColumn(
                name: "ApellidoPaterno",
                table: "Trabajadores",
                newName: "PrimerApellido");

            migrationBuilder.AlterColumn<DateOnly>(
                name: "FechaNacimiento",
                table: "Trabajadores",
                type: "date",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AddColumn<int>(
                name: "AreaId",
                table: "Trabajadores",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "DireccionCorporativaId",
                table: "Trabajadores",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "DireccionDomicilio",
                table: "Trabajadores",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "EsCuentaGenerica",
                table: "Trabajadores",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateOnly>(
                name: "FechaIncorporacion",
                table: "Trabajadores",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));

            migrationBuilder.AddColumn<string>(
                name: "HomologarAccesosDesde",
                table: "Trabajadores",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "JefeDirecto",
                table: "Trabajadores",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LugarTrabajoId",
                table: "Trabajadores",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "SegundoApellido",
                table: "Trabajadores",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SegundoNombre",
                table: "Trabajadores",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "SolicitaTelefono",
                table: "Trabajadores",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "TieneTelefonoCorporativo",
                table: "Trabajadores",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AreaId",
                table: "Trabajadores");

            migrationBuilder.DropColumn(
                name: "DireccionCorporativaId",
                table: "Trabajadores");

            migrationBuilder.DropColumn(
                name: "DireccionDomicilio",
                table: "Trabajadores");

            migrationBuilder.DropColumn(
                name: "EsCuentaGenerica",
                table: "Trabajadores");

            migrationBuilder.DropColumn(
                name: "FechaIncorporacion",
                table: "Trabajadores");

            migrationBuilder.DropColumn(
                name: "HomologarAccesosDesde",
                table: "Trabajadores");

            migrationBuilder.DropColumn(
                name: "JefeDirecto",
                table: "Trabajadores");

            migrationBuilder.DropColumn(
                name: "LugarTrabajoId",
                table: "Trabajadores");

            migrationBuilder.DropColumn(
                name: "SegundoApellido",
                table: "Trabajadores");

            migrationBuilder.DropColumn(
                name: "SegundoNombre",
                table: "Trabajadores");

            migrationBuilder.DropColumn(
                name: "SolicitaTelefono",
                table: "Trabajadores");

            migrationBuilder.DropColumn(
                name: "TieneTelefonoCorporativo",
                table: "Trabajadores");

            migrationBuilder.RenameColumn(
                name: "Sexo",
                table: "Trabajadores",
                newName: "Nombres");

            migrationBuilder.RenameColumn(
                name: "PrimerNombre",
                table: "Trabajadores",
                newName: "Area");

            migrationBuilder.RenameColumn(
                name: "PrimerApellido",
                table: "Trabajadores",
                newName: "ApellidoPaterno");

            migrationBuilder.AlterColumn<DateTime>(
                name: "FechaNacimiento",
                table: "Trabajadores",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateOnly),
                oldType: "date");

            migrationBuilder.AddColumn<string>(
                name: "ApellidoMaterno",
                table: "Trabajadores",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaIngreso",
                table: "Trabajadores",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }
    }
}
