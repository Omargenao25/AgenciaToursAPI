# TourSmart RD — Fase 2.9: cuentas de viajeros y reservas propias

## Cambios técnicos
- `Usuario.ClienteId` opcional: relación de cuenta a ficha Cliente, índice único para que una ficha no quede asociada a varias cuentas.
- Migración **`20261107100000_VincularUsuariosClientes`**. No modifica ni vincula automáticamente cuentas anteriores.
- Endpoint público **`POST /api/MiCuenta/registrar`**: crea Usuario con rol Cliente y ficha Cliente juntos en una transacción. Contraseña hasheada mediante el PasswordHasher existente. No permite reutilizar correo ni documento de clientes/usuarios existentes.
- Endpoint **`GET /api/MiCuenta/reservas`** para rol Cliente: determina `ClienteId` a partir del ID de usuario de un JWT validado, consultando SQL Server; devuelve exclusivamente sus reservas.
- Endpoint **`GET /api/MiCuenta/metodos-pago`** para rol Cliente vinculado: devuelve los nombres de métodos disponibles.
- Endpoint **`POST /api/MiCuenta/reservas`**: recibe **únicamente TourId, MetodoPagoId, CantidadPersonas**. El ClienteId sale del JWT → Usuario → ClienteId; transacción SERIALIZABLE + bloqueo de fila de tour para proteger cupos; se crea Pendiente.
- MVC: `/viajero/registro`, `/viajero/reservas`, `/viajero/reservar/{tourId}`. Formularios protegidos con antiforgery; JWT únicamente en caché servidor.
- Navegación Mis reservas visible para rol Cliente; pantalla de detalle ofrece reservar o iniciar sesión según estado.
- El dashboard administrativo permanece separado y utiliza los endpoints de personal existentes.

## Antes de hacer pruebas
1. **Respaldar AgenciaToursDB en SQL Server Management Studio**.
2. Detener API y MVC, verificar `git status` y, con árbol limpio, ejecutar:
   ```powershell
   git pull origin feature/toursmart-integrador-ii
   dotnet build AgenciaToursAPI.sln
   ```
3. Si compila con **0 errores**, ejecutar:
   ```powershell
   dotnet ef database update --project .\AgenciaToursAPI.csproj
   ```
4. Verificar los nuevos campos y que ningún usuario antiguo fue vinculado arbitrariamente:
   ```sql
   SELECT Id, Nombre, Correo, Rol, ClienteId FROM dbo.Usuarios ORDER BY Id;
   ```
5. Iniciar API y MVC en terminales independientes (5290 y 5291).

## Pruebas funcionales recomendadas
1. Desde `http://localhost:5291/viajero/registro`, crear viajero ficticio nuevo con **correo y documento distintos a los existentes**.
2. Inicio de sesión con el nuevo correo y contraseña, verificar rol Cliente.
3. Abrir `/viajero/reservas`: inicialmente no debería haber reservas.
4. Abrir un tour futuro que tenga cupos, pulsar Reservar este tour, elegir 1 persona y método de pago; guardar.
5. Esperar estado Pendiente en Mis reservas, cupo descontado en el panel administrativo y nueva reserva visible en `/panel/reservas`.
6. Crear otra cuenta de viajero ficticio totalmente distinta. Verificar que no vea la reserva del primero y no pueda usar el módulo administrativo.
7. Crear reserva desde segundo viajero; verificar aislamiento mutuo.
8. Iniciar sesión como Administrador: sigue teniendo acceso a reservas y tours, pero no a `/viajero/reservas`.
9. Iniciar sesión con una cuenta **Cliente preexistente** que no tenga `ClienteId`: la API debe rechazar acceso a reservas propias y mostrar mensaje de vinculación pendiente, sin asociar datos por simple coincidencia de correo.
10. Intentar registrar correo o documento ya presente: esperar rechazo, sin duplicados.

## Precauciones y limitaciones
- Los clientes existentes como Manolo y Luis **conservan sus reservas y datos**. No se asigna una cuenta automáticamente por correo; se requiere posterior flujo verificado de vinculación de cuenta existente.
- No hay email verification ni recuperación de contraseña. Esta versión es para **entorno de desarrollo local**, no despliegue a internet sin verificación de identidad, protección antiabuso, ratelimit, HTTPS, política de privacidad, antifraude y auditoría.
- El método de pago queda referenciado en la reserva, **no se cobran fondos**.
- La cancelación desde la cuenta del viajero aún no está habilitada; solo consulta y solicitud.
- La API sigue siendo fuente definitiva de permisos y controla los cupos. La web no permite al cliente seleccionar o falsificar `ClienteId` para la reserva.
- La migración añade columna nullable con índice único y FK, no reescribe reservas ni credenciales existentes.
- El código se revisó en GitHub, pero **no se ha compilado ni probado en el ordenador del usuario**. Confirmar build y migración antes de las pruebas.
