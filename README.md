# GestiónTI — Sistema de Solicitudes de Usuario TI · MAF Chile

Sistema web que digitaliza el ciclo de vida de un usuario de TI: **Ingreso**, **Modificación**, **Bloqueo** (egreso) y **acceso VPN** para proveedores. Reemplaza los formularios en papel que circulaban en mano para reunir las firmas de las jefaturas. Desarrollado como práctica profesional.

## Stack

| Parte | Tecnología |
|---|---|
| Frontend | Angular 22 (componentes standalone, signals) — repositorio [gestion-maf-frontend](https://github.com/elenarocco/gestion-maf-frontend) |
| Backend | .NET 10, API en capas — este repositorio |
| Base de datos | PostgreSQL con EF Core (Code First, migraciones) |
| Autenticación | Microsoft Entra ID (cuentas corporativas); login de desarrollo solo en Development |
| Revisión automática | GitHub Actions en ambos repositorios ([CI](.github/workflows/ci.yml)) |

## Estructura

```
backend/
  src/
    MafTi.Api/             → controllers, DTOs, seguridad (permisos, sesión, cabeceras HTTP)
    MafTi.Application/     → reglas de negocio puras (validaciones, SLA, flujo de estados, Sostenibilidad)
    MafTi.Domain/          → entidades
    MafTi.Infrastructure/  → AppDbContext y migraciones
  tests/MafTi.Tests/       → tests de las reglas de negocio (xUnit)
  CONFIGURACION.md         → secretos y variables de entorno (desarrollo y servidor)
docs/                      → documentación funcional y técnica
```

Principio del proyecto: **toda la lógica y las validaciones viven en el backend**. El frontend solo muestra lo que la API decide (incluidas las acciones permitidas en cada solicitud) y la API vuelve a validar cada llamada.

## Documentación

- [Puesta en marcha](docs/puesta-en-marcha.md): cómo levantar el proyecto en tu equipo, tests y CI.
- [Flujo de solicitudes](docs/flujo-de-solicitudes.md): estados, acciones de TI, plazos (SLA) y qué cambia en la base al completar.
- [Permisos](docs/permisos.md): qué habilita cada permiso y cómo revisar o cambiar lo que tiene cada rol.
- [Sostenibilidad](docs/sostenibilidad.md): cálculo de papel evitado vs. huella del sistema, supuestos y cómo se midieron.
- [Base de datos inicial para producción](docs/instalacion-base-de-datos.md): copia limpia de la base de desarrollo, sin datos de prueba.
- [Configuración y secretos](backend/CONFIGURACION.md).

## Ramas

Mismas ramas en los dos repositorios:

| Rama | Para qué |
|---|---|
| `main` | Versión entregada (estable). |
| `develop` | Integración: lo que ya está probado y listo para la próxima entrega. |
| `feature/antonia` (backend) / `feature/antonia-v2` (frontend) | Trabajo en curso. |

Flujo: se trabaja en la rama de trabajo; cuando una versión está estable (CI en verde) pasa a `develop`, y en cada entrega `develop` pasa a `main`.
