# Sistema de Solicitudes de Usuario TI — MAF Chile

Sistema web que digitaliza el ciclo de vida de un usuario de TI (Ingreso, Modificación, Bloqueo y acceso VPN), reemplazando las fichas manuales en papel/Excel. Desarrollado como práctica profesional.

## Stack tecnológico
- **Frontend:** Angular (versión a confirmar: 20 o 21)
- **Backend:** .NET 10
- **Base de datos:** SQL Server
- **Autenticación:** Active Directory
- **Repositorio:** GitHub

## Estructura del proyecto frontend/           → proyecto Angular
backend/
  src/
    MafTi.Api/           → controladores, punto de entrada
    MafTi.Application/   → reglas de negocio
    MafTi.Domain/        → entidades del dominio
    MafTi.Infrastructure/→ acceso a datos (SQL Server)
  tests/           → pruebas automatizadas
docs/              → informe de alcance y diagramas

## Ramas
- `main` — código estable
- `develop` — integración de funcionalidades
- `feature/*` — una rama por tarea

## Estado del proyecto
Ver tablero: [maf-ti-solicitudes] https://github.com/junkheadl/gestionti-maf
