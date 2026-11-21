# TourSmart RD — Fase 5.25: recordatorios con estado y reintentos controlados

## Cambios
Esta fase refuerza únicamente el recordatorio previo a una excursión confirmada. No cambia pagos, cupos, correo de recuperación ni las notificaciones ordinarias de creación/confirmación/cancelación.

La API ahora guarda en `Reservas`:
- `RecordatorioEstado`: Pendiente, Enviando, Fallido, Enviado, Revisar o Agotado.
- `RecordatorioIntentos`: 0 a 3.
- `RecordatorioProximoIntentoUtc`, `RecordatorioLeaseHastaUtc`, `RecordatorioLeaseToken`.
- Se conservan `RecordatorioIntentadoUtc` y `RecordatorioEnviadoUtc` por compatibilidad.

La migración `20261120223000_RecordatoriosReintentosControlados` marca como Enviado los correos históricos con `RecordatorioEnviadoUtc`, y como Revisar los intentos históricos sin confirmación. Así no se reenvían accidentalmente al actualizar.

Cada minuto, el servicio obtiene las reservas confirmadas que saldrán dentro de la ventana configurada; reserva de forma atómica un lease de dos minutos y marca Enviando. Un envío aceptado por SMTP queda Enviado. Si SMTP responde con un rechazo temporal reconocido (ServiceNotAvailable o MailboxBusy), pasa a Fallido y vuelve a intentarse después de 2 o 5 minutos si no han empezado la excursión y todavía quedan intentos. Máximo tres intentos por reserva. Si la entrega es incierta, el proceso termina abruptamente o caduca el lease, queda Revisar, sin reenvío automático. Los estados no afirman entrega en la bandeja del destinatario.

**No existe garantía de exactamente una entrega vía SMTP**: incluso un rechazo o una excepción puede ser ambiguo; los errores temporales seleccionados pueden ocasionar algún duplicado. Los estados Revisar requieren supervisión manual. No cambiar estados directamente en producción sin verificar la bandeja y los registros.

## Cómo actualizar en desarrollo
1. Detener API y Web con Ctrl+C y guardar copia de seguridad de la base SQL Server.
2. En la raíz `AgenciaToursAPI`, revisar `git status` y preservar todo cambio local/fotografías antes del pull; evitar `git reset --hard` y `git clean -fd`.
3. Ejecutar:

```powershell
git checkout feature/toursmart-integrador-ii
git pull --ff-only origin feature/toursmart-integrador-ii
dotnet build AgenciaToursAPI.sln
dotnet ef database update --project .\AgenciaToursAPI.csproj
```

4. Confirmar los secretos actuales de SMTP, `Notifications:Enabled` y `Reminders:Enabled`; no compartir credenciales. Para activar los recordatorios en desarrollo (API):

```powershell
dotnet user-secrets set "Reminders:Enabled" "true" --project .\AgenciaToursAPI.csproj
dotnet user-secrets set "Reminders:HoursBefore" "24" --project .\AgenciaToursAPI.csproj
```

5. Terminal API:
```powershell
dotnet run --project .\AgenciaToursAPI.csproj --launch-profile http
```

6. Terminal Web:
```powershell
dotnet run --project .\TourSmart.Web\TourSmart.Web.csproj --launch-profile http
```

## Pruebas aisladas recomendadas
- Crear un tour futuro y una reserva confirmada con adelanto registrado bajo reglas normales. Configurar correo propio. Verificar que se marca `Enviado` al aceptar Gmail, y que reiniciar la API no reenvía el mismo recordatorio.
- Usar un SMTP de pruebas controlado que **rechace antes de aceptar el mensaje** con código MailboxBusy o ServiceNotAvailable; comprobar estado Fallido, próximo intento y límite de tres (no simular por alterar tablas).
- Comprobar que reservas Pendiente/Cancelada, excursiones pasadas o cerradas nunca reciben aviso.
- Interrumpir el proceso durante un ensayo en entorno aislado: el lease vencido debe pasar a Revisar y no reintentarse de manera automática.
- Verificar que el estado de las reservas históricas y sus pagos/cupos se mantiene.
- Ejecutar las pruebas existentes de reservas y seguridad.

Consulta de diagnóstico en SQL Server (solo lectura):

```sql
SELECT Id, Estado, RecordatorioEstado, RecordatorioIntentos,
       RecordatorioIntentadoUtc, RecordatorioEnviadoUtc,
       RecordatorioProximoIntentoUtc, RecordatorioLeaseHastaUtc
FROM dbo.Reservas ORDER BY Id DESC;
```

## Restricciones
- No desplegar un worker multi-instancia comercial sin revisar concurrencia bajo carga, SQL y entrega real. El lease reduce duplicación concurrente, pero no garantiza entrega única frente a fallos de red o servidor.
- Un correo en Revisar no se reenvía automáticamente. Considerar una UI administrativa de revisión manual y proveedor transaccional con identificadores persistentes en una iteración posterior.
- CI compila/publica, no ejecuta la migración ni valida SMTP/SQL Server con datos reales.
