# TourSmart RD — Verificación de correo de clientes

## Objetivo
Un correo puede tener formato correcto y aun así ser falso. La verificación se hace demostrando que el cliente recibe un enlace enviado a ese buzón. Se admiten Gmail, Outlook, Yahoo y dominios de empresas; no se afirma certificación por el proveedor.

## Implementado
- Formato obligatorio de correo en registro (ya existía).
- Nuevo campo `Usuarios.CorreoConfirmado` y tabla `VerificacionesCorreo` con token aleatorio de un solo uso; la base guarda sólo el hash SHA-256, caduca en 30 minutos.
- Los usuarios existentes quedan marcados como confirmados al aplicar la migración, para no bloquear sus cuentas actuales.
- Cuando `EmailVerification:Enabled=true` Y la configuración SMTP está completa, las cuentas nuevas se registran como no confirmadas y reciben el enlace.
- Hasta verificar, el API de login deniega las cuentas nuevas sin confirmar.
- `/viajero/verificar-correo?token=...` permite confirmar mediante botón POST; `/viajero/reenviar-verificacion` permite solicitar otro enlace.
- Los reenvíos se limitan a 1 cada 2 minutos y 5 por 24 horas por usuario; la respuesta no revela si el correo existe.
- Cuenta no confirmada no puede efectuar reservas como Cliente porque no puede iniciar sesión.
- Correos de clientes no se restringen a Gmail; la comprobación consiste en la posesión del buzón, no en una lista de proveedores.
- Los mensajes de verificación se envían exclusivamente si se activa esta funcionalidad. El flujo anterior se conserva mientras está desactivada.

## Importante antes de activar
1. Respaldar la base.
2. Detener API y Web; hacer `git pull` y compilar.
3. Aplicar `dotnet ef database update --project .\AgenciaToursAPI.csproj`, que ejecutará `20261113183000_VerificacionCorreoClientes`.
4. Configurar notificaciones SMTP por User Secrets, sin subir contraseñas a Git:
```powershell
dotnet user-secrets set "Notifications:SmtpHost" "smtp.gmail.com" --project .\AgenciaToursAPI.csproj
dotnet user-secrets set "Notifications:SmtpPort" "587" --project .\AgenciaToursAPI.csproj
dotnet user-secrets set "Notifications:SmtpUser" "CORREO_REAL_DE_LA_AGENCIA" --project .\AgenciaToursAPI.csproj
dotnet user-secrets set "Notifications:FromEmail" "CORREO_REAL_DE_LA_AGENCIA" --project .\AgenciaToursAPI.csproj
# La contraseña de aplicación de Google se guarda sólo en tu computadora.
# User Secrets NO cifra los valores en reposo; sólo evita subirlos al repositorio.
dotnet user-secrets set "EmailVerification:PublicBaseUrl" "http://localhost:5291" --project .\AgenciaToursAPI.csproj
dotnet user-secrets set "EmailVerification:Enabled" "true" --project .\AgenciaToursAPI.csproj
```
El secreto `Notifications:SmtpPassword` se configura por separado de modo seguro, como se explica en `docs/NOTIFICACIONES_RESERVAS_SMTP.md`.

`Notifications:Enabled` es independiente de `EmailVerification:Enabled`. Para permitir verificación sin otros correos transaccionales se puede dejar el primero desactivado.

**No activar** `EmailVerification:Enabled` hasta confirmar que el remitente SMTP funciona; sin acceso real al buzón un usuario nuevo no podrá iniciar sesión.

## Pruebas
- Registrar cliente con un correo de prueba propio: comprobar que recibe un enlace y que no puede acceder antes de verificar.
- Abrir enlace y pulsar Confirmar; comprobar que después puede iniciar sesión.
- Repetir el enlace: debe rechazarse por uso único.
- Reenviar enlace cuando el anterior caduque. Se limita a 5 diarios y 2 minutos entre intentos.
- Probar correo incorrecto o que no existe: no debe recibir mensaje aunque el formato sea correcto.
- Probar cuentas preexistentes de Administrador/Operador/Cliente: deben seguir accediendo.
- No compartir tokens ni reenviar capturas con enlaces completos.
- Si SMTP falla, el usuario puede usar el reenvío una vez esté resuelto.

## Pendientes para producción
- Cola persistente de correo con reintentos, limpieza periódica de tokens y limitación adicional por IP para proteger de abuso.
- Proveedor de correo transaccional y URL pública HTTPS.
- Probar con múltiples buzones y límites de Gmail. La función no valida si un correo corresponde a una persona real, sólo si se puede acceder a ese buzón.
