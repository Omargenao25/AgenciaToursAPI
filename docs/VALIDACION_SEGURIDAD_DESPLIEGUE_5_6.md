# TourSmart RD — Fase 5.6: validación integral, seguridad y preparación del despliegue

## Alcance de los cambios
- **CI en GitHub Actions:** `.github/workflows/validar-dotnet.yml` ejecuta restauración, compilación Release .NET 8 y verificación de errores con `dotnet format` en cada push a `feature/toursmart-integrador-ii` y en PR hacia la rama. No requiere base SQL ni secretos.
- **Script de seguridad de solo lectura:** `scripts/Test-TourSmartSecurity.ps1` comprueba códigos HTTP de endpoints protegidos y rutas web. Pruebas por roles opcionales con JWT de cuentas **de prueba** (variables de entorno).
- **Script de integridad de solo lectura:** `scripts/Test-TourSmartIntegridad.ps1` comprueba totales de pasajeros, confirmados, pendientes, presentes, faltantes y coherencia con una excursión cerrada.
- **API en producción:** fallo de arranque si la clave JWT no alcanza 32 bytes o parece un ejemplo; excepciones inesperadas generan un JSON genérico (sin mostrar trazas); HTTPS/HSTS y cabeceras `nosniff`, `DENY`, `no-referrer`.
- **MVC en producción:** cookie de sesión con `SecurePolicy.Always`; arranque bloqueado si `Api:BaseUrl` no es HTTPS; cabeceras de protección y respuestas autenticadas sin caché.
- **Desarrollo local sin cambios de acceso:** los perfiles HTTP de `dotnet run` siguen disponibles en `Development`.
- **Ningún cambio en esquema o datos SQL. No hay migración de Fase 5.6. No se publica automáticamente la aplicación.**

## Instalación
Detener web/API, guardar cambios locales pendientes y ejecutar desde raíz:
```powershell
git status
git pull origin feature/toursmart-integrador-ii
dotnet restore AgenciaToursAPI.sln
dotnet build AgenciaToursAPI.sln -c Release
```
Revisar el resultado de la pestaña **Actions** en GitHub. Un push puede dar errores de compilación o de formato en CI: solucionar esos errores antes de promocionar la rama. Sin acceso a la máquina y su SQL, la revisión en GitHub no sustituye una compilación real.

Para levantar desarrollo (en terminales separadas):
```powershell
dotnet run --project .\AgenciaToursAPI.csproj --launch-profile http
dotnet run --project .\TourSmart.Web\TourSmart.Web.csproj --launch-profile http
```

## Prueba 1: controles anónimos
```powershell
.\scripts\Test-TourSmartSecurity.ps1 -TourId 1 -ReservaId 1
```
Usa identificadores de pruebas existentes si vas a suministrar tokens por rol. Sin tokens, se verifican accesos anónimos. Se aceptan redirecciones web al login, pero las API privadas deben devolver 401. Los tests nunca mandan POST/PUT/PATCH/DELETE.

Para ampliar la prueba con roles, establecer en **esa terminal**, temporalmente:
```powershell
$env:TOURSMART_TOKEN_ADMIN = "<JWT de prueba>"
$env:TOURSMART_TOKEN_OPERADOR = "<JWT de prueba>"
$env:TOURSMART_TOKEN_CLIENTE = "<JWT de prueba>"
.\scripts\Test-TourSmartSecurity.ps1 -TourId 1 -ReservaId 1
```
No copiar los JWT en chats, capturas, commits, tickets ni archivos. Borrar las variables al terminar:
```powershell
Remove-Item Env:TOURSMART_TOKEN_ADMIN -ErrorAction SilentlyContinue
Remove-Item Env:TOURSMART_TOKEN_OPERADOR -ErrorAction SilentlyContinue
Remove-Item Env:TOURSMART_TOKEN_CLIENTE -ErrorAction SilentlyContinue
```

## Prueba 2: integridad del manifiesto y cierre
Con un token de Administrador de pruebas en la variable `TOURSMART_TOKEN_ADMIN`:
```powershell
.\scripts\Test-TourSmartIntegridad.ps1 -TourId 1
```
Debe mostrar `[OK]` si la suma de reservas coincide con el manifiesto y con el cierre registrado. No cambia nada en SQL. Los datos deben permanecer estables durante la lectura: evitar check-ins simultáneos en el tour seleccionado.

