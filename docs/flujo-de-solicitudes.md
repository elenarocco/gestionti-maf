# Flujo de solicitudes

Hay cuatro tipos de solicitud y cuatro estados: **Pendiente**, **En proceso**, **Completada** y **Rechazada**. Las reglas están en `MafTi.Application/FlujoSolicitud.cs` y `CalculadoraSla.cs`; el frontend solo muestra los botones de las acciones que la API devuelve como permitidas.

## Estados y acciones de TI (Bandeja de TI)

| Tipo | Nace en | Acciones de TI |
|---|---|---|
| **Ingreso** | Pendiente | Pendiente → *Aprobar* (En proceso) o *Rechazar*. En proceso → *Marcar completada* o *Rechazar*. |
| **Modificación** | Pendiente | Igual que Ingreso. |
| **Bloqueo** (egreso) | En proceso | RRHH lo crea ya aprobado. TI lo *completa* o lo *rechaza*. |
| **VPN** | Pendiente | 1) TI aprueba el acceso: habilita la ficha (sigue Pendiente) o la rechaza. 2) Quien la pidió completa la ficha del proveedor (pasa a En proceso). 3) TI confirma el envío al proveedor (Completada) o rechaza la ficha. |

Toda acción queda registrada en el historial de la solicitud (quién, cuándo y comentario).

### Corregir una solicitud rechazada

Quien creó una solicitud rechazada (excepto VPN) puede **corregirla**: se abre el mismo formulario con los datos anteriores y se crea una solicitud nueva enlazada a la original.

## Qué cambia en la base al completar

Mientras una solicitud está abierta, **no modifica nada fuera de ella**. Los cambios se aplican recién cuando TI la marca como completada:

| Tipo | Efecto al completar |
|---|---|
| Ingreso | Se crea el trabajador con los datos de la solicitud y recibe los accesos pedidos. |
| Modificación | Los datos propuestos pasan a la ficha del trabajador y se agregan o quitan los accesos pedidos. |
| Bloqueo definitivo | El trabajador queda inactivo, con fecha de salida igual a la del bloqueo. |
| Bloqueo temporal | El trabajador sigue activo en la base. |
| VPN | El acceso queda en el registro de **Accesos VPN** (con fecha de caducidad). |

Si el cambio no se puede aplicar (por ejemplo, alguien ingresó a la misma persona mientras la solicitud estaba abierta), la solicitud no se completa y TI ve el motivo.

## Plazos (SLA)

Se calculan en hora de Chile y solo en **días hábiles** (lunes a viernes, sin los feriados de la tabla `Feriados`):

| Tipo | Plazo |
|---|---|
| Ingreso | 3 días hábiles. Cuenta el día del envío si es hábil y antes de las 12:00; si no, desde el siguiente día hábil. Vence a las 23:59 del tercer día. |
| Bloqueo | El mismo día si es hábil; si no, el siguiente día hábil. Vence a las 23:59. |
| Modificación | 48 horas que solo corren en días hábiles (viernes 15:00 → martes 15:00). |
| VPN | Igual que Modificación. |

Una solicitud **vencida** es una Pendiente o En proceso cuyo plazo ya pasó; se marca en rojo en las listas y cuenta como urgente en el Dashboard. Los feriados nacionales de 2026 y 2027 se cargaron en la migración `AgregarFeriados`; los años siguientes hay que agregarlos.
