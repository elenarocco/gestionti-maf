-- Datos de PRUEBA para levantar una base local vacía (creada con `dotnet ef database update`).
-- Todo es ficticio: no reemplaza la base real del equipo.
-- La matriz rol-permiso es PROVISIONAL (según historias de usuario y casos de prueba); ajustarla con la real.
-- Se ejecuta una sola vez sobre una base sin datos.

BEGIN;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "Catalogos") OR EXISTS (SELECT 1 FROM "Roles") THEN
        RAISE EXCEPTION 'La base ya tiene datos. Este script es solo para una base vacía.';
    END IF;
END $$;

-- ---------- Catálogos ----------
INSERT INTO "Catalogos" ("Tipo", "Nombre", "Activo") VALUES
    ('DireccionCorporativa', 'Dirección de Operaciones', true),
    ('DireccionCorporativa', 'Dirección de Finanzas', true),
    ('DireccionCorporativa', 'Dirección Comercial', true),
    ('DireccionCorporativa', 'Legal', true),
    ('DireccionCorporativa', 'Gestión de Personas y Administración', true),
    ('DireccionCorporativa', 'Deputy COO / Control Interno / BI & Control', true),
    ('LugarTrabajo', 'Oficina Central', true),
    ('LugarTrabajo', 'Centro de Financiamiento (Regiones)', true),
    ('LugarTrabajo', 'Centro de Financiamiento (Santiago)', true),
    ('Cargo', 'Analista', true),
    ('Cargo', 'Ejecutivo Comercial', true),
    ('Cargo', 'Jefe de Área', true),
    ('Cargo', 'Asistente', true),
    ('Sistema', 'SAP', true),
    ('Sistema', 'Salesforce', true),
    ('Sistema', 'Office 365', true),
    ('Carpeta', '\\srv-archivos\finanzas', true),
    ('Carpeta', '\\srv-archivos\comercial', true);

-- Áreas colgando de su dirección (PadreId)
INSERT INTO "Catalogos" ("Tipo", "Nombre", "Activo", "PadreId")
SELECT 'Area', a.nombre, true, d."Id"
FROM (VALUES
    ('TI', 'Dirección de Operaciones'),
    ('Operaciones', 'Dirección de Operaciones'),
    ('Contabilidad', 'Dirección de Finanzas'),
    ('Riesgo', 'Dirección de Finanzas'),
    ('Comercial', 'Dirección Comercial'),
    ('Amicar', 'Dirección Comercial'),
    ('Fiscalía', 'Legal'),
    ('Recursos Humanos', 'Gestión de Personas y Administración'),
    ('BI & Control', 'Deputy COO / Control Interno / BI & Control')
) AS a(nombre, direccion)
JOIN "Catalogos" d ON d."Tipo" = 'DireccionCorporativa' AND d."Nombre" = a.direccion;

-- Área "antigua" sin dirección: reproduce el caso de los duplicados que quedaron en la base real
INSERT INTO "Catalogos" ("Tipo", "Nombre", "Activo") VALUES ('Area', 'Comercial (antigua)', true);

-- ---------- Roles y permisos ----------
INSERT INTO "Roles" ("Nombre", "Descripcion") VALUES
    ('Super Admin', 'Acceso total'),
    ('Admin TI', 'Ejecuta y cierra solicitudes'),
    ('Jefatura', 'Crea solicitudes de su área'),
    ('RRHH', 'Revisa bloqueos'),
    ('Auditoría', 'Solo lectura');

INSERT INTO "Permisos" ("Codigo", "Descripcion") VALUES
    ('CrearSolicitudIngreso', 'Crear solicitud de Ingreso'),
    ('CrearSolicitudModificacion', 'Crear solicitud de Modificación'),
    ('CrearSolicitudBloqueo', 'Crear solicitud de Bloqueo'),
    ('SolicitarAccesoVPN', 'Solicitar acceso VPN'),
    ('ConsultarTodasLasSolicitudes', 'Ver solicitudes de todos los tipos'),
    ('ConsultarTrabajadoresYAccesos', 'Ver trabajadores y sus accesos'),
    ('AdministrarCatalogos', 'Agregar, editar y desactivar sistemas y carpetas');