## Prueba 3: matriz de procesos (manual, base de pruebas aislada)
| Flujo | Caso positivo | Caso negativo |
|---|---|---|
| Login/logout | Sesión válida, menú según rol | Token inválido o revocado -> 401 |
| Crear reserva | Nombres = cantidad de plazas | Cantidad distinta de nombres -> 400, sin reserva |
| Cupos | Reservas hasta capacidad | Sobreventa -> 409 |
| Adelanto | Pago ficticio neto suficiente -> confirmar | Pago insuficiente -> 409 |
| Cancelación | Conciliación de devoluciones previa | Cancelar con saldo pendiente -> 409 |
| Check-in | Solo pasajeros Confirmados | Pendiente, otro tour o fecha iniciada -> rechazo |
| Cierre | Un cierre por tour, asistencias consistentes | Segundo cierre, nombres faltantes, pendientes -> 409 |
| Tras cierre | Consultas históricas disponibles | Nuevo check-in, reserva, cambios de nombres -> rechazo |
| Reportes | Historial y rendimiento solo Administrador | Cliente y Operador bloqueados de esos reportes |
| Impresión | Hoja de salida A4 | Sin teléfonos, documentos o saldos en impresión |

**Importante:** los scripts incluidos son smoke tests de consulta, no tests automatizados E2E de operaciones mutables. Los casos manuales se ejecutan solo con datos ficticios en una base aislada.

## Checklist de staging / producción
1. Crear una **base SQL Server dedicada**; realizar respaldo y probar restauración. Aplicar migraciones antes de exponer la aplicación. Nunca compartir la conexión del desarrollo local con producción.
2. Establecer `ASPNETCORE_ENVIRONMENT=Production` y configurar `ConnectionStrings__Conexion`, `Jwt__Key` (aleatoria, al menos 32 bytes), `Jwt__Issuer`, `Jwt__Audience`, `Api__BaseUrl` con un URL **HTTPS** accesible desde el backend web. Conservar secretos en el gestor de secretos del proveedor; no en Git ni appsettings comprometidos.
3. Publicar **API y web** bajo HTTPS real con certificados válidos. Validar terminación TLS, encabezados de proxy inverso, redirecciones y HSTS. No usar `TrustServerCertificate=True` para SQL fuera del laboratorio; configurar confianza TLS auténtica.
4. Mantener Swagger solo en Development; no abrir puertos SQL públicamente. Restringir firewall, cuentas administrativas y permisos de base de datos.
5. La web conserva JWT en `IMemoryCache`: **desplegar una sola instancia web** hasta diseñar almacenamiento distribuido de sesiones y revocaciones. Reinicios cerrarán sesiones actuales. Evitar escalar horizontalmente sin rediseño.
6. El servicio `VencimientoReservasService` ejecuta vencimientos en segundo plano: validar su idempotencia y evitar múltiples instancias del proceso API sin coordinación. Revisar la configuración de tareas programadas antes de escalar.
7. Confirmar que SMTP de recuperación de contraseña esté configurado y verificado; no exponer credenciales, contraseñas ni JWT en logs.
8. Crear cuentas separadas de Administrador, Operador y Cliente; probar permisos, CSRF, sesión expirada, logout, respuesta 401/403, restricciones de fechas y de cierre.
9. Probar desde red externa autorizada, verificar copias de seguridad, alertas de errores, logs sin datos sensibles y procedimiento de restauración y rollback.
10. No declarar un entorno «listo para producción» hasta superar pruebas de carga, seguridad especializada, recuperación ante fallos y revisión de privacidad de datos personales.

## Notas técnicas
- Endurecimiento de headers y TLS es **defensa en profundidad**, no prueba formal de seguridad.
- El uso de JWT de prueba únicamente se recomienda en una API local o staging; evita pruebas destructivas con datos reales.
- La CI compila y verifica estáticamente, pero **no inicia SQL Server ni ejecuta los flujos completos de la agencia**.
- Implementación subida a `feature/toursmart-integrador-ii`; el equipo debe revisar y validar los resultados en su entorno antes de fusionar o desplegar.
