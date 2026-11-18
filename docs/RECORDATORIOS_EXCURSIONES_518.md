# TourSmart RD — Recordatorios de excursiones (5.18)

La API revisa cada minuto las reservas confirmadas de excursiones futuras, no cerradas operativamente. Se utiliza la hora local del servidor, igual que el módulo de reservas; mantener la zona horaria de Santo Domingo en el servidor de demostración. El aviso incluye nombre del tour, reserva, salida, pasajeros y contacto oficial. Como no existe un campo de punto de encuentro, indica consultar a la agencia.

La migración 20261117210000_RecordatoriosExcursiones agrega dos columnas nullable en Reservas: RecordatorioIntentadoUtc y RecordatorioEnviadoUtc. Una actualización atómica reclama el intento antes de SMTP; una segunda instancia no puede reclamarlo. Tras éxito SMTP se registra EnviadoUtc. No implica entrega en bandeja. Si SMTP falla, queda el intento sin confirmación y se registra el error; no hay reintento automático para evitar duplicados ante respuestas inciertas. Un fallo o reinicio entre la marca y el envío puede dejar un aviso sin entregar. Una cancelación concurrente inmediatamente después de la última comprobación puede coincidir con un envío. No se envía un segundo recordatorio al cambiar el horario de una reserva que ya tuvo intento.

## Activación local

Detener ambas aplicaciones, respaldar SQL Server y ejecutar desde la raíz:

```powershell
git checkout feature/toursmart-integrador-ii
git pull origin feature/toursmart-integrador-ii
dotnet build AgenciaToursAPI.sln
dotnet ef database update --project .\AgenciaToursAPI.csproj
dotnet user-secrets set "Reminders:Enabled" "true" --project .\AgenciaToursAPI.csproj
dotnet user-secrets set "Reminders:HoursBefore" "24" --project .\AgenciaToursAPI.csproj
```

Se requiere Notifications:Enabled=true y las credenciales SMTP existentes. El recordatorio está desactivado por defecto.

## Prueba aislada
1. Utilizar una base de pruebas y un correo propio.
2. Crear tour futuro que comience dentro de las próximas 24 horas; registrar y confirmar una reserva de prueba siguiendo las reglas de adelanto.
3. Esperar una pasada del servicio y comprobar el correo y la marca de envío.
4. Reiniciar la API y verificar que no se repita.
5. Probar otra reserva pendiente o cancelada: no debe recibir aviso.
6. Comprobar que un tour fuera de la ventana no genera recordatorio.
7. Consultar las marcas SQL, sin cambiar los estados ni reiniciar marcas en producción:
   SELECT Id, Estado, RecordatorioIntentadoUtc, RecordatorioEnviadoUtc FROM Reservas;
8. Desactivar Reminders:Enabled si no se desean nuevos envíos. El servicio requiere que la API siga ejecutándose; no funciona con el equipo apagado.

