# TourSmart RD — Fase 5.0: pasajeros individuales en cada reserva

## Diseño
- `PasajeroReserva` (tabla `PasajerosReserva`): `Id`, `ReservaId`, `Orden`, `NombreCompleto`.
- Cada reserva nueva requiere **exactamente tantos nombres como plazas** (`CantidadPersonas`), tanto en `POST /api/MiCuenta/reservas` como en `POST /api/Reservas`. La API valida de 1 a 100 pasajeros y nombres de 3 a 120 caracteres.
- El titular de la reserva **no se presupone viajero**: se registra explícitamente dentro de los pasajeros si efectivamente asistirá.
- La reserva y todos sus pasajeros se guardan dentro de la misma transacción SQL: la creación falla completa si no se pueden guardar los nombres.
- Las páginas de reserva del viajero y del personal incorporan campos de nombre por pasajero, ajustados a la cantidad de plazas.
- El manifiesto de Fase 4.9 ahora muestra nombres individuales bajo cada titular, manteniendo separados Confirmadas y Pendientes.
- Para reservas antiguas, no se fabrican nombres. Se muestran advertencias y se pueden completar mediante `Editar nombres` desde el manifiesto.
- Corrección administrativa de nombres: `GET/PUT /api/reservas/{id}/pasajeros` y `/panel/tours/reservas/{id}/pasajeros`. Acceso restringido a Administrador y Operador; se exige el mismo número de nombres que plazas, se bloquea una reserva cancelada o un tour ya iniciado.
- Corregir nombres no cambia cupos, pagos, reembolsos, estado, fecha, precio ni titular original de la reserva.
- El endpoint de cambios usa transacción serializable y bloqueo de la reserva, permitiendo reemplazar la lista de forma atómica.
- No se recopilan documentos de identidad ni fechas de nacimiento de acompañantes en esta fase.

## Migración y compatibilidad
- La migración `20261109010000_PasajerosReserva` añade una tabla nueva; no borra ni altera las reservas existentes.
- Las reservas antiguas comienzan sin lista nominal y pueden completarse desde el panel.
- Antes de actualizar, **haz respaldo de la base de datos**; la migración es aditiva, pero las listas contienen información personal.
- La creación de reservas nuevas por clientes API externos debe empezar a enviar `Pasajeros`: lista de nombres completos del mismo tamaño que `CantidadPersonas`. De lo contrario la API responde 400.

## Instalación
Detener API y MVC. En raíz de repositorio:
```powershell
git status
git pull origin feature/toursmart-integrador-ii
dotnet build AgenciaToursAPI.sln
dotnet ef database update --project .\AgenciaToursAPI.csproj
```
No ejecutar la migración si la compilación falla. No mezclar cambios locales pendientes con pull.

En terminales separadas:
```powershell
dotnet run --project .\AgenciaToursAPI.csproj --launch-profile http
dotnet run --project .\TourSmart.Web\TourSmart.Web.csproj --launch-profile http
```

## Pruebas
1. Como Cliente, reservar un tour de prueba para **5 personas**. Deben aparecer 5 campos; registrar 5 nombres distintos (o legítimamente iguales si hay homónimos), incluyendo al titular solo si viajará.
2. Crear reserva y comprobar que queda **Pendiente** con cantidad 5.
3. En el panel personal, abrir `Gestionar tours → Pasajeros`. La reserva debe aparecer con los cinco nombres bajo el titular y ocupar 5 plazas Pendientes.
4. Registrar el adelanto ficticio mínimo; confirmar la reserva. Debe pasar a Confirmadas con **los mismos cinco nombres**, sin cambiar las plazas comprometidas.
5. Desde **Editar nombres**, corregir un nombre y guardar. Revisar manifiesto: el cambio debe persistir, sin cambiar precios ni cantidad.
6. Como Operador, crear otra reserva desde `Gestionar reservas → Crear` para 2 personas, aportando 2 nombres. Comprobar su manifiesto.
7. Intentar en Swagger una creación de 5 plazas con solo 4 nombres: debe rechazarse con HTTP 400 y sin crear la reserva.
8. Intentar editar una reserva de 5 plazas enviando 4 nombres: debe rechazarse y conservar la lista anterior.
9. Intentar editar nombres de una reserva Cancelada o de un tour ya comenzado: debe rechazar.
10. Entrar como Cliente a los endpoints administrativos de manifiesto/edición: debe devolver 403; los clientes no deben poder consultar acompañantes de otras reservas.
11. Si existen reservas antiguas, comprobar la indicación de falta de nombres y completar un caso con **Editar nombres**.
12. Revisar la tabla `PasajerosReserva`: una fila por plaza con un único `Orden` por reserva.
13. Confirmar que el cliente puede reservar **para otras personas** sin aparecer automáticamente en la lista.

## Próximas mejoras
- Check-in nominal por pasajero, registros de llegada y asistencia, protegidos por rol.
- Posibilidad de autoservicio seguro para corregir acompañantes por el propio titular antes de la salida, con registro de auditoría de modificaciones.
- Política de privacidad, retención y eliminación de datos de pasajeros posterior a cada viaje.
- Pruebas automatizadas de concurrencia, migración y extremo a extremo.
- El código se subió al repositorio; compilación real y migración siguen pendientes de verificar en la PC del usuario.
