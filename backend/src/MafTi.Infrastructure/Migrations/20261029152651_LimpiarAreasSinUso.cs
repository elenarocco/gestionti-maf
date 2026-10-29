using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MafTi.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class LimpiarAreasSinUso : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Antes de producción se borran las áreas que quedaron fuera de la estructura oficial (inactivas) y que
            // nadie usa. Se revisan también las columnas que guardan un área sin foránea (datos de Ingreso y
            // Modificación, área del usuario), para no dejar referencias colgando. Las que todavía tienen algo
            // asociado se quedan inactivas. Idempotente.
            migrationBuilder.Sql(@"
                DELETE FROM ""Catalogos"" a
                WHERE a.""Tipo"" = 'Area' AND NOT a.""Activo""
                  AND NOT EXISTS (SELECT 1 FROM ""Trabajadores"" t WHERE t.""AreaId"" = a.""Id"")
                  AND NOT EXISTS (SELECT 1 FROM ""SolicitudesIngreso"" s WHERE s.""AreaId"" = a.""Id"")
                  AND NOT EXISTS (SELECT 1 FROM ""SolicitudesModificacion"" s WHERE s.""AreaId"" = a.""Id"")
                  AND NOT EXISTS (SELECT 1 FROM ""UsuariosSistema"" u WHERE u.""AreaId"" = a.""Id"")
                  AND NOT EXISTS (SELECT 1 FROM ""SolicitudDetalles"" d WHERE d.""CatalogoId"" = a.""Id"")
                  AND NOT EXISTS (SELECT 1 FROM ""Accesos"" x WHERE x.""CatalogoId"" = a.""Id"");");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Migración de datos: las áreas borradas no se recrean.
        }
    }
}
