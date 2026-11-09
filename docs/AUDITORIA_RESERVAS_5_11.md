# TourSmart RD — Fase 5.11, auditoría global de reservas y plazas

## Objetivo
Comparar todas las reservas del sistema con el catálogo de tours, sin escribir datos ni revelar información personal. Verifica:
- La existencia del tour de cada reserva.
- Estados válidos (`Pendiente`, `Confirmada`, `Cancelada`).
- Cantidad de personas entre 1 y 100.
- Total de reserva no negativo.
- Que las reservas canceladas no ocupen plazas.
- Que la ocupación sumada de reservas activas coincida con `cuposReservados` de la API.
- Que la ocupación no exceda la capacidad de cada tour.
- Que `cuposDisponibles` corresponda a capacidad menos ocupación.

## Cómo ejecutarlo
Abre PowerShell en la raíz de `AgenciaToursAPI`. Necesitas la API funcionando y un JWT de personal de **pruebas**, obtenido de forma autorizada. No compartas el token por chat, capturas ni commits.

```powershell
git pull origin feature/toursmart-integrador-ii
Set-ExecutionPolicy -Scope Process -ExecutionPolicy Bypass
# Si TOURSMART_TOKEN_ADMIN ya existe en tu sesión, no hace falta reasignarlo.
.\scripts\Test-TourSmartReservas.ps1
```

Si no tienes un token de Administrador de pruebas disponible, no inventes ni pegues uno en una captura. La prueba indica claramente si falta, sin modificar datos. Solo `GET /api/Reservas` requiere autorización de personal, mientras `GET /api/Tours` es público.

## Interpretación
- `[OK]`: las respuestas consultadas son consistentes para las reglas enumeradas.
- `[AVISO]`: condición que conviene revisar, sin afirmar un error.
- `[ERROR]`/`[FALLO]`: discrepancia a investigar. No modificar registros automáticamente.
- `[BLOQUEADO]`: falta el token autorizado.

La API procesa las solicitudes GET por separado; si se modifican reservas durante el chequeo, puede haber diferencias transitorias. Ejecutar preferiblemente en un entorno de pruebas sin actividad. Esta auditoría no prueba pagos, lista de pasajeros, check-in, SQL ni carreras de concurrencia: existen pruebas complementarias de manifiesto y geografía.

## Terminales del proyecto
Terminal 1, API:
```powershell
dotnet run --project .\AgenciaToursAPI.csproj --launch-profile http
```
Terminal 2, web:
```powershell
dotnet run --project .\TourSmart.Web\TourSmart.Web.csproj --launch-profile http
```
