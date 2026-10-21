using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace MafTi.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AgregarSolicitudVpnYBandejaTi : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "TrabajadorId",
                table: "Solicitudes",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.CreateTable(
                name: "SolicitudesVpn",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SolicitudId = table.Column<int>(type: "integer", nullable: false),
                    MotivoId = table.Column<int>(type: "integer", nullable: false),
                    FormularioHabilitado = table.Column<bool>(type: "boolean", nullable: false),
                    FechaEnvioProveedor = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FechaIncorporacion = table.Column<DateOnly>(type: "date", nullable: true),
                    FechaCaducidad = table.Column<DateOnly>(type: "date", nullable: true),
                    Rut = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: true),
                    Cargo = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Nombres = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Apellidos = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Departamento = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    UbicacionFisica = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    EquipoCorporativo = table.Column<bool>(type: "boolean", nullable: true),
                    CompaniaExterna = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    CuentaCorreo = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    Justificacion = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SolicitudesVpn", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SolicitudesVpn_Catalogos_MotivoId",
                        column: x => x.MotivoId,
                        principalTable: "Catalogos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SolicitudesVpn_Solicitudes_SolicitudId",
                        column: x => x.SolicitudId,
                        principalTable: "Solicitudes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SolicitudesVpn_MotivoId",
                table: "SolicitudesVpn",
                column: "MotivoId");

            migrationBuilder.CreateIndex(
                name: "IX_SolicitudesVpn_SolicitudId",
                table: "SolicitudesVpn",
                column: "SolicitudId",
                unique: true);

            // Motivos de VPN (Catálogo tipo "MotivoVpn"). Idempotente.
            migrationBuilder.Sql(@"
                INSERT INTO ""Catalogos"" (""Tipo"", ""Nombre"", ""Activo"")
                SELECT 'MotivoVpn', m.nombre, true
                FROM (VALUES ('Acceso temporal proveedor'), ('Soporte externo')) AS m(nombre)
                WHERE NOT EXISTS (
                    SELECT 1 FROM ""Catalogos"" c WHERE c.""Tipo"" = 'MotivoVpn' AND c.""Nombre"" = m.nombre);");

            // Roles y permisos no tienen semilla en código (se cargaron en la BD): se insertan solo si faltan.
            // GestionarBandejaTI es nuevo (solo Admin TI). Los demás ya existen; se aseguran para BD nuevas.
            migrationBuilder.Sql(@"
                INSERT INTO ""Permisos"" (""Codigo"", ""Descripcion"")
                SELECT p.codigo, p.descripcion
                FROM (VALUES
                    ('GestionarBandejaTI', 'Ver la Bandeja de TI y aprobar, completar o rechazar solicitudes'),
                    ('SolicitarAccesoVPN', 'Crear solicitudes de VPN'),
                    ('CompletarFichaVPN', 'Completar la ficha VPN de una solicitud propia'),
                    ('ConsultarSolicitudesPropias', 'Ver las solicitudes creadas por uno mismo'),
                    ('RevisarAprobarBloqueo', 'Aprobar solicitudes de bloqueo')
                ) AS p(codigo, descripcion)
                WHERE NOT EXISTS (SELECT 1 FROM ""Permisos"" x WHERE x.""Codigo"" = p.codigo);

                INSERT INTO ""RolPermisos"" (""RolId"", ""PermisoId"")
                SELECT r.""Id"", p.""Id""
                FROM ""Roles"" r
                JOIN (VALUES
                    ('Admin TI', 'GestionarBandejaTI'),
                    ('Jefatura', 'SolicitarAccesoVPN'),
                    ('Jefatura', 'CompletarFichaVPN'),
                    ('Jefatura', 'ConsultarSolicitudesPropias'),
                    ('RRHH', 'RevisarAprobarBloqueo')
                ) AS a(rol, codigo) ON a.rol = r.""Nombre""
                JOIN ""Permisos"" p ON p.""Codigo"" = a.codigo
                WHERE NOT EXISTS (
                    SELECT 1 FROM ""RolPermisos"" rp WHERE rp.""RolId"" = r.""Id"" AND rp.""PermisoId"" = p.""Id"");");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Se quita solo el permiso nuevo; los demás existían antes de esta migración.
            migrationBuilder.Sql(@"
                DELETE FROM ""RolPermisos"" rp
                USING ""Permisos"" p
                WHERE rp.""PermisoId"" = p.""Id"" AND p.""Codigo"" = 'GestionarBandejaTI';
                DELETE FROM ""Permisos"" WHERE ""Codigo"" = 'GestionarBandejaTI';");

            migrationBuilder.DropTable(
                name: "SolicitudesVpn");

            migrationBuilder.Sql(@"DELETE FROM ""Catalogos"" WHERE ""Tipo"" = 'MotivoVpn';");

            // TrabajadorId vuelve a ser obligatorio: se eliminan las solicitudes VPN, que no tienen trabajador.
            migrationBuilder.Sql(@"
                DELETE FROM ""HistorialSolicitudes"" h
                USING ""Solicitudes"" s
                WHERE h.""SolicitudId"" = s.""Id"" AND s.""TrabajadorId"" IS NULL;
                DELETE FROM ""Solicitudes"" WHERE ""TrabajadorId"" IS NULL;");

            migrationBuilder.AlterColumn<int>(
                name: "TrabajadorId",
                table: "Solicitudes",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);
        }
    }
}
