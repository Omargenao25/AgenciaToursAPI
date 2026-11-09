# TourSmart RD — Auditoría funcional de geografía (Fase 5.8)

La verificación automatizada de países, destinos y tours consulta exclusivamente los tres endpoints GET públicos. No necesita usuarios de prueba ni modifica registros.

## Iniciar el proyecto
Desde la raíz, abrir dos terminales de PowerShell.

**Terminal 1 — API**
```powershell
dotnet run --project .\AgenciaToursAPI.csproj --launch-profile http
```

**Terminal 2 — Web**
```powershell
dotnet run --project .\TourSmart.Web\TourSmart.Web.csproj --launch-profile http
```

**Terminal 3 opcional — prueba de solo lectura**
```powershell
.\scripts\Test-TourSmartGeografia.ps1
```

En la tercera terminal debe aparecer `[OK]` y un conteo de países, destinos y tours. Si hay incidencias, identificar los ID sin borrar datos a ciegas. El script detecta países duplicados (incluyendo variaciones de mayúsculas/acentos), destinos duplicados dentro de un país, relaciones rotas, duración inválida y duraciones de tours distintas a las del destino.

## Límites y criterio de aceptación
- Esta prueba HTTP no demuestra que existan índices únicos ni que la migración esté instalada en SQL Server.
- Los datos proceden de llamadas GET; para revisar tablas directamente también se dispone de `Script-Sql/VerificarGeografiaAntesDeMigrar.sql`.
- Para aplicar `20261109170000_IntegridadGeografia`, verificar primero un respaldo probado, el entorno SQL correcto y cero incidencias en el script SQL.
- El proyecto no debe fusionarse con `master` ni desplegarse por una compilación verde únicamente.
- Pruebas de escrituras concurrentes deben realizarse solamente sobre una base de pruebas aislada y con datos ficticios.
