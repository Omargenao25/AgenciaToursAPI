# TourSmart RD — Fase 3.1: perfil del viajero

## Implementado
- `GET /api/MiCuenta/perfil`: únicamente rol Cliente; obtiene la ficha desde el ID del usuario del JWT y su relación con ClienteId. Devuelve nombre, apellido, teléfono, correo, tipo/número de documento y país emisor, sin credenciales.
- `PUT /api/MiCuenta/perfil`: solo acepta `Nombre`, `Apellido` y `Telefono`; ignora e impide editar el correo, documento o rol. Actualiza en una transacción tanto Cliente (nombre, apellido, teléfono) como Usuario.Nombre. No acepta ClienteId del navegador.
- La API rechaza cuentas sin ficha verificada. Valida obligatorios, longitud individual y nombre completo <=100 caracteres.
- Pantalla protegida `/viajero/perfil`, con formulario antiforgery y sección de identificación de solo lectura. JWT siempre en caché del servidor MVC.
- Acceso desde menú, dashboard del cliente y Mis reservas.
- Sin cambios de esquema y **sin nuevas migraciones**.

## Pruebas
1. Detener ambos procesos. Confirmar `git status`; con árbol de trabajo limpio: `git pull origin feature/toursmart-integrador-ii`.
2. `dotnet build AgenciaToursAPI.sln` debe mostrar **0 errores**.
3. Terminal API: `dotnet run --project .\AgenciaToursAPI.csproj --launch-profile http`.
4. Terminal MVC: `dotnet run --project .\TourSmart.Web\TourSmart.Web.csproj --launch-profile http`.
5. Iniciar sesión como Manuel y abrir `http://localhost:5291/viajero/perfil`: mostrar su nombre, teléfono y documento.
6. Modificar **solo teléfono** y guardar. Debe mostrar éxito; refrescar la página debe conservar el nuevo número.
7. Modificar nombre o apellido y guardar: la ficha del cliente debe reflejar los datos. El saludo basado en la cookie de inicio de sesión mostrará el nuevo nombre **después de cerrar sesión y volver a entrar**.
8. Abrir `/viajero/reservas`: las reservas históricas se mantienen y su titularidad no cambia.
9. Intentar campos vacíos o nombre completo >100 caracteres: debe rechazar la operación sin modificar datos.
10. Iniciar sesión con Rohanny: su perfil no debe mostrar los datos de Manuel y sus modificaciones no deben afectarlo.
11. Probar sin sesión: el sistema debe redirigir a acceso. Como Administrador/Operador: acceso denegado a `/viajero/perfil`.
12. Validación avanzada: un PUT directo al endpoint con campos extra (`ClienteId`, `Correo`, `NumeroDocumento`) **no debe alterar** esos datos protegidos, porque no figuran en el DTO de actualización.

## Seguridad
- El correo y el documento no son editables desde la cuenta. La corrección de identidad necesitará un proceso posterior de verificación independiente.
- El cambio de perfil **no altera la contraseña** ni emite JWT nuevo. El saludo utiliza el nombre capturado al iniciar sesión; cambia tras el próximo login.
- El endpoint protege el acceso a la ficha mediante el ID de usuario del token y la relación persistida en SQL Server; nunca mediante IDs proporcionados por el navegador.
- El perfil muestra información personal: evitar compartir capturas con correo, documento o teléfono reales.
- La compilación y las pruebas funcionales **deben verificarse en el entorno local del usuario**.
