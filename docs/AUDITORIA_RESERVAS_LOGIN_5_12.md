# TourSmart RD - Auditoria de reservas con inicio de sesion interactivo

Se agrego `scripts/Invoke-TourSmartAuditoriaReservas.ps1` para evitar copiar JWT manualmente.

## Uso

1. Arrancar la API y la web en dos terminales.
2. Abrir una tercera consola PowerShell en la raiz del repositorio.
3. Ejecutar:

```powershell
git pull origin feature/toursmart-integrador-ii
Set-ExecutionPolicy -Scope Process -ExecutionPolicy Bypass
.\scripts\Invoke-TourSmartAuditoriaReservas.ps1
```

El asistente pide el correo del **Administrador de pruebas** y luego la contrasena (se escribe oculta). Llama al endpoint local `POST /api/Auth/login` y exige que el rol devuelto sea Administrador. Ejecuta `Test-TourSmartReservas.ps1`, que solo realiza consultas GET, y restaura o quita la variable `TOURSMART_TOKEN_ADMIN` al finalizar.

**Seguridad**: solo se aceptan URLs de la API con host localhost/127.0.0.1; no utilizar usuarios reales sobre una base de datos compartida. El proceso conserva la clave brevemente en memoria para formar el JSON requerido, pero no la imprime ni almacena archivos. Los secretos gestionados por .NET pueden permanecer temporalmente en memoria administrada. Nunca compartir contrasenas ni JWT en el chat ni en capturas.

Se espera `[OK]` si las reservas consultadas son coherentes. Si la API esta apagada, las credenciales fallan o se usa otro rol, mostrara un error sin revelar datos sensibles.

### Terminal 1 - API
```powershell
dotnet run --project .\AgenciaToursAPI.csproj --launch-profile http
```

### Terminal 2 - Web
```powershell
dotnet run --project .\TourSmart.Web\TourSmart.Web.csproj --launch-profile http
```

### Limites
Esta herramienta no prueba escrituras concurrentes ni confirma los saldos de pagos o la lista de pasajeros. Debe complementarse con las comprobaciones de manifiesto, pagos y conciliacion. Evitar consultas durante modificaciones activas de reservas para disminuir diferencias transitorias.
