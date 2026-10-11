-- Permisos usados por los cambios de estado de solicitudes (aprobar, iniciar, rechazar, completar),
-- con la misma asignación que tiene la base real del equipo.
-- Idempotente: en una base que ya los tiene no cambia nada.

BEGIN;

INSERT INTO "Permisos" ("Codigo", "Descripcion")
SELECT p.codigo, p.descripcion
FROM (VALUES
    ('RevisarAprobarBloqueo', 'Aprobar una solicitud de Bloqueo (pasa a En proceso)'),
    ('RechazarSolicitud',     'Rechazar una solicitud con motivo'),
    ('MarcarCompletada',      'Iniciar y completar solicitudes')
) AS p(codigo, descripcion)
WHERE NOT EXISTS (SELECT 1 FROM "Permisos" x WHERE x."Codigo" = p.codigo);

INSERT INTO "RolPermisos" ("RolId", "PermisoId")
SELECT r."Id", p."Id"
FROM (VALUES
    ('Super Admin', 'RevisarAprobarBloqueo'),
    ('Super Admin', 'RechazarSolicitud'),
    ('Super Admin', 'MarcarCompletada'),
    ('Admin TI',    'RechazarSolicitud'),
    ('Admin TI',    'MarcarCompletada'),
    ('RRHH',        'RevisarAprobarBloqueo'),
    ('RRHH',        'RechazarSolicitud')
) AS rp(rol, permiso)
JOIN "Roles" r ON r."Nombre" = rp.rol
JOIN "Permisos" p ON p."Codigo" = rp.permiso
WHERE NOT EXISTS (SELECT 1 FROM "RolPermisos" x WHERE x."RolId" = r."Id" AND x."PermisoId" = p."Id");

COMMIT;

SELECT r."Nombre" AS rol, p."Codigo" AS permiso
FROM "RolPermisos" rp
JOIN "Roles" r ON r."Id" = rp."RolId"
JOIN "Permisos" p ON p."Id" = rp."PermisoId"
WHERE p."Codigo" IN ('RevisarAprobarBloqueo', 'RechazarSolicitud', 'MarcarCompletada')
ORDER BY r."Nombre", p."Codigo";
