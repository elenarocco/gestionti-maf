# Permisos

El sistema autoriza por **permisos**, no por el nombre del rol. Cada rol tiene un conjunto de permisos (tabla `RolPermisos`) y:

- la **API** valida el permiso en cada llamada (`[RequierePermiso]`), sin importar lo que muestre la pantalla;
- el **frontend** usa los mismos permisos (los recibe en `GET /api/auth/yo`) para mostrar u ocultar opciones del menú y botones.

Para dar o quitar una función a un rol basta con cambiar sus permisos (pantalla **Usuarios y roles** o SQL); no hace falta tocar código.

## Qué habilita cada permiso

| Permiso | Habilita |
|---|---|
| `ConsultarTrabajadoresYAccesos` | Lista y ficha de trabajadores y sus accesos. |
| `ConsultarTodasLasSolicitudes` | Ver todas las solicitudes y el registro de Accesos VPN. |
| `ConsultarSolicitudesPropias` | Ver las solicitudes que creó el propio usuario ("Mis solicitudes"). |
| `CrearSolicitudIngreso` | Crear solicitudes de Ingreso. |
| `CrearSolicitudModificacion` | Crear solicitudes de Modificación. |
| `CrearSolicitudBloqueo` | Crear solicitudes de Bloqueo (egreso). |
| `SolicitarAccesoVPN` | Pedir un acceso VPN para un proveedor. |
| `CompletarFichaVPN` | Completar la ficha VPN de una solicitud propia ya aprobada. |
| `GestionarBandejaTI` | Bandeja de TI: aprobar, completar y rechazar solicitudes; registro de Accesos VPN. |
| `AdministrarCatalogos` | Administrar sistemas y carpetas de red. |
| `ExportarReportes` | Botón **Descargar** (Excel o PDF) en las listas. |
| `AdministrarUsuariosYRoles` | Pantalla Usuarios y roles. Quien lo tiene puede darse cualquier permiso: solo para Gestión General. |
| `VerSostenibilidad` | Pantalla Sostenibilidad. |
| `EditarParametrosSostenibilidad` | Editar los supuestos de Sostenibilidad y ver sus datos de ejemplo. |

Además del permiso, la Jefatura **solo ve datos de su área** (trabajadores, solicitudes y Dashboard). Esa regla es por rol y está en `UsuarioActual.AreaRestringida`.

## Ver qué tiene cada rol

```sql
SELECT r."Nombre" AS rol, p."Codigo" AS permiso
FROM "RolPermisos" rp
JOIN "Roles" r ON r."Id" = rp."RolId"
JOIN "Permisos" p ON p."Id" = rp."PermisoId"
ORDER BY r."Nombre", p."Codigo";
```

## Dar o quitar un permiso por SQL

```sql
-- Dar
INSERT INTO "RolPermisos" ("RolId", "PermisoId")
SELECT r."Id", p."Id" FROM "Roles" r, "Permisos" p
WHERE r."Nombre" = 'Auditoría' AND p."Codigo" = 'ExportarReportes'
  AND NOT EXISTS (SELECT 1 FROM "RolPermisos" x WHERE x."RolId" = r."Id" AND x."PermisoId" = p."Id");

-- Quitar
DELETE FROM "RolPermisos" rp USING "Roles" r, "Permisos" p
WHERE rp."RolId" = r."Id" AND rp."PermisoId" = p."Id"
  AND r."Nombre" = 'Auditoría' AND p."Codigo" = 'ExportarReportes';
```

El cambio aplica en la siguiente llamada a la API; el menú se actualiza cuando el usuario recarga la página o vuelve a iniciar sesión.
