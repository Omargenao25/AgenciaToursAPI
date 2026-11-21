# TourSmart RD — Fase 5.28: auditoría de entrega y estabilización

## Alcance de esta fase
- Añadido `scripts/Test-TourSmartSmoke528.ps1`: prueba de humo **de solo lectura** de API y Web locales. Inspecciona lista y detalle de tours, presencia de los campos `puntoEncuentro`, `instruccionesSalida`, `queLlevar`, página pública de tour, inicio Web y denegación 401 de rutas privadas sin JWT. **No** crea usuarios, reservas, transferencias ni envía correos.
- Corregido el valor por defecto de fecha en `TourFormularioViewModel` para usar `HorarioAgencia.Ahora.Date.AddDays(7)`; ya no depende de la zona horaria del proceso Web.
- Ampliado `scripts/Test-TourSmartPreDespliegue.ps1` para comprobar existencia del proyecto de tests, el nuevo smoke script y las últimas migraciones.
- GitHub Actions sigue compilando, ejecutando las pruebas xUnit y publicando artefactos API/Web. Esto **no** prueba conexión local SQL Server ni Gmail.

## Preparación en Windows / PowerShell
1. Detén API y Web pulsando Ctrl+C en ambas terminales. Conserva el backup de SQL Server, tus fotos en `TourSmart.Web/wwwroot/uploads/` y modificaciones en `Script-Sql/AgenciaToursDB.sql`.
2. Revisa `git status`. Si el archivo SQL editado bloquea el pull:
```powershell
git stash push -m "Respaldo SQL fase 5.28" -- Script-Sql/AgenciaToursDB.sql
git pull --ff-only origin feature/toursmart-integrador-ii
git stash apply 'stash@{0}'
```
Si aparece conflicto, no lo sobrescribas y comprueba el stash antes de avanzar.
3. Ejecuta:
```powershell
dotnet restore AgenciaToursAPI.sln
dotnet build AgenciaToursAPI.sln
dotnet test .\TourSmart.Tests\TourSmart.Tests.csproj
```
**Esta fase no contiene migración nueva**. Si aún no aplicaste 5.25/5.26, respalda antes la base SQL y ejecuta `dotnet ef database update --project .\AgenciaToursAPI.csproj` solo después de confirmar build y backup.

## Iniciar API y Web
En terminal 1:
```powershell
dotnet run --project .\AgenciaToursAPI.csproj --launch-profile http
```
En terminal 2:
```powershell
dotnet run --project .\TourSmart.Web\TourSmart.Web.csproj --launch-profile http
```

## Prueba HTTP rápida (tercera terminal)
La ejecución de scripts puede estar bloqueada por política de Windows. Puedes invocar una sesión aislada:
```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\Test-TourSmartSmoke528.ps1
```
Si deseas verificar un tour específico que tenga configurado su punto de encuentro:
```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\Test-TourSmartSmoke528.ps1 -TourId 1
```
Reemplaza 1 por ID real. Sin `-TourId`, se intenta seleccionar una excursión con datos de salida y, si no existe, el primer tour. El script informa sobre los datos de logística ausentes sin fallar artificialmente.

La verificación de 401 **no sustituye** las pruebas de permisos 403 entre Cliente/Operador/Administrador; ejecuta el auditor existente en una base de prueba con las cuentas preparadas:
```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\Invoke-TourSmartAuditoriaFinal.ps1 -TourId 1 -ReservaId 1
```
No envíes contraseñas ni tokens al chat.

## Criterios de aceptación
1. `dotnet build` y `dotnet test` completan sin fallos.
2. `Test-TourSmartPreDespliegue.ps1` informa 0 errores (avisos operativos son esperados).
3. Smoke HTTP informa 0 errores con la API y Web en ejecución.
4. Cliente ve punto de encuentro/instrucciones guardados en un tour real.
5. Crear y cancelar una **reserva de prueba** no modifica cupos indebidamente. Una reserva ajena no es visible para un cliente distinto. Pagos, SMTP y reintentos deben probarse solo en ambiente aislado.
6. Respaldo de SQL e imágenes comprobado; rutas HTTP, secretos y usuarios de demo preparados para la exposición.

## Resultado y limitaciones
Este es un cierre técnico preliminar, no una certificación de seguridad comercial. Falta ejecutar las pruebas HTTP/SQL/SMTP en el entorno local de demostración y efectuar un ensayo completo. No usar el script como autorización para desplegar públicamente.
