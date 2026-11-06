# Fase 1.4 — Estados y cancelación lógica de reservas

## Comportamiento implementado
- `POST /api/Reservas` crea reservas en estado **Pendiente**.
- `PATCH /api/Reservas/{id}/confirmar` cambia Pendiente a **Confirmada**; confirmar una Confirmada se trata como idempotente; no permite confirmar Cancelada ni confirmar después del inicio del tour.
- `PATCH /api/Reservas/{id}/cancelar` cambia Pendiente/Confirmada a **Cancelada** sin borrar la fila; una Cancelada se trata como idempotente. Solo permite cancelar antes de iniciar el tour.
- `DELETE /api/Reservas/{id}` delega a cancelación lógica por compatibilidad; **no borra** historial.
- Las reservas Pendiente y Confirmada **ocupan cupos**; Cancelada **no ocupa**. Esto afecta GET Tours, POST/PUT Reservas y PUT Tours.
- `PUT /api/Reservas/{id}` prohíbe modificar una reserva Cancelada.
- Las respuestas de reservas incluyen `estado` y `fechaCambioEstado` (UTC).
- Los endpoints administrativos siguen exigiendo Administrador u Operador.

## Migración
`20261106040000_AgregarEstadosReserva`

Las reservas ya existentes pasan a **Confirmada**, para preservar el sentido de sus cupos actualmente ocupados. Las nuevas reservas serán Pendiente desde la API. Agrega `Estado` no nulo y `FechaCambioEstado` nullable y constraint SQL de valores permitidos.

**Antes de aplicar:** detener la API, respaldar la base de datos y comprobar `git status`.
```powershell
git pull origin feature/toursmart-integrador-ii
dotnet build
dotnet ef migrations list
```
Después de confirmar compilación y respaldo:
```powershell
dotnet ef database update
```

## Plan manual de pruebas — usar el Tour 4 si conserva fecha futura
1. GET /api/Reservas: reserva ID 3 debería permanecer **Confirmada**, porque era histórica al migrar.
2. GET /api/Tours/4: con capacidad 5 y reserva 3 de 3 personas, deberían quedar 2 cupos.
3. POST /api/Reservas con `{"clienteId":1,"tourId":4,"metodoPagoId":1,"cantidadPersonas":2}`: 201 y estado Pendiente. Se ocupan las últimas 2 plazas.
4. PATCH /api/Reservas/{nuevoId}/confirmar: 204. Revisar GET Reserva; estado Confirmada y cupos siguen en 0.
5. PATCH /api/Reservas/{nuevoId}/cancelar: 204. Revisar GET Reserva; la misma ID existe y está Cancelada, cupos del tour vuelven a 2.
6. PATCH /api/Reservas/{nuevoId}/confirmar: 409. PUT de reserva cancelada: 409.
7. Repetir cancelación: 204 sin efectos adicionales en cupos.
8. POST de 3 plazas después de cancelar: 409 por solo 2 disponibles.
9. Comprueba que las reservas canceladas quedan en GET /api/Reservas y que no cuentan para disminuir capacidad.
10. Faltan pruebas automatizadas, cargas concurrentes, expiración de Pendiente, auditoría de cambios y políticas de reembolso.

## Precauciones
- `FechaCambioEstado` usa UTC; `FechaReserva` en código heredado usa hora local. La unificación de fechas queda pendiente.
- No hay cobros reales ni reembolsos conectados: cancelar no procesa dinero.
- Los roles Cliente todavía no tienen endpoints de reservas personales.
- No usar registros de clientes reales para probar sin autorización.
