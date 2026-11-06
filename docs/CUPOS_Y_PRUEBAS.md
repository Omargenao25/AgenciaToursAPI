# Fase 1.3 — Capacidad de Tours y control de reservas

## Cambios
- `Tours.CapacidadMaxima` entero obligatorio entre 1 y 100000 al crear/editar.
- `GET /api/Tours` y `GET /api/Tours/{id}`: `capacidadMaxima`, `cuposReservados` y `cuposDisponibles`.
- `POST /api/Reservas`: comprueba el aforo y responde HTTP 409 al excederlo.
- `PUT /api/Reservas/{id}`: excluye de la suma la propia reserva para poder cambiar la cantidad o mover el tour sin duplicar cupos.
- `DELETE /api/Reservas/{id}`: conserva las reglas de fecha y libera plazas al eliminar.
- `PUT /api/Tours/{id}`: no permite reducir la capacidad por debajo de las personas reservadas.
- Operaciones de escritura cubiertas mediante transacciones Serializable y bloqueos de actualización de fila de Tour en SQL Server.
- Migración `20261106020000_AgregarCapacidadTours`: los tours previos reciben capacidad 100; si un tour ya tiene más de 100 personas reservadas, se asigna el total previamente reservado.

## Aplicación
1. Respaldar la base de datos, detener la API, `git pull origin feature/toursmart-integrador-ii`.
2. `dotnet build` y `dotnet ef migrations list`; confirmar que la migración figura Pending.
3. **Solo tras verificar el respaldo:** `dotnet ef database update`.
4. `dotnet run` y abrir Swagger.
5. Iniciar sesión como Administrador, autorizar con el JWT recién emitido.

## Pruebas manuales (datos de prueba, no modificar reservas reales)
1. Crear tour con `capacidadMaxima=5`, fecha futura y relaciones existentes. Esperar 201.
2. Consultar GET tour: capacidad 5, reservados 0, disponibles 5.
3. Crear reserva de 3 personas: 201; consultar el tour: disponibles 2.
4. Intentar reservar 3 más: 409; comprobar que siguen disponibles 2.
5. Reservar 2: 201; disponibles 0.
6. Modificar reserva de 3 a 4: 409, sin cambiar el total ni cupos.
7. Modificar capacidad del tour de 5 a 4: 409 y persistencia sin cambios.
8. Eliminar la reserva de 2 antes de que inicie el tour: 204; disponibles 2.
9. Modificar capacidad a 3: 204; disponibles 0.
10. Intentar crear tour con capacidad 0: 400.
11. Enviar dos reservas simultáneas por la última plaza: solo una debe quedar persistida, la otra debe recibir conflicto u otro rechazo controlado; revisar DB. Este escenario **requiere prueba de concurrencia específica** antes de considerarlo validado.

## Cuidados y tareas pendientes
- Verificar en entorno real todos los códigos y cálculos; esta versión fue revisada estáticamente, no compilada ni ejecutada por el asistente.
- El modelo actual no tiene estados de reserva: toda reserva existente cuenta como ocupada hasta ser eliminada. En la siguiente fase, integrar cancelaciones y estados.
- La operación de borrado de Tour debe incorporarse al mismo protocolo de bloqueo si se habilitan otras rutas de reserva; las FK actualmente protegen la integridad referencial.
- Medir y manejar conflictos de concurrencia / deadlocks de SQL Server; añadir pruebas automatizadas de alta simultánea, edición y eliminación.
- La capacidad inicial de los tours existentes es provisional; ajustar cada tour según su capacidad física real.
