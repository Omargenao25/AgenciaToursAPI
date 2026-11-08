# TourSmart RD — Fase 4.9: manifiesto operativo de excursiones

## Funcionalidad
- Nuevo endpoint autorizado para personal: `GET /api/tours/{tourId}/manifiesto`.
- Acceso desde **Gestionar tours → Pasajeros** a `/panel/tours/{id}/manifiesto`.
- Muestra nombre del tour, fecha y hora, guía, tipo de transporte, capacidad, plazas confirmadas, plazas pendientes, plazas comprometidas y plazas disponibles.
- Lista aparte las reservas **Confirmadas** y las **Pendientes**. Las Canceladas no forman parte de ninguna lista y no ocupan plazas.
- Para cada reserva: número de reserva, titular, teléfono, cantidad de personas, total recibido neto y saldo (reservas confirmadas; también saldo de solicitudes pendientes).
- Los importes netos deducen los reembolsos registrados: `PagosReserva - ReembolsosReserva`.
- Cada fila permite acceder a `Pagos / saldo`.
- **Solo Administrador y Operador** pueden acceder a la API y página. El cliente no obtiene datos de otros viajeros.
- La respuesta es privada y utiliza `Cache-Control: private, no-store`.
- No hay cambios de base de datos, no se cobra ni se confirma ninguna reserva, no se envían correos.

## Restricción importante
Hoy TourSmart RD guarda un **titular por reserva** y `CantidadPersonas`; no almacena los nombres y documentos de cada acompañante. Por eso esta funcionalidad es un manifiesto de **titulares y plazas**, no una lista nominal individual de todos los pasajeros. No debe usarse como verificación individual de identidad.

El manifiesto refleja la situación de la reserva cuando se consulta; las plazas pendientes pueden cambiar de estado por vencimiento o confirmación. Actualiza antes de la salida.

## Instalación
Detener API y MVC. Desde la raíz del repositorio:
```powershell
git status
git pull origin feature/toursmart-integrador-ii
dotnet build AgenciaToursAPI.sln
```
Resolver cambios locales antes del `pull`. **Sin migración nueva**.

En dos terminales distintas:
```powershell
dotnet run --project .\AgenciaToursAPI.csproj --launch-profile http
dotnet run --project .\TourSmart.Web\TourSmart.Web.csproj --launch-profile http
```

## Pruebas
1. Acceder como Administrador u Operador a `http://localhost:5291/panel/tours`.
2. Abrir **Pasajeros** en un tour de prueba.
3. Crear una reserva de 2 personas en Pendiente: debe figurar solo en las pendientes; `CuposPendientes` aumenta 2 y los disponibles disminuyen 2.
4. Registrar el adelanto suficiente y confirmar: la reserva pasa a Confirmadas; las plazas comprometidas y disponibles no cambian, pero se mueven 2 plazas de Pendientes a Confirmadas.
5. Crear una segunda reserva y cancelarla: deja de aparecer en el manifiesto y se liberan sus plazas.
6. Verificar que los saldos reflejan pagos menos devoluciones y que los enlaces **Pagos / saldo** abren la reserva correcta.
7. Comprobar que guía, transporte y fecha corresponden al tour escogido.
8. Entrar como Cliente o anónimo: ni el panel ni el endpoint de API permiten ver los titulares y teléfonos de otros clientes.
9. Confirmar que actualizar la lista no cambia datos ni registra pagos.
10. Confirmar que un tour sin reservas muestra listas vacías y capacidad disponible completa.

## Próxima etapa recomendada
Incorporar participantes individuales con consentimiento y permisos adecuados, y posteriormente control de asistencia (check-in) por plaza o persona, separado de la gestión de pagos.

**Pendiente:** compilación y prueba real en la computadora del usuario.
