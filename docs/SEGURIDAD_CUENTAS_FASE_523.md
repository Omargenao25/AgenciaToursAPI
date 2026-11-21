# TourSmart RD — Seguridad de cuentas y límites públicos (fase 5.23)

## Cambios aplicados
- `POST /api/Usuarios` queda retirado (control Administrador, HTTP 410): era una ruta de alta alternativa que creaba usuarios sin ficha Cliente ni verificación. Los viajeros se registran únicamente mediante `/viajero/registro` y la API `POST /api/MiCuenta/registrar`.
- Las nuevas entidades Usuario tienen `CorreoConfirmado=false`; el registro de viajero comprueba que el servicio de verificación SMTP esté correctamente configurado ANTES de crear registros. Si falla la configuración, responde HTTP 503 y no crea fichas ni cuentas. La base de datos pasa a utilizar `DEFAULT 0` para la columna. La migración no cambia los valores existentes.
- Límites en memoria por IP: `acceso` 12/minuto, `registro` 4/10 minutos, `correo` 8/10 minutos. El exceso responde HTTP 429. Se aplica a login, registro de viajero, solicitar recuperación y reenviar verificación. Continúan los límites persistentes por cuenta de solicitudes de correo.

## Instalación con respaldo
Detener API y Web. Conservar fotos y cualquier archivo SQL modificado localmente. Consultar `git status`; guardar cambios locales antes de hacer pull, sin usar reset/clean. Respaldar la base de datos antes de aplicar la nueva migración:

```powershell
git checkout feature/toursmart-integrador-ii
git pull --ff-only origin feature/toursmart-integrador-ii
dotnet build AgenciaToursAPI.sln
dotnet ef database update --project .\AgenciaToursAPI.csproj
```

Nueva migración: `20261120212000_CorreoConfirmadoFailClosed`. Cambia el DEFAULT de `Usuarios.CorreoConfirmado` de true a false; no actualiza filas previas. **No ejecutar la migración si hay errores de compilación o falta el respaldo.**

Configuración ya utilizada en desarrollo: `EmailVerification:Enabled=true`, `EmailVerification:PublicBaseUrl=http://localhost:5291`, y `Notifications:SmtpHost`, `SmtpUser`, `SmtpPassword`, `FromEmail` configurados en User Secrets; no copiar secretos al repo ni al chat. No es necesario volver a generar una contraseña de Gmail.

## Pruebas en una base aislada
1. Cliente nuevo con SMTP habilitado: registro -> CorreoConfirmado=false -> email -> verificar -> login.
2. SMTP de verificación sin configurar: registro debe devolver 503 sin nueva fila Cliente ni Usuario. Restaurar la configuración después de probar.
3. `POST /api/Usuarios` con Administrador autenticado devuelve 410 sin crear usuarios; anónimo devuelve 401.
4. Más de 12 intentos login en un minuto desde la misma IP: algunos devuelven 429. Al terminar la ventana se rehabilita la capacidad.
5. Recuperación y reenvío de verificación siguen enviando solamente dentro de sus límites; las rutas deben responder 429 si se supera su cuota.
6. Revisar que los usuarios existentes mantienen su estado de confirmación y sus contraseñas y que las reservas no cambiaron.
7. Roles Cliente/Operador/Administrador mantienen sus restricciones actuales; ejecutar scripts de auditoría de solo lectura.

## Limitaciones
El rate limiting está en memoria por proceso e IP. No sustituye un WAF, CAPTCHA, políticas distribuidas ni protección sofisticada contra ataques. En un despliegue detrás de un reverse proxy se debe configurar correctamente `ForwardedHeaders` confiable antes de confiar en IPs reales; de lo contrario puede agruparse a muchos usuarios bajo la IP del proxy. No desplegar en internet sin verificar el entorno, TLS, backups, protección contra enumeración por tiempos y pruebas de integración.

Los flujos de recordatorios, horarios, detalles de salida y pruebas automatizadas de negocio siguen en la cola de mejoras. La CI confirma compilación/publicación, no operaciones SQL en el equipo.
