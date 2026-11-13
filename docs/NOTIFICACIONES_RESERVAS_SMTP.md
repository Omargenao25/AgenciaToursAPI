# TourSmart RD — Fase de notificaciones por correo (SMTP)

## Implementación
La API incluye `NotificacionesReservaService`, registrado en inyección de dependencias. Los mensajes utilizan `PlantillaCorreoReserva` con un diseño HTML (identidad azul petróleo y verde TourSmart) y alternativa de texto plano. Los campos de cliente y excursión se codifican para evitar inyección HTML. Puede enviar un correo de texto al email del cliente después de:
- Crear una reserva desde la administración o desde la cuenta del cliente (`Pendiente`).
- Confirmar una reserva desde la administración (`Confirmada`).
- Cancelar una reserva desde la administración o la cuenta del cliente (`Cancelada`).

Se envía **después** del commit de SQL Server. Los datos provienen de consultas al servidor (nunca de direcciones elegidas por el usuario del endpoint). Los correos no contienen tokens, contraseñas ni documentos de identidad.

**La función está desactivada por defecto** y no envía absolutamente nada hasta que `Notifications:Enabled` sea true Y haya credenciales SMTP completas.

**Limitación:** envío inmediato de mejor esfuerzo. Si SMTP falla se registra el error sin cancelar ni revertir la reserva. No hay cola persistente ni reintentos automáticos; futuros despliegues deberían incorporar una bandeja de salida transaccional (outbox).

## Configuración local sin guardar secretos en Git
Desde la raíz del repositorio, con .NET SDK instalado:
```powershell
dotnet user-secrets set "Notifications:SmtpHost" "smtp.gmail.com" --project .\AgenciaToursAPI.csproj
dotnet user-secrets set "Notifications:SmtpPort" "587" --project .\AgenciaToursAPI.csproj
dotnet user-secrets set "Notifications:SmtpUser" "CORREO_DE_LA_AGENCIA" --project .\AgenciaToursAPI.csproj
dotnet user-secrets set "Notifications:FromEmail" "CORREO_DE_LA_AGENCIA" --project .\AgenciaToursAPI.csproj
# La contraseña de aplicación ya utilizada para verificar correo puede reutilizarse.
# No mostrar ni copiar su valor a chats o capturas.
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

## Activación para TourSmart RD después de probar verificación de clientes
Si el correo de verificación de la cuenta ya se entregó correctamente, normalmente las credenciales `Notifications:SmtpHost`, `Notifications:SmtpUser`, `Notifications:SmtpPassword`, `Notifications:FromEmail` ya existen. Solo hay que activar los avisos de reserva desde la terminal local (nunca cambiar contraseña en GitHub):
```powershell
dotnet user-secrets set "Notifications:Enabled" "true" --project .\AgenciaToursAPI.csproj
```
Reinicia la API y la Web. Mantén `EmailVerification:Enabled=true` para seguir verificando nuevos clientes.

Prueba primero crear una **reserva de prueba** con un tour futuro y correo propio. Verifica el correo con asunto `TourSmart RD - Solicitud de reserva recibida #...`. Una solicitud Pendiente no se considera pagada ni confirmada. Para probar una confirmación, registra un adelanto de prueba con las reglas existentes y luego confirma. Para cancelar, usa una reserva sin pagos, o previamente conciliada según las reglas de devolución. No inventar pagos reales ni manipular reservas de terceros.

Esta implementación **no** incluye adjuntos, fotos externas, enlaces con tokens ni un sistema de reintentos. La imagen del tour sigue visible en la web, pero no se incrusta en correos para evitar enlaces localhost y hotlinking.
