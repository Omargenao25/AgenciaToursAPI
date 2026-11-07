# Fase 2.8 — Gestión administrativa de clientes

## Implementado
- `GET /panel/clientes`: listado de clientes para Administrador y Operador.
- Búsqueda por ID, nombre, apellido, nombre completo, cédula y correo (sin distinción de mayúsculas).
- Paginación de diez clientes por página; usa la API para recuperar todos los clientes antes de filtrar (paginación MVC en memoria).
- `GET /panel/clientes/crear`: formulario de cliente nuevo.
- `GET /panel/clientes/{id}/editar`: formulario precargado.
- `POST /panel/clientes/guardar`: crea o actualiza usando JWT en el servidor web y antiforgery.
- Validaciones de campos obligatorios, correo, longitudes, cédula/correo duplicados (la API realiza la comprobación definitiva).
- Mensajes de error ante fallos de conexión, rechazo del API o duplicación.
- Accesos desde el dashboard, listado y formulario de reservas.
- No hay botón Eliminar: preservamos clientes relacionados con reservas; la API existente bloquea borrado cuando tienen reservas.
- No se ha modificado la base de datos ni requerido migraciones.

## Cómo probarlo en Visual Studio
1. Detener los dos proyectos.
2. En la carpeta del repositorio, ejecutar:
   ```powershell
   git status
   git pull origin feature/toursmart-integrador-ii
   dotnet build AgenciaToursAPI.sln
   ```
   Si `git status` revela cambios locales, no ejecutar el pull hasta resolverlos.
3. En terminal 1: `dotnet run --project .\AgenciaToursAPI.csproj --launch-profile http`.
4. En terminal 2: `dotnet run --project .\TourSmart.Web\TourSmart.Web.csproj --launch-profile http`.
5. Iniciar sesión como Administrador y visitar `http://localhost:5291/panel/clientes`.
6. Verificar listado de clientes existentes y probar búsqueda por nombre.
7. Pulsar **Registrar cliente**. Utilizar datos ficticios de prueba que no correspondan a personas reales; llenar todos los campos y guardar.
8. Verificar mensaje de éxito, nuevo registro y resultado en el buscador.
9. Editar ese cliente y modificar su teléfono; validar que cambia sin generar duplicados.
10. Intentar registrar otra vez una cédula o correo existentes. Debe rechazar la operación sin crear registros.
11. Desde **Crear reserva**, comprobar que el nuevo cliente aparezca en el selector.
12. Cerrar sesión y abrir `/panel/clientes`: redirige al login. Probar con Cliente: acceso denegado.

## Limitaciones y seguridad
- La web consulta y muestra información personal de clientes exclusivamente a personal autorizado. No compartir capturas con correos, cédulas o teléfonos reales sin ocultarlos.
- Para producción falta HTTPS obligatorio, auditoría, política de privacidad y acceso más granular, además de paginación/búsqueda en la API y base de datos.
- No hay verificación oficial del dígito de la cédula, únicamente comprobaciones actuales de la API y de los formularios.
- El código fue revisado en GitHub; **la compilación y las pruebas funcionales en el equipo local siguen pendientes**.
