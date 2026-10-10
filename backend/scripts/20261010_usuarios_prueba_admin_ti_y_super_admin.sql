-- Agrega usuarios de PRUEBA para los roles "Admin TI" y "Super Admin" (datos ficticios).
-- Mientras no exista el login con AD, el backend registra cada acción a nombre del usuario
-- activo del rol simulado; sin estos usuarios, esos dos roles no pueden crear solicitudes.
-- Idempotente: se puede ejecutar más de una vez.

BEGIN;

INSERT INTO "Trabajadores" (
    "Rut", "PrimerNombre", "PrimerApellido", "SegundoApellido", "FechaNacimiento", "Sexo", "Correo",
    "DireccionCorporativaId", "AreaId", "LugarTrabajoId", "CargoId",
    "EsCuentaGenerica", "FechaIncorporacion", "TieneTelefonoCorporativo", "SolicitaTelefono", "Activo")
SELECT t.rut, t.nombre, t.apellido, t.materno, t.nacimiento::date, t.sexo, t.correo,
       a."PadreId", a."Id", l."Id", c."Id", false, t.ingreso::date, false, false, true
FROM (VALUES
    ('9876543-3',  'Tomás',     'Herrera', 'Silva', '1987-04-22', 'Masculino', 'therrera@mafchile.com', '2018-06-01'),
    ('13243546-4', 'Valentina', 'Castro',  'Mena',  '1984-12-05', 'Femenino',  'vcastro@mafchile.com',  '2017-02-15')
) AS t(rut, nombre, apellido, materno, nacimiento, sexo, correo, ingreso)
JOIN "Catalogos" a ON a."Tipo" = 'Area' AND a."Nombre" = 'TI'
JOIN "Catalogos" l ON l."Tipo" = 'LugarTrabajo' AND l."Nombre" = 'Oficina Central'
JOIN "Catalogos" c ON c."Tipo" = 'Cargo' AND c."Nombre" = 'Jefe de Área'
WHERE NOT EXISTS (SELECT 1 FROM "Trabajadores" x WHERE x."Rut" = t.rut);

INSERT INTO "UsuariosSistema" ("TrabajadorId", "RolId", "AreaId", "Activo")
SELECT t."Id", r."Id", NULL, true
FROM (VALUES
    ('9876543-3',  'Admin TI'),
    ('13243546-4', 'Super Admin')
) AS u(rut, rol)
JOIN "Trabajadores" t ON t."Rut" = u.rut
JOIN "Roles" r ON r."Nombre" = u.rol
WHERE NOT EXISTS (SELECT 1 FROM "UsuariosSistema" x WHERE x."TrabajadorId" = t."Id" AND x."RolId" = r."Id");

COMMIT;

-- Verificación: debe haber un usuario activo por cada uno de los 5 roles.
SELECT r."Nombre" AS rol, t."PrimerNombre" || ' ' || t."PrimerApellido" AS usuario, t."Correo"
FROM "UsuariosSistema" u
JOIN "Roles" r ON r."Id" = u."RolId"
JOIN "Trabajadores" t ON t."Id" = u."TrabajadorId"
WHERE u."Activo"
ORDER BY r."Nombre";
