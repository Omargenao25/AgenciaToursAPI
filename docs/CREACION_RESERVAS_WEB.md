# TourSmart RD — Creación de reservas desde MVC

## Implementación
- GET `/panel/reservas/crear`: formulario protegido por Administrador u Operador.
- Opciones extraídas de `GET /api/Clientes`, `GET /api/MetodosPago` y `GET /api/Tours`.
- Selección de excursiones futuras con cupos libres.
- POST `/panel/reservas/crear`: validación del modelo y existencia de opciones + antiforgery; envía `POST /api/Reservas` con JWT exclusivamente en el servidor.
- La API impone fechas/cupos/cliente/método de pago y crea la reserva en estado Pendiente.
- Mensajes para 201, 400, 404, 409, 401, 403 y fallo de conexión. En errores por timeout, comprobar el listado antes de repetir para no duplicar.
- Accesos desde `/panel/reservas` y `/panel/tours`.

## Prueba recomendada
1. Detener procesos locales, ejecutar `git status`; si está limpio:
   ```powershell
   git pull origin feature/toursmart-integrador-ii
   dotnet build AgenciaToursAPI.sln
   ```
2. Iniciar API (5290) y MVC (5291) en dos terminales independientes.
3. Iniciar sesión como Administrador y abrir `http://localhost:5291/panel/reservas/crear`.
4. Seleccionar cliente existente, tour `MexicoLindoTour's` (verificar que su fecha no haya pasado), método de pago y 5 personas.
5. Guardar; se espera mensaje de éxito y una nueva reserva Pendiente en `/panel/reservas`.
6. Abrir `/panel/tours`: debe mostrar 5/15 ocupados, 10 plazas libres si no hubo otras reservas.
7. Editar el tour e intentar bajar capacidad a 3. Debe rechazar el cambio y conservar 15 plazas de capacidad.
8. Crear otra reserva con personas por encima de cupos restantes; esperar conflicto sin alteración del inventario de plazas.
9. Verificar que el acceso de Cliente esté prohibido y que cierre de sesión redirija al login.

## Limitaciones
- El formulario administrativo relaciona clientes existentes, no crea nuevos clientes (se añadirá gestión de clientes).
- El método de pago se registra como referencia de la reserva; **no se cobra dinero**, ni se integra un gateway de pagos.
- No se ejecutó compilación local ni pruebas end-to-end en el equipo del usuario: se deben validar en Visual Studio.
