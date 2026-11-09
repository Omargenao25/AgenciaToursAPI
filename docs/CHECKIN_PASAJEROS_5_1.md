# TourSmart RD — Fase 5.1: check-in individual por pasajero

## Objetivo y alcance
- Registrar **llegada individual** de asistentes en reservas Confirmadas desde **Gestionar tours → Pasajeros**.
- Agregar a cada `PasajeroReserva` los campos opcionales `LlegadaUtc` y `RegistradoPor`. Una fila/pasajero se marca como Presente si `LlegadaUtc != null`; de lo contrario sigue Pendiente de llegar.
- En el manifiesto aparecen los indicadores **Presentes** y **Faltantes** (plazas confirmadas menos asistentes presentes), botones individuales **Marcar llegada** y **Revertir**, y hora/responsable de cada check-in.
- El botón solo aparece antes de que comience la excursión. La API vuelve a verificar la regla; una petición directa no puede esquivarla.
- Solo Administrador y Operador tienen acceso; no se permite marcar pasajeros de solicitudes Pendientes, de reservas Canceladas o que pertenezcan a otro tour.
- Endpoint: `PUT /api/tours/{tourId}/check-in/pasajeros/{pasajeroId}`, cuerpo JSON `{"presente":true}` para marcar llegada y `{"presente":false}` para revertir.
- La web utiliza `POST /panel/tours/{id}/manifiesto/check-in/{pasajeroId}` con antifalsificación y envía el cambio a la API con JWT.
- Las actualizaciones SQL son **atómicas y condicionales**, evitando doble registro simultáneo. Repetir el estado ya alcanzado no crea una segunda llegada. 
- No se alteran pagos, reembolsos, reservas, cupos, precios ni titularidad.
- Si existe alguna llegada registrada, se bloquea **Editar nombres** para la reserva hasta revertir las llegadas, a fin de no borrar accidentalmente la asistencia.
- No es un sistema biométrico ni de validación documental; el personal debe verificar la identidad de forma adecuada y respetuosa.
- No se envían correos, SMS ni WhatsApp. No es un sistema automático de ausencias.

## Migración
Migración **aditiva** `20261109020000_CheckInPasajeros`, añade dos columnas nullable a la tabla ya existente `PasajerosReserva`. Los registros anteriores se consideran Pendientes de llegar. No modifica reservas o pagos.

Antes de migrar: detener web y API, **respaldar AgenciaToursDB**, comprobar `git status` y resolver cualquier cambio local.

```powershell
git pull origin feature/toursmart-integrador-ii
dotnet build AgenciaToursAPI.sln
dotnet ef database update --project .\AgenciaToursAPI.csproj
```

No ejecutar la migración si falla la compilación.

Arrancar en dos terminales separadas:
```powershell
dotnet run --project .\AgenciaToursAPI.csproj --launch-profile http
dotnet run --project .\TourSmart.Web\TourSmart.Web.csproj --launch-profile http
```

## Pruebas guiadas
1. Crear o reutilizar una **reserva Confirmada** para un tour futuro en el que todavía no haya comenzado la salida, con 5 pasajeros nominales.
2. Entrar con Administrador a `http://localhost:5291/panel/tours` y abrir **Pasajeros**.
3. Verificar que el manifiesto muestre **Presentes: 0** y **Faltantes: 5** si no hay otras reservas confirmadas en el tour. Las solicitudes pendientes no deben sumarse a estos contadores.
4. Pulsar **Marcar llegada** en un pasajero. Verificar Presentes 1 / Faltantes 4, fecha UTC y responsable.
5. Repetir el marcaje de esa misma persona desde la API: debe ser idempotente y no duplicarse.
6. Marcar otro: Presentes 2 / Faltantes 3. Revertir el primero: Presentes 1 / Faltantes 4.
7. Intentar check-in para un pasajero de una reserva **Pendiente**: HTTP 409, sin cambiar su estado.
8. Intentar pasar el `pasajeroId` de otro tour a la ruta: HTTP 404, sin actualizar.
9. Probar con rol Cliente: API y web protegidas, sin acceso a los pasajeros de otros viajeros.
10. Si intentas **Editar nombres** de una reserva con alguna llegada, la API rechaza con HTTP 409 hasta revertir el check-in.
11. Comprobar que no cambian los importes ni los cupos, y que el estado de la reserva continúa Confirmada.
12. Comprobar que, al comenzar el tour, los botones de asistencia dejan de mostrarse y la API rechaza nuevos cambios.

## Limitaciones y próximos pasos
- El valor `RegistradoPor` corresponde al nombre del usuario autenticado; para auditoría rigurosa debe agregarse un historial append-only de acciones y versiones, incluyendo reversión.
- No se ofrece todavía exportación o impresión de manifiesto para el guía.
- La interfaz no permite check-in durante una excursión ya comenzada; si operaciones necesita ese flujo, se establecerá una ventana de recepción controlada y explícita.
- La fecha/hora de la excursión continúa utilizando la convención local previa del proyecto. Evaluar zona horaria configurable antes de desplegar en múltiples países.
- No hay pruebas automatizadas ni compilación local en este entorno: validar migración, funcionamiento y errores en el equipo de desarrollo.
