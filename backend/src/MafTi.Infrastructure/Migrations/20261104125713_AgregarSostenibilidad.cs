using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MafTi.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AgregarSostenibilidad : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ParametrosSostenibilidad",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false),
                    HojasIngreso = table.Column<int>(type: "integer", nullable: false),
                    HojasModificacion = table.Column<int>(type: "integer", nullable: false),
                    HojasBloqueo = table.Column<int>(type: "integer", nullable: false),
                    HojasVpn = table.Column<int>(type: "integer", nullable: false),
                    CopiasImpresas = table.Column<int>(type: "integer", nullable: false),
                    GramosCO2PorHoja = table.Column<decimal>(type: "numeric(10,3)", precision: 10, scale: 3, nullable: false),
                    KbTransferidosPorSolicitud = table.Column<decimal>(type: "numeric(12,1)", precision: 12, scale: 1, nullable: false),
                    GramosCO2PorMB = table.Column<decimal>(type: "numeric(10,3)", precision: 10, scale: 3, nullable: false),
                    FechaActualizacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ParametrosSostenibilidad", x => x.Id);
                });

            // Valores iniciales (ESTIMACIONES; se editan desde la pantalla Sostenibilidad):
            // - Hojas por formulario = 1 en los cuatro tipos y 1 copia impresa: confirmado por la jefatura
            //   (2026-11-04), cada formulario en papel era una sola hoja y circulaba en mano para las firmas.
            // - GramosCO2PorHoja = 4,7 g CO2e por hoja A4: estimación de ciclo de vida del papel (fabricación y
            //   desecho) entregada como valor inicial; la fuente exacta está pendiente de confirmar.
            // - KbTransferidosPorSolicitud = 823 KB: medido el 2026-11-04 con un navegador automatizado
            //   (Chromium) sobre el flujo completo de UNA solicitud de Ingreso, cada usuario con caché vacía:
            //   RRHH inicia sesión y crea la solicitud (415,7 KB) + Admin TI inicia sesión, la revisa en la Bandeja
            //   de TI, la aprueba y la marca completada (407,3 KB). Front compilado para producción y servido con
            //   gzip; API sin compresión (86,5 KB entre ambos, contados en el servidor con un proxy). Incluye HTML,
            //   JS, CSS, logo y fuentes de Google (estas suman 215,8 KB). No incluye el login de Microsoft Entra ID (se usó el
            //   login de desarrollo). 3 repeticiones con el mismo resultado. Volver a medir si se agregan pantallas.
            // - GramosCO2PorMB = 0,6 g CO2e por MB transferido: basado en el modelo Sustainable Web Design
            //   (energía de red, centros de datos y dispositivos por dato transferido). 1 MB = 1.000 KB.
            migrationBuilder.Sql(@"
                INSERT INTO ""ParametrosSostenibilidad""
                    (""Id"", ""HojasIngreso"", ""HojasModificacion"", ""HojasBloqueo"", ""HojasVpn"", ""CopiasImpresas"",
                     ""GramosCO2PorHoja"", ""KbTransferidosPorSolicitud"", ""GramosCO2PorMB"", ""FechaActualizacion"")
                SELECT 1, 1, 1, 1, 1, 1, 4.7, 823, 0.6, now()
                WHERE NOT EXISTS (SELECT 1 FROM ""ParametrosSostenibilidad"" WHERE ""Id"" = 1);");

            // Permisos del módulo. Ver: Admin TI, Auditoría y Gestión General. Editar los supuestos y ver los datos
            // de ejemplo: solo Gestión General (antes "Super Admin"; se incluyen ambos nombres). Idempotente.
            migrationBuilder.Sql(@"
                INSERT INTO ""Permisos"" (""Codigo"", ""Descripcion"")
                SELECT v.codigo, v.descripcion FROM (VALUES
                    ('VerSostenibilidad', 'Ver el módulo Sostenibilidad (papel evitado vs. huella del sistema)'),
                    ('EditarParametrosSostenibilidad', 'Editar los supuestos de Sostenibilidad y ver sus datos de ejemplo')
                ) AS v(codigo, descripcion)
                WHERE NOT EXISTS (SELECT 1 FROM ""Permisos"" p WHERE p.""Codigo"" = v.codigo);

                INSERT INTO ""RolPermisos"" (""RolId"", ""PermisoId"")
                SELECT r.""Id"", p.""Id""
                FROM ""Roles"" r JOIN ""Permisos"" p ON p.""Codigo"" = 'VerSostenibilidad'
                WHERE r.""Nombre"" IN ('Admin TI', 'Auditoría', 'Gestión General', 'Super Admin')
                  AND NOT EXISTS (SELECT 1 FROM ""RolPermisos"" rp WHERE rp.""RolId"" = r.""Id"" AND rp.""PermisoId"" = p.""Id"");

                INSERT INTO ""RolPermisos"" (""RolId"", ""PermisoId"")
                SELECT r.""Id"", p.""Id""
                FROM ""Roles"" r JOIN ""Permisos"" p ON p.""Codigo"" = 'EditarParametrosSostenibilidad'
                WHERE r.""Nombre"" IN ('Gestión General', 'Super Admin')
                  AND NOT EXISTS (SELECT 1 FROM ""RolPermisos"" rp WHERE rp.""RolId"" = r.""Id"" AND rp.""PermisoId"" = p.""Id"");");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DELETE FROM ""RolPermisos"" rp USING ""Permisos"" p
                WHERE rp.""PermisoId"" = p.""Id"" AND p.""Codigo"" IN ('VerSostenibilidad', 'EditarParametrosSostenibilidad');
                DELETE FROM ""Permisos"" WHERE ""Codigo"" IN ('VerSostenibilidad', 'EditarParametrosSostenibilidad');");

            migrationBuilder.DropTable(
                name: "ParametrosSostenibilidad");
        }
    }
}
