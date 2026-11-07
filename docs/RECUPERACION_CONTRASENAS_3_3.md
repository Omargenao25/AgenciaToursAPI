# TourSmart RD — Fase 3.3: recuperar contraseña por correo

## Implementación
- Página pública `GET/POST /cuenta/recuperar` vinculada al inicio de sesión.
- API `POST /api/Recuperacion/solicitar`: solo emite tokens para cuentas con rol Cliente. Devuelve el mismo mensaje para correos existentes y desconocidos, y también ante fallos de SMTP. Limita nuevas solicitudes por cuenta a una cada dos minutos.
- Token de 32 bytes aleatorios criptográficos, representado con 64 caracteres hexadecimales. Se guarda únicamente su SHA-256, nunca el token original, y caduca en 15 minutos.
- Correo con enlace de restablecimiento que apunta a `Recovery:PublicBaseUrl` + `/cuenta/restablecer?token=...`.
- Página pública `GET/POST /cuenta/restablecer`, con antiforgery, campos de contraseña, `Cache-Control: no-store` y `Referrer-Policy: no-referrer`.
- API `POST /api/Recuperacion/restablecer` verifica el token, la caducidad y la nueva contraseña (>=10 caracteres, mayúscula, minúscula, número y símbolo, distinta de la anterior). Operación serializable con bloqueo de actualización; consume todos los enlaces activos de esa cuenta y rota `VersionSesion`, revocando JWT anteriores.
- Tabla `RecuperacionesContrasena`, migración `20261107170000_RecuperacionContrasenas`, sin modificar reservas.
- **El envío real NO funcionará hasta configurar SMTP**. No hay credenciales incluidas ni enlaces de recuperación mostrados públicamente.

## Aplicación segura en desarrollo
1. Detener API y MVC. Realizar respaldo de `AgenciaToursDB` en SSMS.
2. Desde la carpeta raíz: `git status`. Si no hay cambios locales sin resolver:
   ```powershell
   git pull origin feature/toursmart-integrador-ii
   dotnet build AgenciaToursAPI.sln
   ```
3. Solo con respaldo y **0 errores**, aplicar:
   ```powershell
   dotnet ef database update --project .\AgenciaToursAPI.csproj
   ```
4. Configurar los siguientes **User Secrets en el proyecto AgenciaToursAPI**, reemplazando valores de ejemplo por valores reales de tu proveedor SMTP. No subirlos a GitHub:
   ```powershell
   dotnet user-secrets set "Recovery:SmtpHost" "smtp.TU-PROVEEDOR.com" --project .\AgenciaToursAPI.csproj
   dotnet user-secrets set "Recovery:SmtpPort" "587" --project .\AgenciaToursAPI.csproj
   dotnet user-secrets set "Recovery:SmtpUser" "TU-USUARIO-SMTP" --project .\AgenciaToursAPI.csproj
   dotnet user-secrets set "Recovery:SmtpPassword" "TU-CLAVE-SMTP" --project .\AgenciaToursAPI.csproj
   dotnet user-secrets set "Recovery:FromEmail" "TU-CORREO-REMITENTE" --project .\AgenciaToursAPI.csproj
   dotnet user-secrets set "Recovery:PublicBaseUrl" "http://localhost:5291" --project .\AgenciaToursAPI.csproj
   ```
   **Importante**: `SmtpClient.EnableSsl=true` está configurado para SMTP STARTTLS (generalmente puerto 587); el proveedor debe admitir este modo. Para producción, `PublicBaseUrl` debe ser una URL HTTPS pública y de tu propiedad. La clave SMTP debe ser una credencial autorizada por el proveedor, no la contraseña normal de tu cuenta si este no lo admite.
5. Arrancar API y MVC:
   ```powershell
   dotnet run --project .\AgenciaToursAPI.csproj --launch-profile http
   dotnet run --project .\TourSmart.Web\TourSmart.Web.csproj --launch-profile http
   ```

## Prueba funcional
1. Visitar `http://localhost:5291/cuenta/acceder`, seleccionar **¿Olvidaste tu contraseña?**
2. Solicitar recuperación de cuenta ficticia de viajero con correo real de prueba que controles. La pantalla presenta respuesta genérica.
3. Revisar el buzón de prueba y abrir el enlace dentro de los 15 minutos (en el mismo equipo si es `localhost`).
4. Introducir contraseña fuerte y confirmarla. Debe redirigir al login.
5. Intentar iniciar sesión con la antigua (rechazada) y con la nueva (aceptada).
6. Abrir el mismo enlace de nuevo: debe rechazarse por ser de un solo uso.
7. Iniciar sesión desde dos navegadores antes de recuperar y comprobar después del cambio que ambos accesos anteriores fueron revocados.
8. Solicitar recuperación para correo no existente; mensaje idéntico al de correo válido.
9. Solicitar nuevamente antes de 2 minutos: no genera otro correo.
10. Confirmar que el flujo solo trabaja con rol Cliente; no restablece directamente credenciales administrativas.

## Pendientes para internet/producción
- HTTPS obligatorio y host autorizado; **no utilizar enlaces http://localhost para pruebas externas**.
- Límite de solicitudes por IP y antiabuso distribuido, CAPTCHA adaptable y mecanismos de prevención de enumeración por tiempos.
- Limpieza/retención de registros de recuperación vencidos.
- Envío de alertas de seguridad por correo cuando la contraseña cambie, verificación de correo en el registro y controles contra compromiso de buzón.
- Comprobar la correcta entrega SMTP y políticas SPF/DKIM/DMARC.
- No publicar capturas con enlaces de recuperación, tokens o contraseñas.
- Código subido a GitHub pero compilación/migración/pruebas reales **pendientes en el entorno del usuario**.
