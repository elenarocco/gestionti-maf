-- Deja el permiso "CrearSolicitudBloqueo" asignado SOLO al rol "RRHH".
-- Los datos de Roles/Permisos/RolPermisos no tienen seed en el código (no hay HasData):
-- se cargaron directo en la BD, por eso este cambio va como script SQL.
-- Idempotente: se puede ejecutar más de una vez.

BEGIN;

DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM "Roles" WHERE "Nombre" = 'RRHH') THEN
        RAISE EXCEPTION 'No existe el rol "RRHH".';
    END IF;
    IF NOT EXISTS (SELECT 1 FROM "Permisos" WHERE "Codigo" = 'CrearSolicitudBloqueo') THEN
        RAISE EXCEPTION 'No existe el permiso "CrearSolicitudBloqueo".';
    END IF;
END $$;

-- Quitar el permiso a todo rol distinto de RRHH (hoy: Jefatura).
DELETE FROM "RolPermisos" rp
USING "Roles" r, "Permisos" p
WHERE rp."RolId" = r."Id"
  AND rp."PermisoId" = p."Id"
  AND p."Codigo" = 'CrearSolicitudBloqueo'
  AND r."Nombre" <> 'RRHH';

-- Asignarlo a RRHH si aún no lo tiene.
INSERT INTO "RolPermisos" ("RolId", "PermisoId")
SELECT r."Id", p."Id"
FROM "Roles" r, "Permisos" p
WHERE r."Nombre" = 'RRHH'
  AND p."Codigo" = 'CrearSolicitudBloqueo'
  AND NOT EXISTS (
      SELECT 1 FROM "RolPermisos" rp
      WHERE rp."RolId" = r."Id" AND rp."PermisoId" = p."Id");

-- Verificación: debe devolver una sola fila, RRHH.
SELECT r."Nombre", p."Codigo"
FROM "RolPermisos" rp
JOIN "Roles" r ON r."Id" = rp."RolId"
JOIN "Permisos" p ON p."Id" = rp."PermisoId"
WHERE p."Codigo" = 'CrearSolicitudBloqueo';

COMMIT;
