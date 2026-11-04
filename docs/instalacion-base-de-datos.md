# Base de datos inicial para producción

La base de producción se crea a partir de una **copia** de la base de desarrollo (`maf_ti`), limpia de datos de prueba. Así producción parte con la configuración real (roles, permisos, catálogos, feriados, parámetros de Sostenibilidad, trabajadores y usuarios reales) y con el registro de migraciones, para que las migraciones futuras se apliquen normalmente.

| Queda | Se quita |
|---|---|
| Estructura de todas las tablas | Usuarios de prueba del login de desarrollo (`dev.*@mafchile.com`) y sus trabajadores "Dev ..." |
| Roles, permisos y sus asignaciones | Todas las solicitudes y su historial |
| Catálogos, feriados, parámetros de Sostenibilidad | Registros de descargas y de cambios de usuarios hechos al probar |
| Trabajadores y usuarios reales, con sus accesos | |
| Registro de migraciones (`__EFMigrationsHistory`) | |

> El script resultante contiene **datos personales** (RUT, nombres y correos de trabajadores). Compártelo solo por el canal que indique TI (por ejemplo, una carpeta con acceso restringido), no por correo ni chat.

## 1. Generar el script (desarrollo)

En Git Bash. Cambia `16` por tu versión de PostgreSQL (`C:\Program Files\PostgreSQL\`). Cada comando pide la contraseña de `postgres`.

```bash
PG="/c/Program Files/PostgreSQL/16/bin"
cd /c/Proyectos/gestionti-maf

# a) Copia de la base de trabajo. Antes: detén la API y cierra en pgAdmin las conexiones a maf_ti
#    (PostgreSQL no copia una base con conexiones abiertas).
"$PG/createdb.exe" -U postgres -T maf_ti maf_ti_produccion

# b) Limpiar la COPIA. El script se niega a correr sobre maf_ti.
"$PG/psql.exe" -U postgres -d maf_ti_produccion -v ON_ERROR_STOP=1 -f backend/scripts/limpiar_copia_para_produccion.sql
```

Al terminar muestra cuántas filas quedan por tabla y **la lista de usuarios**. Revisa que estén las personas que deben entrar en producción (al menos una con rol **Gestión General**, que podrá crear al resto desde *Usuarios y roles*). Si falta alguien, agrégalo en tu base de trabajo y repite desde el paso a.

```bash
# c) Generar el script para infraestructura
"$PG/pg_dump.exe" -U postgres -d maf_ti_produccion --no-owner --no-privileges -f gestionti_produccion.sql

# d) (Opcional) borrar la copia
"$PG/dropdb.exe" -U postgres maf_ti_produccion
```

## 2. Cargarlo en el servidor (infraestructura)

En una base **vacía**, con el usuario que usará la API:

```bash
createdb -U <usuario> maf_ti
psql -U <usuario> -d maf_ti -v ON_ERROR_STOP=1 -f gestionti_produccion.sql
```

Después, cada versión nueva del backend que traiga migraciones se aplica con:

```bash
dotnet ef database update --project src/MafTi.Infrastructure --startup-project src/MafTi.Api
```

(o con el script SQL equivalente: `dotnet ef migrations script --idempotent`).

## Importante en producción

- La API debe correr con `ASPNETCORE_ENVIRONMENT=Production`. En `Development` crea automáticamente usuarios de prueba (`dev.*`) para el login de desarrollo, que **no** deben existir en producción.
- La cadena de conexión y demás secretos van como variables de entorno: ver [CONFIGURACION.md](../backend/CONFIGURACION.md).
- Los feriados cargados cubren 2026 y 2027; los años siguientes hay que agregarlos.
