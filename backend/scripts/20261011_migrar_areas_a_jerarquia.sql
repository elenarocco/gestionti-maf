-- Migra trabajadores (y usuarios Jefatura) desde las áreas antiguas, sin dirección (PadreId NULL),
-- a las áreas nuevas que cuelgan de cada dirección corporativa.
--
-- 1) Automático: área nueva con el MISMO nombre dentro de la dirección del trabajador.
-- 2) Manual: equivalencias decididas por el equipo/MAF, escritas por nombre en la sección 2.
-- 3) Las áreas antiguas que quedan sin trabajadores ni usuarios se desactivan (no se borran,
--    porque solicitudes antiguas pueden seguir apuntándolas).
--
-- Idempotente: se puede ejecutar de nuevo cada vez que se agreguen equivalencias manuales.

BEGIN;

CREATE TEMP TABLE equivalencia_area (
    area_antigua_id integer NOT NULL,
    direccion_id    integer NOT NULL,
    area_nueva_id   integer NOT NULL
) ON COMMIT DROP;

-- 1) Automático: mismo nombre en la misma dirección
INSERT INTO equivalencia_area
SELECT DISTINCT a."Id", t."DireccionCorporativaId", n."Id"
FROM "Trabajadores" t
JOIN "Catalogos" a ON a."Id" = t."AreaId" AND a."PadreId" IS NULL
JOIN "Catalogos" n ON n."Tipo" = 'Area' AND n."PadreId" = t."DireccionCorporativaId" AND n."Nombre" = a."Nombre";

-- 2) Manual: agregar una fila por decisión ('Área antigua', 'Dirección del trabajador', 'Área nueva').
--    Los nombres deben escribirse exactamente como están en Catalogos.
INSERT INTO equivalencia_area
SELECT a."Id", d."Id", n."Id"
FROM (VALUES
    (NULL::text, NULL::text, NULL::text)   -- fila vacía para que la lista nunca quede sin filas
    -- , ('Executive Comité', 'Presidencia', 'Executive Committee')
) AS m(area_antigua, direccion, area_nueva)
JOIN "Catalogos" d ON d."Tipo" = 'DireccionCorporativa' AND d."Nombre" = m.direccion
JOIN "Catalogos" a ON a."Tipo" = 'Area' AND a."PadreId" IS NULL AND a."Nombre" = m.area_antigua
JOIN "Catalogos" n ON n."Tipo" = 'Area' AND n."PadreId" = d."Id" AND n."Nombre" = m.area_nueva;

-- 3) Aplicar a usuarios Jefatura (antes que a trabajadores, porque se cruza por el área antigua)
UPDATE "UsuariosSistema" u
SET "AreaId" = e.area_nueva_id
FROM "Trabajadores" t, equivalencia_area e
WHERE u."TrabajadorId" = t."Id"
  AND u."AreaId" = e.area_antigua_id
  AND t."DireccionCorporativaId" = e.direccion_id;

UPDATE "Trabajadores" t
SET "AreaId" = e.area_nueva_id
FROM equivalencia_area e
WHERE t."AreaId" = e.area_antigua_id
  AND t."DireccionCorporativaId" = e.direccion_id;

-- 4) Desactivar áreas antiguas que ya no usa nadie
UPDATE "Catalogos" a
SET "Activo" = false
WHERE a."Tipo" = 'Area' AND a."PadreId" IS NULL AND a."Activo"
  AND NOT EXISTS (SELECT 1 FROM "Trabajadores" t WHERE t."AreaId" = a."Id")
  AND NOT EXISTS (SELECT 1 FROM "UsuariosSistema" u WHERE u."AreaId" = a."Id");

COMMIT;

-- Pendientes: trabajadores que siguen en un área sin equivalencia (falta decidirlos en la sección 2)
SELECT a."Nombre" AS area_antigua, d."Nombre" AS direccion, count(*) AS trabajadores
FROM "Trabajadores" t
JOIN "Catalogos" a ON a."Id" = t."AreaId"
JOIN "Catalogos" d ON d."Id" = t."DireccionCorporativaId"
WHERE a."PadreId" IS DISTINCT FROM t."DireccionCorporativaId"
GROUP BY a."Nombre", d."Nombre"
ORDER BY count(*) DESC, a."Nombre";
