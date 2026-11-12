# TourSmart RD — Fase de notificaciones por correo (SMTP)

## Implementación
La API incluye `NotificacionesReservaService`, registrado en inyección de dependencias. Puede enviar un correo de texto al email del cliente después de:
- Crear una reserva desde la administración o desde la cuenta del cliente (`Pendiente`).
- Confirmar una reserva desde la administración (`Confirmada`).
- Cancelar una reserva desde la administración o la cuenta del cliente (`Cancelada`).

Se envía **después** del commit de SQL Server. Los datos provienen de consultas al servidor (nunca de direcciones elegidas por el usuario del endpoint). Los correos no contienen tokens, contraseñas ni documentos de identidad.

**La función está desactivada por defecto** y no envía absolutamente nada hasta que `Notifications:Enabled` sea true Y haya credenciales SMTP completas.

**Limitación:** envío inmediato de mejor esfuerzo. Si SMTP falla se registra el error sin cancelar ni revertir la reserva. No hay cola persistente ni reintentos automáticos; futuros despliegues deberían incorporar una bandeja de salida transaccional (outbox).

## Configuración local sin guardar secretos en Git
Desde la raíz del repositorio, con .NET SDK instalado:
```powershell
dotnet user-secrets init --project .\AgenciaToursAPI.csproj
dotnet user-secrets set "Notifications:SmtpHost" "smtp.gmail.com" --project .\AgenciaToursAPI.csproj
dotnet user-secrets set "Notifications:SmtpPort" "587" --project .\AgenciaToursAPI.csproj
dotnet user-secrets set "Notifications:SmtpUser" "CORREO_DE_LA_AGENCIA" --project .\AgenciaToursAPI.csproj
dotnet user-secrets set "Notifications:FromEmail" "CORREO_DE_LA_AGENCIA" --project .\AgenciaToursAPI.csproj
# Nunca publiques esta contraseña. El comando siguiente deja un marcador que debes reemplazar.
dotnet user-secrets set "Notifications:SmtpPassword" "CONTRASENA_DE_APLICACION_SMTP" --project .\AgenciaToursAPI.csproj
dotnet user-secrets set "Notifications:Enabled" "true" --project .\AgenciaToursAPI.csproj
```
Con Gmail se necesita una **contraseña de aplicación** compatible con la cuenta; no uses la contraseña normal de Gmail. User Secrets es para desarrollo, no es un almacén cifrado de producción.

Si no deseas enviar correos reales:
```powershell
dotnet user-secrets set "Notifications:Enabled" "false" --project .\AgenciaToursAPI.csproj
```

## Pruebas
1. Tener API y Web levantadas, con un cliente de prueba que use un correo de tu propiedad.
2. Crear una reserva: comprobar que el correo diga `Pendiente`, pasajeros y total, sin afirmar que está pagada.
3. Registrar pago y confirmar según la regla del adelanto: comprobar correo de confirmación.
4. Cancelar una reserva sin pagos, o con reembolso íntegro conciliado: comprobar aviso de cancelación.
5. Repetir una confirmación o cancelación ya realizada: no debería enviar una segunda notificación.
6. Desactivar SMTP y crear una reserva: la reserva debe guardarse sin intentar enviar correo.
7. Bloquear la salida SMTP temporalmente y comprobar que no cambian los estados de la base.

## Consideraciones
- Al confirmar deben cumplirse las reglas existentes de adelanto mínimo, plazo y capacidad; el correo no las altera.
- Este código todavía no notifica sobre expiración automática ni registro de cada pago: esos eventos necesitarán su integración aparte.
- No hubo cambios de esquema ni nuevas migraciones.
- Para producción usar un proveedor de correo transaccional, secretos seguros y una cola con reintentos.
