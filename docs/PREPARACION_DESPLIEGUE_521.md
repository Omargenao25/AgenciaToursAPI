# TourSmart RD — Fase 5.21: preparación de entrega y despliegue

## Alcance
La etapa NO publica el sistema ni cambia bases de datos o credenciales. Añade verificación de prerequisitos en PowerShell y una comprobación de `dotnet publish` para generar por separado las carpetas publicables de API y Web en GitHub Actions. Esto no significa que ya esté listo para producción.

### Validación local no destructiva
Desde la raíz de AgenciaToursAPI:
```powershell
.\scripts\Test-TourSmartPreDespliegue.ps1
```
El script comprueba estructura y SDK, alerta sobre imágenes de usuario y no lee contraseñas. Para una configuración de producción en un entorno de ensayo separado, `-Production` revisa únicamente presencia y formato de algunas variables de ambiente, **no prueba conexión a SQL ni SMTP**.

### Construcción reproducible
```powershell
dotnet restore AgenciaToursAPI.sln
dotnet build AgenciaToursAPI.sln -c Release
dotnet publish .\AgenciaToursAPI.csproj -c Release -o .\publish\api
dotnet publish .\TourSmart.Web\TourSmart.Web.csproj -c Release -o .\publish\web
```
Estas carpetas son artefactos para revisión, no deben exponerse a internet sin configuración. No publicar `appsettings.Development.json`, el directorio de User Secrets ni archivos de respaldo de SQL. No reemplazar la carpeta `wwwroot/uploads` de una instalación existente sin copia de seguridad; contiene imágenes locales que no están en GitHub.

### Advertencias técnicas antes de la implementación pública
1. La API SQL local aún utiliza `Server=LAPTOP-...` y `TrustServerCertificate=True` en `appsettings.json`. Sobrescribir completamente `ConnectionStrings:Conexion` mediante secretos de entorno y configurar TLS confiable para la base de datos de producción, en un servidor dedicado. No modificar configuración local del alumno.
2. Los JWT se validan contra SQL; `Jwt:Key` de producción debe ser secreto robusto de 32 bytes o más. Los JWT no se deben registrar ni compartir.
3. `Api:BaseUrl` requiere HTTPS en la Web fuera de Development. Configurar URL pública del sitio para enlaces de verificación y recuperación (no localhost).
4. Ejecutar **una sola instancia** de la API para servicios periódicos hasta coordinar vencimientos y recordatorios; una sola instancia Web debido a sesiones en `IMemoryCache`. Escalado horizontal requiere arquitectura adicional.
5. SMTP: almacenar contraseñas en gestor de secretos, validar SPF/DKIM/DMARC y limitar volumen. Gmail sirve para las pruebas académicas, no sustituye necesariamente a un proveedor transaccional.
6. Los recordatorios marcan intentos antes del envío y no reintentan: pueden existir notificaciones perdidas ante fallo de proceso/SMTP. Necesita cola transaccional y reintentos para operación comercial.
7. Respaldar SQL Server, simular restauración y comprobar migraciones pendientes en una copia de datos; no ejecutar `database update` sobre producción sin plan.
8. Comprobantes financieros residen en SQL Server. Revisar permisos y retención de datos privados.
9. Auditar permisos de Administrador/Operador/Cliente, límites de archivos, inyección, CSRF, CORS, headers HTTP y sesiones antes de producción.

### Checklist para la exposición
- Abrir la Web y comprobar catálogo con fotografías.
- Iniciar sesión con cliente verificado; crear una reserva futura de prueba.
- Revisar adelanto 30 % y enviar comprobante de prueba sobre una base aislada.
- Acceder con operador y demostrar rechazo con motivo (sin aprobar dinero ficticio en una cuenta real).
- Mostrar notificación HTML y gestión de reservas.
- Abrir Finanzas y Control de adelantos; explicar que reportes pendientes no equivalen a cobros.
- Mostrar check-in, guía/transportes y cierre si ya existen datos apropiados.
- Explicar 401/403 de acceso por roles y verificación por correo.
- Presentar diagrama de actividades/secuencia de Práctica 4.
- Dejar una copia local/backup antes de empezar y ensayar el flujo sin tocar datos reales.

## Estado
CI comprueba compilación Release y publicación de ambos ejecutables, no levanta SQL Server ni valida interfaz visual. La preparación para internet continúa pendiente de proveedor, dominio, certificado, almacenamiento, base dedicada y seguridad.
