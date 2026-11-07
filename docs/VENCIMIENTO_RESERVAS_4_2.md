# TourSmart RD — Fase 4.2: vencimiento automático de solicitudes pendientes

## Alcance
- La política de cada tour incluye `HorasLimiteAdelanto` (1 a 168 horas; **24 por defecto**) además del porcentaje del adelanto mínimo.
- El Administrador/Operador puede configurar porcentaje y plazo antes de que el tour tenga su primera reserva: **Gestionar tours → Adelanto**.
- La API pública `GET /api/Tours/{id}/condiciones` devuelve ambos parámetros; la web informa de ellos al viajero antes de crear la reserva.
- Toda reserva nueva, administrativa o de autoservicio, guarda `VencePendienteUtc = DateTime.UtcNow.AddHours(tour.HorasLimiteAdelanto)`.
- Las reservas previas a esta migración se conservan con `VencePendienteUtc = NULL`, **sin expiración retroactiva**.
- Un servicio hospedado de la API (`VencimientoReservasService`) revisa al iniciar y cada minuto. Procesa hasta 100 pendientes vencidas sin pagos por ciclo.
- Para cada reserva, bajo transacción serializable y `UPDLOCK,HOLDLOCK`, vuelve a comprobar el estado, la fecha y la ausencia de pagos; la cambia a `Cancelada`, guarda `MotivoCancelacion = VencimientoSinPago` y `FechaCambioEstado`.
- El historial se mantiene; los cupos quedan libres porque el cálculo de ocupación excluye `Cancelada`.
- **Jamás se cancela automáticamente una reserva con uno o más pagos**, incluso si no alcanzó el adelanto: se conserva en estado Pendiente para revisión/conciliación manual.
- La API rechaza confirmar una reserva cuyo plazo expiró **sin pagos** aunque el servicio aún no la haya procesado. Igualmente, rechaza el **primer pago tardío** de esa reserva.
- El servicio **no cobra, devuelve, elimina pagos ni confirma reservas automáticamente**. Si un cliente pagó parte del adelanto antes del vencimiento, la agencia puede decidir cómo proceder.
- Las reservas ya Confirmadas o Canceladas no son modificadas.
- El proceso periódico funciona **solo mientras la API esté ejecutándose**. Al volver a encender la API, revisa los vencimientos pendientes. Para producción multiinstancia, conviene un planificador persistente, supervisión y métricas.

## Migración
`20261107230000_VencimientoReservasPendientes`:
- `Tours.HorasLimiteAdelanto int NOT NULL DEFAULT 24` con `CHECK BETWEEN 1 AND 168`.
- `Reservas.VencePendienteUtc datetime2 NULL`.
- `Reservas.MotivoCancelacion nvarchar(30) NULL`.
- Índice en `(Estado, VencePendienteUtc)`.
- No se elimina ni actualiza ninguna reserva existente.
- Antes de ejecutar, hacer backup de `AgenciaToursDB`.

## Instalación
1. Detén API y MVC y confirma `git status`.
2. Si no hay cambios sin resolver, ejecuta:
   ```powershell
   git pull origin feature/toursmart-integrador-ii
   dotnet build AgenciaToursAPI.sln
   ```
3. Solo con respaldo y compilación exitosa:
   ```powershell
   dotnet ef database update --project .\AgenciaToursAPI.csproj
   ```
4. Arranca las aplicaciones en **dos terminales**:
   ```powershell
   dotnet run --project .\AgenciaToursAPI.csproj --launch-profile http
   dotnet run --project .\TourSmart.Web\TourSmart.Web.csproj --launch-profile http
   ```

## Pruebas seguras usando reservas y pagos ficticios
1. Crear un tour futuro nuevo con 15 plazas y política 30% / 24h. Ver los parámetros en el formulario del turista; después de tener reservas, la API debe impedir editar la política.
2. Crear una reserva de prueba de una persona sin pago; debería quedar Pendiente con fecha límite UTC. Consultar `/viajero/reservas/{id}/comprobante`.
3. **Solo en una base de datos de pruebas con respaldo**, cambiar exclusivamente la fecha del registro de esa reserva a dos minutos antes de la hora UTC actual, sin tocar una reserva real. En SSMS, primero identificar correctamente `Id` y que no tenga pagos:
   ```sql
   SELECT r.Id, r.Estado, r.VencePendienteUtc, COUNT(p.Id) AS CantidadPagos
   FROM dbo.Reservas r
   LEFT JOIN dbo.PagosReserva p ON p.ReservaId = r.Id
   WHERE r.Id = @ReservaPruebaId
   GROUP BY r.Id, r.Estado, r.VencePendienteUtc;
   ```
   (Sustituir `@ReservaPruebaId` por un ID de prueba real o declarar previamente el parámetro).
4. Para simular vencimiento, usar un ID ficticio **comprobado**. Este comando es intencionadamente seguro: actualiza solo Pendiente **sin pagos**, y solo si coincide el ID indicado:
   ```sql
   DECLARE @ReservaPruebaId int = 12345; -- REEMPLAZAR por el ID real de tu reserva ficticia
   UPDATE dbo.Reservas
     SET VencePendienteUtc = DATEADD(minute, -2, SYSUTCDATETIME())
   WHERE Id = @ReservaPruebaId AND Estado = N'Pendiente'
     AND NOT EXISTS (SELECT 1 FROM dbo.PagosReserva WHERE ReservaId = @ReservaPruebaId);
   SELECT @@ROWCOUNT AS FilasActualizadas;
   ```
5. Con la API encendida, esperar la siguiente revisión periódica (aproximadamente un minuto). Comprobar que Estado es Cancelada, MotivoCancelacion es VencimientoSinPago, el comprobante muestra cancelación por vencimiento y el tour recuperó esas plazas.
6. Repetir simulación con una **segunda** reserva ficticia que tenga un pago parcial de RD$1 registrado **antes del vencimiento**. No debe cancelarse automáticamente ni liberar plazas. Ver «Requiere revisión» desde Pagos / saldo.
7. Confirmar que una reserva Confirmada nunca se cancela por vencimiento.
8. Confirmar que no se aceptan primeros pagos posteriores al plazo ni confirmación de solicitudes vencidas sin pagos. Probar con cuenta Cliente: no puede modificar política ni ejecutar acciones de personal.
9. Confirmar que reservas históricas sin `VencePendienteUtc` no son afectadas.

## Límites y pendientes
- Sin sistema de notificaciones: el viajero debe consultar el estado.
- Sin pagos digitales reales: solo abonos introducidos tras verificación manual.
- Sin vencimiento de saldo restante ni recordatorios.
- Sin reembolsos ni conciliación automática de abonos parciales.
- Servicio hospedado en la API, no un scheduler persistente distribuido.
- Compilación, migración y ejecución real pendientes en la PC del usuario.
