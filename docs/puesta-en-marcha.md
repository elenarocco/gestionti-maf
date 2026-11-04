# Puesta en marcha

Cómo levantar GestiónTI en un equipo de desarrollo. Los secretos (cadena de conexión, clave del login de desarrollo) **no van en el repositorio**: ver [CONFIGURACION.md](../backend/CONFIGURACION.md).

## Requisitos

| Herramienta | Versión |
|---|---|
| .NET SDK | 10 |
| PostgreSQL | 16 o superior |
| Node.js | 22.22.3 o superior, o 24 (lo exige el Angular CLI 22) |
| Herramienta de migraciones | `dotnet tool install --global dotnet-ef` |

## Backend (`gestionti-maf`)

```bash
cd backend/src/MafTi.Api

# Una sola vez: secretos de desarrollo (quedan en tu usuario, fuera del repositorio)
dotnet user-secrets set "ConnectionStrings:Default" "Host=localhost;Port=5432;Database=maf_ti;Username=postgres;Password=TU_CONTRASEÑA"
dotnet user-secrets set "AuthDesarrollo:Clave" "una-clave-larga-de-al-menos-32-caracteres"

# Crear o actualizar la base (desde backend/)
cd ../..
dotnet ef database update --project src/MafTi.Infrastructure --startup-project src/MafTi.Api

# Levantar la API (queda en http://localhost:5153)
dotnet run --project src/MafTi.Api
```

En Development, al arrancar, la API crea un usuario de prueba por cada rol que no tenga usuario activo (`dev.<rol>@mafchile.com`). Con ellos funciona el **login de desarrollo** del frontend.

Antes de aplicar migraciones en una base con datos, respáldala (en pgAdmin: clic derecho sobre la base → *Backup…*, formato *Custom*).

## Frontend (`gestion-maf-frontend`)

```bash
npm ci
npx ng serve        # http://localhost:4200
```

Con `ng serve` (configuración development) la pantalla de login muestra, además del botón de Microsoft, un selector **"Entrar como"** con los usuarios de prueba de cada rol. En producción ese selector no existe.

La URL de la API y los datos de Entra ID están en `src/environments/environment.ts` (producción) y `environment.development.ts`.

## Tests

| Repositorio | Comando | Qué cubre |
|---|---|---|
| Backend | `dotnet test` (desde `backend/`) | Reglas de negocio: validaciones, SLA con feriados, flujo de estados, VPN, usuarios, Sostenibilidad |
| Frontend | `npx ng test --watch=false` | Creación de pantallas y menú lateral según permisos |

Para crear una pantalla en un test del frontend, usa `proveedoresDePrueba()` de `src/testing/proveedores-prueba.ts` (HTTP simulado, rutas y MSAL simulado).

## Revisión automática (CI)

Cada push y cada Pull Request ejecuta GitHub Actions (`.github/workflows/ci.yml` en cada repositorio):

- **Backend:** compila, ejecuta los tests y verifica que no falte ninguna migración (falla si alguien cambió una entidad o el `AppDbContext` sin crear la migración).
- **Frontend:** build de producción (incluye los límites de tamaño de `angular.json`) y tests.

El resultado se ve en la pestaña **Actions** de GitHub y en cada Pull Request.

## Nueva migración

```bash
cd backend
dotnet ef migrations add NombreDescriptivo --project src/MafTi.Infrastructure --startup-project src/MafTi.Api
```

Si la migración carga datos (permisos, parámetros, catálogos), escribe el SQL para que se pueda ejecutar más de una vez sin duplicar (`WHERE NOT EXISTS ...`), como en las migraciones existentes.
