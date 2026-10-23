# Configuración del backend

Los secretos **no van en `appsettings.json`** ni en ningún archivo del repositorio. Si falta alguno, la API
no arranca y muestra un mensaje que dice cuál falta y cómo configurarlo.

| Clave | Qué es | Secreto | Dónde se usa |
|---|---|---|---|
| `ConnectionStrings:Default` | Cadena de conexión a PostgreSQL | Sí | Siempre |
| `AuthDesarrollo:Clave` | Llave para firmar el token del login de desarrollo (mínimo 32 caracteres) | Sí | Solo en Development |
| `AzureAd:TenantId`, `AzureAd:ClientId`, `AzureAd:Audience` | Datos de la app en Microsoft Entra ID | No (son públicos) | Siempre; ya están en `appsettings.json` |
| `Cors:Origenes` | URL(s) del frontend que pueden llamar a la API desde el navegador | No | Siempre; en desarrollo ya viene `http://localhost:4200` |
| `AllowedHosts` | Dominio(s) por los que se llega a la API, separados por `;` | No | Obligatorio fuera de desarrollo (no se acepta `*`) |

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
Cors__Origenes__0=https://<dominio del frontend>
AllowedHosts=<dominio de la API>
ASPNETCORE_HTTPS_PORT=443
```

Si hay más de un frontend permitido: `Cors__Origenes__1=...`, `Cors__Origenes__2=...`. Si la API se
publica bajo más de un nombre: `AllowedHosts=api.dominio.cl;otro.dominio.cl`.

### HTTPS y servidor web

La API está pensada para ir **detrás de un servidor web** (IIS o nginx) que tiene el certificado y recibe el
HTTPS:

- El servidor web debe enviar las cabeceras `X-Forwarded-For` y `X-Forwarded-Proto` (IIS con el módulo de
  ASP.NET Core lo hace solo; en nginx: `proxy_set_header X-Forwarded-Proto $scheme;` y
  `proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;`).
- Por seguridad, la API solo confía en esas cabeceras si el servidor web está **en la misma máquina**. Si
  está en otra, hay que agregar su IP en `SeguridadHttp.cs` (`KnownProxies`).
- Una petición por HTTP se redirige a HTTPS (por eso `ASPNETCORE_HTTPS_PORT=443`), y las respuestas por HTTPS
  llevan HSTS: el navegador recuerda usar siempre HTTPS con ese dominio.

### Qué revisa la API al arrancar

Si falta algo, la API **no arranca** y dice qué falta: la cadena de conexión, los datos de Entra ID,
`Cors:Origenes` y, fuera de desarrollo, `AllowedHosts`. En producción, además, no existen el login de
desarrollo ni Swagger.

- `AuthDesarrollo__Clave` **no se configura** en el servidor: fuera de Development el login de desarrollo no
  existe.
- Usa en producción un usuario de PostgreSQL propio de la aplicación (no `postgres`) y una contraseña
  distinta a la de desarrollo.
- Agrega la URL real del frontend como redirect "Aplicación de página única (SPA)" en Entra ID.
- En el frontend, `apiUrl` de `src/environments/environment.ts` debe apuntar a `https://<dominio de la API>/api`.
- El servidor debe tener la zona horaria `America/Santiago` disponible y los datos de idioma (ICU, en Linux el
  paquete `libicu`): el SLA se calcula en hora de Chile y el aviso de plazo usa los días en español.
- Los feriados del SLA están en la tabla `Feriados` (cargados 2026 y 2027). Antes de 2028 hay que agregar los del
  año siguiente con una migración.
