# TourSmart RD — Fase 4.3: reembolsos y conciliación administrativa

## Alcance
- Registra constancias de devoluciones **ya realizadas por fuera del sistema**. No envía dinero, no integra pasarela de pago, no emite facturas fiscales y no garantiza que una transferencia realmente llegó a su destino.
- Solo Administrador puede registrar devoluciones. Administrador y Operador pueden consultar.
- Endpoints: GET y POST `/api/reservas/{reservaId}/reembolsos`; el POST exige JWT Administrador, monto positivo con máximo dos decimales, motivo de 8-200 caracteres, medio de devolución válido y ClaveOperacion única.
- Tabla `ReembolsosReserva`: conserva importe, medio, referencia, motivo, observación, instante UTC, responsable y clave idempotente. No ofrece editar ni eliminar movimientos.
- Los pagos originales permanecen **inmutables**. El importe pendiente de devolver es `SUM(PagosReserva.Monto) - SUM(ReembolsosReserva.Monto)`, siempre >= 0, con validación dentro de transacción serializable y bloqueo exclusivo de la reserva por `UPDLOCK, HOLDLOCK`.
- La repetición exacta de un POST con idéntica `ClaveOperacion` responde OK sin duplicar; reutilizar la clave con otro importe, medio, motivo o referencia devuelve conflicto (409).
- La cancelación administrativa de una reserva que tuvo pagos se permite **solo cuando el total devuelto coincide con el total cobrado**, y siempre respetando las reglas de fecha del tour. Se conserva `MotivoCancelacion = ReembolsoConciliado`.
- El turista mantiene bloqueada su cancelación si existen pagos históricos: debe contactar a la agencia.
- Las reservas canceladas no admiten registrar nuevos reembolsos por este flujo.
- Navegación: Gestión de reservas → Pagos / saldo → Reembolsos / conciliación.
- La pantalla de pagos muestra el total bruto recibido; **no confundirlo con caja neta tras devoluciones**. El panel de reembolsos muestra total recibido, total devuelto y pendiente de devolver.
- El servicio de vencimientos de la Fase 4.2 sigue preservando cualquier reserva pendiente con pagos registrados, aunque estén reembolsados: no se cancela automáticamente. La cancelación procede por el flujo administrativo tras la conciliación.

## Instalar
1. Detener API y MVC; crear un respaldo verificado de `AgenciaToursDB`.
2. `git status` y `git pull origin feature/toursmart-integrador-ii` si está limpio.
3. `dotnet build AgenciaToursAPI.sln`; antes de migrar, confirmar 0 errores.
4. `dotnet ef database update --project .\AgenciaToursAPI.csproj`.
5. Reiniciar API y MVC.
La migración `20261108170000_ReembolsosReserva` es aditiva: crea una tabla nueva sin borrar pagos ni reservas existentes.

## Pruebas financieras (solo cuentas e importes ficticios)
1. Crear una reserva futura de prueba y registrar RD$ 2,000 en pagos.
2. Abrir Reembolsos / conciliación: recibido 2,000, devuelto 0, pendiente 2,000.
3. Como Administrador, registrar devolución **ficticia y declarada como realizada para la prueba** de RD$ 500, con referencia y motivo. Comprobar recibido 2,000, devuelto 500, pendiente 1,500, movimiento en historial.
4. Intentar cancelar desde Gestión de reservas: debe rechazar con 409, sin cambiar estado ni liberar cupos.
5. Registrar otro reembolso de RD$ 1,500: pendiente de devolver 0; ambos movimientos deben seguir visibles y el pago original debe seguir intacto.
6. Cancelar desde Gestión de reservas **antes del inicio del tour**: debe permitir; la reserva se conserva Cancelada y se liberan las plazas.
7. Intentar registrar otra devolución de RD$ 0.01 sobre la misma reserva: debe rechazarse, sin cambio de datos.
8. En otra reserva de prueba, intentar reembolsar más de lo pagado: conflicto sin crear registro.
9. Repetir exactamente la misma solicitud POST en Swagger con `ClaveOperacion` idéntica: no duplica. Misma clave con otro importe o motivo: conflicto.
10. Como Operador, puede ver historial pero no registrar reembolsos. Como Cliente no accede a ese historial ni a operaciones financieras administrativas.
11. Confirmar que una cancelación de reserva sin pagos sigue funcionando.
12. Confirmar que el servicio de vencimientos sigue sin cancelar automáticamente reservas con pagos; incluso si el monto ya fue devuelto, la cancelación queda en manos del flujo administrativo.

## Prueba de Fase 4.2 pendiente de confirmar
Usar una **reserva exclusivamente de pruebas**, sin ningún pago, previa copia de seguridad. Editar exclusivamente la fecha de vencimiento para establecerla 2 minutos antes de `SYSUTCDATETIME()`. Tras el barrido periódico de la API (aprox. un minuto), comprobar estado Cancelada, motivo VencimientoSinPago y liberación de cupos. Repetir con reserva con un pago de prueba y comprobar que permanece Pendiente. Instrucciones SQL de resguardo en `docs/VENCIMIENTO_RESERVAS_4_2.md`.

## Pendientes
- Separar solicitudes de reembolso pendientes, autorizaciones y desembolsos reales con workflow de aprobación de 2 personas.
- Adjuntar pruebas documentales y conciliación bancaria real.
- Políticas específicas por tarifa (penalidad, no-reembolsable, condiciones del proveedor), contracargos, pagos disputados, comprobantes fiscales y auditoría financiera externa.
- Ajustar indicadores de caja neta en reportes.
- Pruebas automatizadas, de concurrencia y de integración con SQL Server.
- El código se subió a GitHub; compilación, migración y validación de extremo a extremo **pendientes en la PC del usuario**.
