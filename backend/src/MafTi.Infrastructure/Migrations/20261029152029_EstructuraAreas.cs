using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MafTi.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class EstructuraAreas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Estructura oficial de áreas por dirección corporativa (definida por MAF el 29-10-2026).
            // 1) La dirección "Presidencia" pasa a llamarse "Executive Committee" y su área es "Presidencia".
            // 2) Cada área oficial se obtiene renombrando la que ya existía en esa dirección (así conserva su Id y
            //    a quien ya estaba en ella) o, si no había equivalente, se crea.
            // 3) Quedan activas solo las áreas oficiales; las demás (incluidas las antiguas sin dirección) se
            //    desactivan: no se borran porque pueden tener trabajadores o historial. Idempotente.
            migrationBuilder.Sql(@"
                UPDATE ""Catalogos"" SET ""Nombre"" = 'Executive Committee'
                WHERE ""Tipo"" = 'DireccionCorporativa' AND ""Nombre"" = 'Presidencia';

                CREATE TEMP TABLE areas_oficiales (direccion text, nombre text, antiguo text) ON COMMIT DROP;
                INSERT INTO areas_oficiales VALUES
                    ('Dirección de Operaciones', 'Riesgo y Admisión', 'Riesgo'),
                    ('Dirección de Operaciones', 'Cobranza, Gestión de Activos y Garantías', 'Cobranza Gestión de Activos y Garantías'),
                    ('Dirección de Finanzas', 'TI', NULL),
                    ('Dirección de Finanzas', 'Finanzas y Contabilidad', 'Finanzas'),
                    ('Dirección Comercial', 'Nuevos Negocios', NULL),
                    ('Dirección Comercial', 'Comercial Toyota', NULL),
                    ('Dirección Comercial', 'Comercial', NULL),
                    ('Dirección Comercial', 'Seguros', NULL),
                    ('Dirección Comercial', 'Marketing y Desarrollo de Negocios', 'Marketing'),
                    ('Legal', 'Control Interno', NULL),
                    ('Gestión de Personas y Administración', 'Gestión de Personas y Administración', 'Administración'),
                    ('Deputy COO | Control Interno | BI & Control', 'Business Intelligence and Control', 'Data y Analytics'),
                    ('Executive Committee', 'Presidencia', 'Executive Committee');

                UPDATE ""Catalogos"" a SET ""Nombre"" = o.nombre
                FROM areas_oficiales o
                JOIN ""Catalogos"" d ON d.""Tipo"" = 'DireccionCorporativa' AND d.""Nombre"" = o.direccion
                WHERE a.""Tipo"" = 'Area' AND a.""PadreId"" = d.""Id"" AND a.""Nombre"" = o.antiguo
                  AND NOT EXISTS (SELECT 1 FROM ""Catalogos"" x
                                  WHERE x.""Tipo"" = 'Area' AND x.""PadreId"" = d.""Id"" AND x.""Nombre"" = o.nombre);

                INSERT INTO ""Catalogos"" (""Tipo"", ""Nombre"", ""Activo"", ""PadreId"")
                SELECT 'Area', o.nombre, true, d.""Id""
                FROM areas_oficiales o
                JOIN ""Catalogos"" d ON d.""Tipo"" = 'DireccionCorporativa' AND d.""Nombre"" = o.direccion
                WHERE NOT EXISTS (SELECT 1 FROM ""Catalogos"" x
                                  WHERE x.""Tipo"" = 'Area' AND x.""PadreId"" = d.""Id"" AND x.""Nombre"" = o.nombre);

                UPDATE ""Catalogos"" a SET ""Activo"" = EXISTS (
                    SELECT 1 FROM areas_oficiales o
                    JOIN ""Catalogos"" d ON d.""Tipo"" = 'DireccionCorporativa' AND d.""Nombre"" = o.direccion
                    WHERE a.""PadreId"" = d.""Id"" AND a.""Nombre"" = o.nombre)
                WHERE a.""Tipo"" = 'Area';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Migración de datos: no se revierte (las áreas desactivadas siguen en la tabla y se pueden reactivar).
        }
    }
}
