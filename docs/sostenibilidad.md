# Sostenibilidad (computación verde)

La pantalla **Sostenibilidad** compara el papel que se evita al digitalizar los formularios con la huella estimada del propio sistema, en gramos de CO2e. Son **estimaciones con supuestos visibles, no mediciones**.

- Endpoint: `GET /api/sostenibilidad/resumen?desde=&hasta=` (permiso `VerSostenibilidad`). Sin fechas, usa el mes en curso.
- Edición de supuestos: `PUT /api/sostenibilidad/parametros` (permiso `EditarParametrosSostenibilidad`).
- Cálculo: `MafTi.Application/CalculadoraSostenibilidad.cs` (con tests). El frontend no calcula nada.

## Qué cuenta

Las solicitudes **Completadas** cuya fecha de completado (historial: "Completada" o "Ficha VPN enviada al proveedor") cae en el rango. Una solicitud creada en octubre y completada en noviembre cuenta en noviembre.

## Fórmulas

```
Hojas evitadas         = Σ (solicitudes del tipo × hojas del formulario del tipo × copias impresas)
CO2e papel evitado     = hojas evitadas × g CO2e por hoja
CO2e del sistema       = solicitudes × (KB por solicitud ÷ 1.000) × g CO2e por MB
Ahorro neto            = CO2e papel evitado − CO2e del sistema
```

## Supuestos (tabla `ParametrosSostenibilidad`)

| Supuesto | Valor inicial | Origen |
|---|---|---|
| Hojas por formulario | 1 en los cuatro tipos | Confirmado por la jefatura: cada formulario era una sola hoja. |
| Copias impresas | 1 | El mismo papel circulaba en mano para las firmas. |
| g CO2e por hoja | 4,7 | Estimación de ciclo de vida de una hoja A4. **Fuente pendiente de confirmar.** |
| KB transferidos por solicitud | 823 | Medido (ver abajo). |
| g CO2e por MB | 0,6 | Basado en el modelo Sustainable Web Design. **Pendiente de confirmar la versión del modelo.** |

Se editan desde la pantalla (sección **Editar supuestos**, solo Gestión General). El cambio afecta a todos los cálculos, también los de períodos anteriores.

## Cómo se midieron los 823 KB

El 04-11-2026, con un navegador automatizado (Chromium) y caché vacía, sobre el flujo completo de **una** solicitud de Ingreso:

1. RRHH inicia sesión y crea la solicitud: 415,7 KB.
2. Admin TI inicia sesión, la revisa en la Bandeja de TI, la aprueba y la marca completada: 407,3 KB.

Front compilado para producción y servido con gzip; la API se midió del lado del servidor con un proxy, **todavía sin compresión** (86,5 KB entre ambos usuarios). Incluye HTML, JS, CSS, logo y fuentes de Google (215,8 KB). No incluye el inicio de sesión de Microsoft (se usó el login de desarrollo). Tres repeticiones dieron el mismo resultado.

**Volver a medir** si se agregan pantallas al flujo o cuando el sistema esté en el servidor definitivo. Desde que la API comprime sus respuestas (Brotli o gzip, entre 67 % y 74 % menos), la parte de la API debería bajar.

## Datos de ejemplo

El interruptor **Datos de ejemplo** (solo Gestión General, apagado por defecto) muestra un **mes típico** con los promedios mensuales informados por la jefatura: Ingreso 7, Modificación 15, Bloqueo (egreso) 3 y VPN 2. Mientras está activo, la pantalla muestra un aviso rojo fijo: no son solicitudes registradas en el sistema.

Con los supuestos iniciales, un mes típico da 27 hojas evitadas, 126,9 g de CO2e del papel, 13,3 g del sistema y **113,6 g de ahorro neto** (≈ 1,4 kg al año).

## Lo que no está en el cálculo

El traslado manual del formulario para reunir las firmas de las jefaturas era a pie dentro de la oficina, sin emisiones. Su beneficio es de **tiempo y trazabilidad**, no de CO2e.