INSERT INTO "RolPermisos" ("RolId", "PermisoId")
SELECT r."Id", p."Id"
FROM (VALUES
    ('Super Admin', 'CrearSolicitudIngreso'),
    ('Super Admin', 'CrearSolicitudModificacion'),
    ('Super Admin', 'CrearSolicitudBloqueo'),
    ('Super Admin', 'SolicitarAccesoVPN'),
    ('Super Admin', 'ConsultarTodasLasSolicitudes'),
    ('Super Admin', 'ConsultarTrabajadoresYAccesos'),
    ('Super Admin', 'AdministrarCatalogos'),
    ('Admin TI', 'ConsultarTodasLasSolicitudes'),
    ('Admin TI', 'ConsultarTrabajadoresYAccesos'),
    ('Admin TI', 'AdministrarCatalogos'),
    ('Jefatura', 'CrearSolicitudIngreso'),
    ('Jefatura', 'CrearSolicitudModificacion'),
    ('Jefatura', 'SolicitarAccesoVPN'),
    ('Jefatura', 'ConsultarTrabajadoresYAccesos'),
    ('RRHH', 'CrearSolicitudBloqueo'),
    ('RRHH', 'ConsultarTrabajadoresYAccesos'),
    ('Auditoría', 'ConsultarTodasLasSolicitudes'),
    ('Auditoría', 'ConsultarTrabajadoresYAccesos')
) AS rp(rol, permiso)
JOIN "Roles" r ON r."Nombre" = rp.rol
JOIN "Permisos" p ON p."Codigo" = rp.permiso;

-- ---------- Trabajadores (RUT con dígito verificador válido) ----------
INSERT INTO "Trabajadores" (
    "Rut", "PrimerNombre", "SegundoNombre", "PrimerApellido", "SegundoApellido",
    "FechaNacimiento", "Sexo", "Correo", "DireccionCorporativaId", "AreaId", "LugarTrabajoId", "CargoId",
    "EsCuentaGenerica", "FechaIncorporacion", "FechaSalida", "TieneTelefonoCorporativo", "SolicitaTelefono", "Activo")
SELECT t.rut, t.nombre, NULL, t.apellido, t.materno, t.nacimiento::date, t.sexo, t.correo,
       a."PadreId", a."Id", l."Id", c."Id",
       false, t.ingreso::date, t.salida::date, false, false, t.activo
FROM (VALUES
    -- rut, nombre, apellido, materno, nacimiento, sexo, correo, área, cargo, ingreso, salida, activo
    ('12345678-5', 'Carla',  'Muñoz',  'Rojas',   '1985-03-12', 'Femenino',  'cmunoz@mafchile.com',  'TI',               'Jefe de Área',       '2020-01-06', NULL,         true),
    ('15678234-3', 'Juan',   'Pérez',  'González', '1990-07-01', 'Masculino', 'jperez@mafchile.com',  'Comercial',        'Ejecutivo Comercial','2022-05-02', NULL,         true),
    ('16789012-1', 'Daniela','Soto',   NULL,      '1993-11-20', 'Femenino',  'dsoto@mafchile.com',   'Recursos Humanos', 'Analista',           '2021-09-13', NULL,         true),
    ('17890123-0', 'Pedro',  'Lagos',  'Vera',    '1988-02-28', 'Masculino', 'plagos@mafchile.com',  'Contabilidad',     'Analista',           '2019-04-01', NULL,         true),
    -- Inactivo: sirve para probar la reincorporación (debe reutilizar su correo)
    ('18901234-9', 'Ana',    'Reyes',  'Díaz',    '1995-06-15', 'Femenino',  'areyes@mafchile.com',  'Riesgo',           'Asistente',          '2021-01-04', '2025-12-31', false)
) AS t(rut, nombre, apellido, materno, nacimiento, sexo, correo, area, cargo, ingreso, salida, activo)
JOIN "Catalogos" a ON a."Tipo" = 'Area' AND a."Nombre" = t.area
JOIN "Catalogos" l ON l."Tipo" = 'LugarTrabajo' AND l."Nombre" = 'Oficina Central'
JOIN "Catalogos" c ON c."Tipo" = 'Cargo' AND c."Nombre" = t.cargo;

