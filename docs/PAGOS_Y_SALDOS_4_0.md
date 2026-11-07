# TourSmart RD — Fase 4.0: pagos y saldos administrativos

## Alcance
- Los roles Administrador y Operador pueden consultar pagos y registrar abonos efectivamente recibidos para una reserva.
- No procesa cobros en línea, tarjetas, devoluciones, impuestos fiscales ni emisión de factura.
- La reserva conserva su total; el saldo se obtiene como `Total - SUM(PagosReserva.Monto)`. Estados derivados: Sin pagos, Pago parcial y Pagada.
- Los movimientos incluyen monto (decimal 18,2), método, referencia, observación, fecha UTC, responsable y ClaveOperacion única.
- API `GET /api/reservas/{reservaId}/pagos` y `POST /api/reservas/{reservaId}/pagos`, restringida a personal.
- Interfaz `/panel/reservas` → **Pagos / saldo** → `/panel/pagos/{reservaId}`.
- La operación POST requiere `ClaveOperacion` aleatoria; en la transacción serializable se bloquea la reserva (UPDLOCK, HOLDLOCK), consulta pagos existentes y rechaza montos superiores al saldo.
- Repetir una operación con la misma clave y los mismos valores no agrega pagos (respuesta 200); reutilizar clave para valores distintos devuelve 409.
- Sin operaciones de borrado o edición de pagos; los pagos confirmados son historial auditable.
- Si ya existen pagos para una reserva, se bloquea su cancelación (tanto personal como turista) y edición desde la API hasta implementar reembolsos/conciliación, evitando estados financieros contradictorios.
- La columna `MetodoPagoId` de Reserva representaba el método seleccionado al crear la solicitud; cada `PagoReserva` tiene su propio método realmente recibido.

## Instalación
1. Detener API y MVC. Hacer respaldo de `AgenciaToursDB`.
2. Ejecutar `git status`. Si no existen cambios locales que impidan actualizar:
   ```powershell
   git pull origin feature/toursmart-integrador-ii
   dotnet build AgenciaToursAPI.sln
   ```
3. **Solo con 0 errores y respaldo realizado**:
   ```powershell
   dotnet ef database update --project .\AgenciaToursAPI.csproj
   ```
   Esto aplica la migración `20261107190000_PagosReserva`; no borra reservas existentes.
4. Reiniciar procesos:
   ```powershell
   dotnet run --project .\AgenciaToursAPI.csproj --launch-profile http
   dotnet run --project .\TourSmart.Web\TourSmart.Web.csproj --launch-profile http
   ```

## Pruebas recomendadas (usar exclusivamente dinero y cuentas ficticias)
1. Entrar como Administrador u Operador, abrir `/panel/reservas`, pulsar **Pagos / saldo** en una reserva activa con **0 movimientos**. Debe mostrar Pagado=0, Saldo=Total.
2. Sobre una reserva ficticia de RD$8,000, registrar RD$2,000: Pagado=2,000, Saldo=6,000, un movimiento.
3. Recargar: el historial y el saldo deben persistir.
4. Registrar RD$6,000: Pagado=8,000, Saldo=0, estado Pagada. Ya no debe mostrar formulario de cobro.
5. Intentar superar el saldo en otra reserva: debe rechazarse sin generar movimiento.
6. Intentar registrar importe cero, negativo o más de dos decimales: la API debe rechazarlo.
7. En una reserva con pagos, intentar cancelar desde panel o cuenta cliente: la API debe devolver 409 y mantener sus plazas/estado. Cambiar importe/cliente de la misma reserva también debe bloquearse.
8. En una reserva sin pagos, comprobar que cancelar todavía funciona como antes y libera cupos.
9. Probar el mismo POST dos veces con idéntica clave `ClaveOperacion`: no se debe duplicar el movimiento. Reutilizarla con distinto importe/método/referencia debe devolver 409.
10. Usuario Cliente no debe poder abrir `/panel/pagos/{id}` ni invocar la API de pagos.
11. Comprobar una segunda cuenta de personal y la fecha/responsable de los movimientos.
12. Comprobar que el historial de pagos nunca se borra al recargar.

## Pendientes de producción
- Devoluciones/reembolsos y conciliación de reservas pagadas.
- Reversos con motivo y aprobación; manejo de contracargos.
- Auditoría y autorización más granular de caja, roles por operación, corte de caja y reportes financieros.
- Integración real con procesadores de pago, notificaciones y facturación fiscal cuando corresponda.
- Pruebas de concurrencia y despliegue para transacciones serializables sobre SQL Server.
- Compilación, migración y validación de resultados **pendientes en la PC del usuario**.
