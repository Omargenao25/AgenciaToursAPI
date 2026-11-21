# TourSmart RD — Fase 5.26: instrucciones y punto de encuentro por excursión

## Alcance
La agencia puede establecer en cada tour, desde **Panel → Tours → Crear/Editar**:
- Punto de encuentro (hasta 250 caracteres).
- Instrucciones de salida (hasta 1000 caracteres).
- Qué llevar (hasta 500 caracteres).

Los campos son opcionales; un tour histórico sin datos no falla ni muestra textos inventados. Se normalizan espacios exteriores y se guardan como NULL si están vacíos. El cliente los consulta en **/tours/{id}** y se incorporan en los recordatorios futuros de excursión confirmada (texto plano y HTML codificado). No se modifica el importe ni el estado de reservas ni las reglas financieras.

La migración `20261120234000_DatosSalidaExcursiones` añade tres columnas NULL a Tours y no modifica filas existentes. Los operadores deben revisar la veracidad de los puntos de encuentro y no publicar datos personales privados. El sistema muestra estos campos públicamente.

## Instalación local segura
1. Detener ambos procesos mediante Ctrl+C. Respaldar SQL Server, imágenes y cambios locales.
2. Ejecutar `git status`. Si `Script-Sql/AgenciaToursDB.sql` tiene modificaciones locales, preservar mediante `git stash push -m "SQL antes de 5.26" -- Script-Sql/AgenciaToursDB.sql`, actualizar con `git pull --ff-only origin feature/toursmart-integrador-ii` y recuperar con `git stash apply 'stash@{0}'`; si hay conflictos, detenerse para resolverlos. Nunca usar reset hard ni clean fd.
3. Compilar y después aplicar migración:

```powershell
dotnet build AgenciaToursAPI.sln
dotnet ef database update --project .\AgenciaToursAPI.csproj
```

4. Terminal 1 (API):

```powershell
dotnet run --project .\AgenciaToursAPI.csproj --launch-profile http
```

5. Terminal 2 (Web):

```powershell
dotnet run --project .\TourSmart.Web\TourSmart.Web.csproj --launch-profile http
```

## Pruebas
1. Crear un tour futuro con punto de encuentro, instrucciones y lista de artículos. Verificar que se guarda.
2. Abrir `http://localhost:5291/tours/{id}` y cotejar los textos y los saltos de línea.
3. Editar el mismo tour y cambiar únicamente los tres campos: la fecha, el precio y los cupos deben permanecer.
4. Crear o consultar un tour sin estos campos: no debe mostrar una sección vacía ni bloquear la reserva.
5. En base aislada de pruebas, usar una reserva confirmada a una salida futura próxima y SMTP propio de prueba, y comprobar que el correo de recordatorio incluya los tres campos. No reintentar con una reserva a la que ya se envió un correo; usar otra reserva de prueba.
6. Verificar que la salida y caducidad se evalúan en hora de Santo Domingo, que los estados de recordatorios permanecen correctos, y que el módulo de pagos sigue intacto.
7. No publicar datos personales o direcciones privadas en campos públicos.

## Limitaciones
Los cambios de logística realizados después de enviar un recordatorio NO generan automáticamente un correo de actualización. El sistema aún no guarda un historial de versiones de instrucciones, por lo que conviene notificar manualmente a viajeros de excursiones con reservas afectadas. Integrar auditoría de cambios y notificaciones de actualización en una fase posterior.

CI revisa compilación/publicación y no ejecuta la migración ni la prueba de SMTP local.