-- Trabajador apuntando al área antigua: Modificación debería fallar hasta migrarlo
INSERT INTO "Trabajadores" (
    "Rut", "PrimerNombre", "PrimerApellido", "FechaNacimiento", "Sexo", "Correo",
    "DireccionCorporativaId", "AreaId", "LugarTrabajoId", "CargoId",
    "EsCuentaGenerica", "FechaIncorporacion", "TieneTelefonoCorporativo", "SolicitaTelefono", "Activo")
SELECT '19012345-6', 'Luis', 'Fuentes', '1991-09-09', 'Masculino', 'lfuentes@mafchile.com',
       d."Id", a."Id", l."Id", c."Id", false, '2023-03-01', false, false, true
FROM "Catalogos" d, "Catalogos" a, "Catalogos" l, "Catalogos" c
WHERE d."Tipo" = 'DireccionCorporativa' AND d."Nombre" = 'Dirección Comercial'
  AND a."Tipo" = 'Area' AND a."Nombre" = 'Comercial (antigua)'
  AND l."Tipo" = 'LugarTrabajo' AND l."Nombre" = 'Oficina Central'
  AND c."Tipo" = 'Cargo' AND c."Nombre" = 'Ejecutivo Comercial';

-- ---------- Usuarios del sistema ----------
-- El frontend usa creadoPorId = 1: el primer usuario insertado (Carla, Jefatura de TI) toma ese Id en una base vacía.
INSERT INTO "UsuariosSistema" ("TrabajadorId", "RolId", "AreaId", "Activo")
SELECT t."Id", r."Id", CASE WHEN u.rol = 'Jefatura' THEN t."AreaId" END, true
FROM (VALUES
    ('12345678-5', 'Jefatura'),
    ('16789012-1', 'RRHH'),
    ('17890123-0', 'Auditoría')
) AS u(rut, rol)
JOIN "Trabajadores" t ON t."Rut" = u.rut
JOIN "Roles" r ON r."Nombre" = u.rol;

-- ---------- Accesos vigentes (para la precarga del Bloqueo y Modificación) ----------
INSERT INTO "Accesos" ("TrabajadorId", "CatalogoId", "FechaOtorgado", "Estado")
SELECT t."Id", c."Id", now(), 'Activo'
FROM (VALUES
    ('15678234-3', 'Salesforce'),
    ('15678234-3', 'Office 365'),
    ('15678234-3', '\\srv-archivos\comercial'),
    ('17890123-0', 'SAP'),
    ('17890123-0', '\\srv-archivos\finanzas')
) AS x(rut, catalogo)
JOIN "Trabajadores" t ON t."Rut" = x.rut
JOIN "Catalogos" c ON c."Nombre" = x.catalogo;

COMMIT;

-- Verificación rápida
SELECT 'Catalogos' AS tabla, count(*) FROM "Catalogos"
UNION ALL SELECT 'Roles', count(*) FROM "Roles"
UNION ALL SELECT 'RolPermisos', count(*) FROM "RolPermisos"
UNION ALL SELECT 'Trabajadores', count(*) FROM "Trabajadores"
UNION ALL SELECT 'UsuariosSistema', count(*) FROM "UsuariosSistema"
UNION ALL SELECT 'Accesos', count(*) FROM "Accesos";
