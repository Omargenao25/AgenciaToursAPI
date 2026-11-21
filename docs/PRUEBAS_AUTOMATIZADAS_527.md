# TourSmart RD — Fase 5.27: pruebas automatizadas de reglas

## Implementado
Proyecto xUnit `TourSmart.Tests/TourSmart.Tests.csproj` incluido en `AgenciaToursAPI.sln`. GitHub Actions ejecuta `dotnet test` después de compilar la solución en Release y bloquea el flujo si alguna prueba falla.

`Security/ReglasReserva.cs` centraliza fórmulas puras realmente utilizadas por los controladores de reservas, pagos y comprobantes:
- `HayCupos`: impide sobreventa, desbordamiento y cantidades inválidas.
- `AdelantoMinimo`: porcentaje redondeado hacia arriba al centavo.
- `Saldo`: total menos cobros netos de reembolsos.
- `TieneAdelanto`: suficiente adelanto neto para confirmar.

`TourSmart.Tests/ReglasNegocioTests.cs` verifica escenarios favorables y límites (incluye cupos llenos y centavos), normalización/validación de pasajeros y atributos de autorización declarados en endpoints sensibles. **Estas pruebas no hacen solicitudes HTTP ni usan SQL**. Verificar un atributo `[Authorize]` no sustituye demostrar que un JWT de cliente obtiene HTTP 403. Para ello siguen siendo necesarios los scripts de seguridad y pruebas funcionales locales.

## Cómo actualizar sin perder tus cambios
1. Detén API y Web con Ctrl+C.
2. Comprueba `git status`. No borres las fotos en `TourSmart.Web/wwwroot/uploads`.
3. Si `Script-Sql/AgenciaToursDB.sql` tiene modificaciones y Git no permite pull:
```powershell
git stash push -m "Respaldo SQL para fase 5.27" -- Script-Sql/AgenciaToursDB.sql
git pull --ff-only origin feature/toursmart-integrador-ii
git stash apply 'stash@{0}'
```
Detente si aparecen conflictos; `apply` conserva el stash.
4. Compila y ejecuta pruebas:
```powershell
dotnet restore AgenciaToursAPI.sln
dotnet build AgenciaToursAPI.sln
dotnet test TourSmart.Tests/TourSmart.Tests.csproj --configuration Debug
```

## Ejecución normal
Terminal API:
```powershell
dotnet run --project .\AgenciaToursAPI.csproj --launch-profile http
```
Terminal Web:
```powershell
dotnet run --project .\TourSmart.Web\TourSmart.Web.csproj --launch-profile http
```

## Restricciones y siguiente etapa
No hay migración SQL para esta fase. Las pruebas xUnit puras no cubren EF Core SQL Server, bloqueos entre transacciones, las políticas 401/403 reales, SMTP, archivos ni la experiencia completa del navegador. Ejecutar scripts `Test-TourSmartReservas.ps1`, `Test-TourSmartSecurity.ps1` e `Invoke-TourSmartAuditoriaFinal.ps1` en entorno aislado y con datos preparados para validar esas áreas. La siguiente iteración puede incluir pruebas de integración HTTP usando WebApplicationFactory y una base SQL de pruebas, sin tocar la real.
