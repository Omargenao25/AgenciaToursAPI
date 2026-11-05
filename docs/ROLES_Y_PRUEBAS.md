# Fase 1.2 — Roles y permisos

## Roles
- Administrador: administración de usuarios y módulos operativos.
- Operador: administración de tours, reservas, destinos, guías y catálogos; sin acceso a la administración de usuarios.
- Cliente: login y consulta pública del catálogo; las reservas personales se implementarán en una fase siguiente (vinculación Usuario/Cliente y filtrado por propietario).

## Estado actual
El registro público `POST /api/Usuarios` asigna **Cliente** sin permitir elegir otro rol. El JWT emitido después de iniciar sesión contiene un claim de rol y la respuesta de login muestra el rol. Los roles se guardan en `Usuarios.Rol` (valor inicial `Cliente`). Las consultas GET de Tours, Categorias, Destinos y Paises son públicas; sus operaciones de escritura exigen Administrador u Operador. Los datos privados de Clientes y Reservas, y la gestión de Guias, Transportes y MetodosPago, quedan reservados a personal. La administración de Usuarios exige Administrador, excepto el registro público.

## Aplicar migración en entorno de desarrollo
1. Respaldar la base de datos antes de migrar.
2. Detener la API.
3. Desde el directorio del .csproj, ejecutar:
   ```powershell
   dotnet ef database update
   ```
   Si no está disponible dotnet-ef: `dotnet tool install --global dotnet-ef --version 8.*` (solo si procede).
4. Verificar que la tabla `Usuarios` tiene la columna `Rol`. Las cuentas existentes se inicializan como `Cliente`.

## Primer administrador (bootstrap manual controlado)
No hay creación de administradores abierta al público. Después de migrar, en **tu base local de desarrollo** y con una cuenta propia identificada, ejecutar **desde SSMS** primero:
```sql
SELECT Id, Nombre, Correo, Rol FROM dbo.Usuarios;
```
Comprobar el ID correcto y elevar **solamente esa cuenta** (sustituir el marcador por ID numérico real):
```sql
UPDATE dbo.Usuarios
SET Rol = N'Administrador'
WHERE Id = <ID_DE_TU_USUARIO>;
```
Confirmar que se modificó una sola fila. No copiar credenciales ni datos personales a capturas públicas. En producción esta operación debe formar parte de un proceso administrativo auditado, no ejecutarse indiscriminadamente.

## Pruebas funcionales
- Detener y reiniciar API después de aplicar la migración.
- Volver a iniciar sesión (tokens antiguos no llevan rol).
- Sin token: `GET /api/Reservas` debe devolver 401.
- Token Cliente: `GET /api/Reservas` y `GET /api/Usuarios` deben devolver 403.
- Token Cliente: `GET /api/Tours` debe devolver 200.
- Token Administrador: `GET /api/Usuarios` debe devolver 200.
- Token Administrador u Operador: `GET /api/Reservas` debe devolver 200.
- Registro nuevo: rol Cliente en respuesta y en SQL.

## Limitaciones pendientes
- No hay API para asignar Operador o cambiar roles aún; por ahora, hacerlo únicamente en ambiente local de pruebas mediante un procedimiento administrativo controlado.
- Los JWT no tienen revocación inmediata si cambia el rol: el permiso anterior persistirá hasta que el token venza (actualmente 60 min); no usar esta implementación para producción sin estrategia de revocación o validación dinámica.
- No se ha ejecutado `dotnet build`, pruebas de integración ni `dotnet ef database update` en el equipo del usuario; estos cambios son de código y deben validarse en su entorno local.
