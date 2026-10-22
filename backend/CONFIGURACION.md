# Configuración del backend

Los secretos **no van en `appsettings.json`** ni en ningún archivo del repositorio. Si falta alguno, la API
no arranca y muestra un mensaje que dice cuál falta y cómo configurarlo.

| Clave | Qué es | Secreto | Dónde se usa |
|---|---|---|---|
| `ConnectionStrings:Default` | Cadena de conexión a PostgreSQL | Sí | Siempre |
| `AuthDesarrollo:Clave` | Llave para firmar el token del login de desarrollo (mínimo 32 caracteres) | Sí | Solo en Development |
| `AzureAd:TenantId`, `AzureAd:ClientId`, `AzureAd:Audience` | Datos de la app en Microsoft Entra ID | No (son públicos) | Siempre; ya están en `appsettings.json` |

## En tu equipo (desarrollo)

Se usan los *user-secrets* de .NET: se guardan en tu usuario de Windows
(`%APPDATA%\Microsoft\UserSecrets\63bd35e4-188e-47c8-9e93-ecbc385a89b6\secrets.json`), fuera del repositorio.
Se configuran una sola vez, desde `backend/src/MafTi.Api`:

```bash
dotnet user-secrets set "ConnectionStrings:Default" "Host=localhost;Port=5432;Database=maf_ti;Username=postgres;Password=TU_CONTRASEÑA"
dotnet user-secrets set "AuthDesarrollo:Clave" "una-clave-larga-de-al-menos-32-caracteres"
```

Para ver qué tienes configurado: `dotnet user-secrets list`.

Los user-secrets solo se cargan con `ASPNETCORE_ENVIRONMENT=Development`, que es lo que usan `dotnet run` y
`dotnet ef` en desarrollo.

## En el servidor (producción)

Se usan **variables de entorno**. En los nombres, los dos puntos (`:`) se reemplazan por doble guion bajo (`__`):

```
ASPNETCORE_ENVIRONMENT=Production
ConnectionStrings__Default=Host=...;Port=5432;Database=maf_ti;Username=...;Password=...
```

- `AuthDesarrollo__Clave` **no se configura** en el servidor: fuera de Development el login de desarrollo no
  existe.
- Usa en producción un usuario de PostgreSQL propio de la aplicación (no `postgres`) y una contraseña
  distinta a la de desarrollo.
- Agrega la URL real del frontend como redirect "Aplicación de página única (SPA)" en Entra ID.
