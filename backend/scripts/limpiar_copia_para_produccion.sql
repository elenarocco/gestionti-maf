-- Limpia una COPIA de la base de desarrollo para usarla como base inicial de producción.
-- Ver docs/instalacion-base-de-datos.md. NUNCA correr sobre la base con la que trabajas (maf_ti).
--
-- Deja:   estructura, roles, permisos, catálogos, feriados, parámetros de Sostenibilidad,
--         trabajadores y usuarios reales con sus accesos, y el registro de migraciones aplicadas.
-- Quita:  usuarios de prueba del login de desarrollo (dev.*@mafchile.com) y sus trabajadores "Dev ...",
--         todas las solicitudes y su historial, y los registros de auditoría generados al probar.
--
-- Todo en una transacción: si algo falla, no se borra nada.

BEGIN;

-- 0) Protección: no correr sobre la base de trabajo.
DO $$
BEGIN
    IF current_database() = 'maf_ti' THEN
        RAISE EXCEPTION 'Este script borra datos: córrelo sobre la COPIA (maf_ti_produccion), no sobre maf_ti.';
    END IF;
END $$;

-- 1) Solicitudes de prueba y todo lo que cuelga de ellas.
--    Los accesos se conservan: solo se desliga la solicitud que los otorgó.
UPDATE "Accesos" SET "SolicitudId" = NULL WHERE "SolicitudId" IS NOT NULL;
DELETE FROM "AccesosVpn";
DELETE FROM "HistorialSolicitudes";
DELETE FROM "SolicitudDetalles";
DELETE FROM "SolicitudesBloqueo";
DELETE FROM "SolicitudesIngreso";
DELETE FROM "SolicitudesModificacion";
DELETE FROM "SolicitudesVpn";
UPDATE "Solicitudes" SET "SolicitudOrigenId" = NULL WHERE "SolicitudOrigenId" IS NOT NULL;
DELETE FROM "Solicitudes";

-- 2) Auditoría generada al probar (descargas y cambios de usuarios).
DELETE FROM "RegistrosDescarga";
DELETE FROM "CambiosUsuarioSistema";

-- 3) Usuarios de prueba del login de desarrollo y sus trabajadores ficticios.
DELETE FROM "UsuariosSistema" u
USING "Trabajadores" t
WHERE t."Id" = u."TrabajadorId" AND t."Correo" LIKE 'dev.%@mafchile.com';

DELETE FROM "Accesos" a
USING "Trabajadores" t
WHERE t."Id" = a."TrabajadorId" AND t."Correo" LIKE 'dev.%@mafchile.com';

DELETE FROM "Trabajadores" WHERE "Correo" LIKE 'dev.%@mafchile.com';

-- 4) Resumen de lo que queda (revisar antes de generar el script para infraestructura).
SELECT 'Roles' AS tabla, count(*) AS filas FROM "Roles"
UNION ALL SELECT 'Permisos', count(*) FROM "Permisos"
UNION ALL SELECT 'Catalogos', count(*) FROM "Catalogos"
UNION ALL SELECT 'Feriados', count(*) FROM "Feriados"
UNION ALL SELECT 'Trabajadores', count(*) FROM "Trabajadores"
UNION ALL SELECT 'UsuariosSistema', count(*) FROM "UsuariosSistema"
UNION ALL SELECT 'Accesos', count(*) FROM "Accesos"
UNION ALL SELECT 'Solicitudes', count(*) FROM "Solicitudes";

SELECT t."PrimerNombre" || ' ' || t."PrimerApellido" AS usuario, t."Correo", r."Nombre" AS rol
FROM "UsuariosSistema" u
JOIN "Trabajadores" t ON t."Id" = u."TrabajadorId"
JOIN "Roles" r ON r."Id" = u."RolId"
ORDER BY r."Nombre";

COMMIT;
