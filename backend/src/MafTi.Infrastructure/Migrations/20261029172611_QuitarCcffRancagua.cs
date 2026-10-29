using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MafTi.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class QuitarCcffRancagua : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // El lugar de trabajo "CCFF Rancagua" deja de existir (definido por MAF el 29-10-2026). Al momento de
            // quitarlo solo lo tenían los usuarios de prueba del login de desarrollo: quien lo tenga pasa a
            // "Oficina Central" y luego se borra. Si alguna solicitud lo usa, se deja inactivo. Idempotente.
            migrationBuilder.Sql(@"
                UPDATE ""Trabajadores"" t SET ""LugarTrabajoId"" = o.""Id""
                FROM ""Catalogos"" r, ""Catalogos"" o
                WHERE r.""Tipo"" = 'LugarTrabajo' AND r.""Nombre"" = 'CCFF Rancagua'
                  AND o.""Tipo"" = 'LugarTrabajo' AND o.""Nombre"" = 'Oficina Central'
                  AND t.""LugarTrabajoId"" = r.""Id"";

                UPDATE ""Catalogos"" SET ""Activo"" = false
                WHERE ""Tipo"" = 'LugarTrabajo' AND ""Nombre"" = 'CCFF Rancagua';

                DELETE FROM ""Catalogos"" r
                WHERE r.""Tipo"" = 'LugarTrabajo' AND r.""Nombre"" = 'CCFF Rancagua'
                  AND NOT EXISTS (SELECT 1 FROM ""SolicitudesIngreso"" s WHERE s.""LugarTrabajoId"" = r.""Id"")
                  AND NOT EXISTS (SELECT 1 FROM ""SolicitudesModificacion"" s WHERE s.""LugarTrabajoId"" = r.""Id"");");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Migración de datos: no se revierte.
        }
    }
}
