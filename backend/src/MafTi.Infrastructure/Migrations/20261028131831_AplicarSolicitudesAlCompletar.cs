using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace MafTi.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AplicarSolicitudesAlCompletar : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AccesosVpn",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SolicitudId = table.Column<int>(type: "integer", nullable: false),
                    Rut = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Nombres = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Apellidos = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Cargo = table.Column<string>(type: "text", nullable: true),
                    CompaniaExterna = table.Column<string>(type: "text", nullable: true),
                    Departamento = table.Column<string>(type: "text", nullable: true),
                    CuentaCorreo = table.Column<string>(type: "text", nullable: true),
                    Motivo = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    FechaDesde = table.Column<DateOnly>(type: "date", nullable: true),
                    FechaHasta = table.Column<DateOnly>(type: "date", nullable: true),
                    FechaRegistro = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccesosVpn", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AccesosVpn_Solicitudes_SolicitudId",
                        column: x => x.SolicitudId,
                        principalTable: "Solicitudes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SolicitudesIngreso",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SolicitudId = table.Column<int>(type: "integer", nullable: false),
                    Rut = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    PrimerNombre = table.Column<string>(type: "text", nullable: false),
                    SegundoNombre = table.Column<string>(type: "text", nullable: true),
                    PrimerApellido = table.Column<string>(type: "text", nullable: false),
                    SegundoApellido = table.Column<string>(type: "text", nullable: true),
                    FechaNacimiento = table.Column<DateOnly>(type: "date", nullable: false),
                    Sexo = table.Column<string>(type: "text", nullable: false),
                    Correo = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
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
                    table.PrimaryKey("PK_SolicitudesIngreso", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SolicitudesIngreso_Solicitudes_SolicitudId",
                        column: x => x.SolicitudId,
                        principalTable: "Solicitudes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AccesosVpn_FechaHasta",
                table: "AccesosVpn",
                column: "FechaHasta");

            migrationBuilder.CreateIndex(
                name: "IX_AccesosVpn_SolicitudId",
                table: "AccesosVpn",
                column: "SolicitudId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SolicitudesIngreso_SolicitudId",
                table: "SolicitudesIngreso",
                column: "SolicitudId",
                unique: true);

            // Datos existentes. No se borra ni desactiva ningún trabajador: los Ingresos anteriores conservan el que ya
            // tenían y solo copian sus datos a SolicitudesIngreso (para verlos y corregirlos igual que los nuevos).
            // Las VPN ya completadas pasan al registro de accesos VPN. Idempotente.
            migrationBuilder.Sql(@"
                INSERT INTO ""SolicitudesIngreso"" (""SolicitudId"", ""Rut"", ""PrimerNombre"", ""SegundoNombre"", ""PrimerApellido"",
                    ""SegundoApellido"", ""FechaNacimiento"", ""Sexo"", ""Correo"", ""DireccionCorporativaId"", ""AreaId"", ""CargoId"",
                    ""LugarTrabajoId"", ""FechaIncorporacion"", ""DireccionDomicilio"", ""JefeDirecto"", ""HomologarAccesosDesde"",
                    ""TieneTelefonoCorporativo"", ""SolicitaTelefono"")
                SELECT s.""Id"", t.""Rut"", t.""PrimerNombre"", t.""SegundoNombre"", t.""PrimerApellido"", t.""SegundoApellido"",
                    t.""FechaNacimiento"", t.""Sexo"", t.""Correo"", t.""DireccionCorporativaId"", t.""AreaId"", t.""CargoId"",
                    t.""LugarTrabajoId"", t.""FechaIncorporacion"", t.""DireccionDomicilio"", t.""JefeDirecto"", t.""HomologarAccesosDesde"",
                    t.""TieneTelefonoCorporativo"", t.""SolicitaTelefono""
                FROM ""Solicitudes"" s JOIN ""Trabajadores"" t ON t.""Id"" = s.""TrabajadorId""
                WHERE s.""Tipo"" = 'Ingreso'
                  AND NOT EXISTS (SELECT 1 FROM ""SolicitudesIngreso"" i WHERE i.""SolicitudId"" = s.""Id"");

                INSERT INTO ""AccesosVpn"" (""SolicitudId"", ""Rut"", ""Nombres"", ""Apellidos"", ""Cargo"", ""CompaniaExterna"",
                    ""Departamento"", ""CuentaCorreo"", ""Motivo"", ""FechaDesde"", ""FechaHasta"", ""FechaRegistro"")
                SELECT s.""Id"", COALESCE(v.""Rut"", ''), v.""Nombres"", COALESCE(v.""Apellidos"", ''), v.""Cargo"", v.""CompaniaExterna"",
                    v.""Departamento"", v.""CuentaCorreo"", COALESCE(c.""Nombre"", ''), v.""FechaIncorporacion"", v.""FechaCaducidad"",
                    COALESCE(v.""FechaEnvioProveedor"", now())
                FROM ""Solicitudes"" s
                JOIN ""SolicitudesVpn"" v ON v.""SolicitudId"" = s.""Id""
                LEFT JOIN ""Catalogos"" c ON c.""Id"" = v.""MotivoId""
                WHERE s.""Tipo"" = 'VPN' AND s.""Estado"" = 'Completada' AND v.""Nombres"" IS NOT NULL
                  AND NOT EXISTS (SELECT 1 FROM ""AccesosVpn"" a WHERE a.""SolicitudId"" = s.""Id"");");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AccesosVpn");

            migrationBuilder.DropTable(
                name: "SolicitudesIngreso");
        }
    }
}
