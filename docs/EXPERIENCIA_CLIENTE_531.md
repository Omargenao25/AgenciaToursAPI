# TourSmart RD — Fase 5.31: experiencia del viajero

Rama de diseño: `feature/toursmart-visual-530`. Rama estable sin cambios: `feature/toursmart-integrador-ii`.

## Qué incluye
- **Reservar excursión**: flujo visual de 3 etapas informativas, nombres de pasajeros, selección del método previsto, resumen lateral con fotografía y datos reales, cálculo dinámico de total y adelanto estimados. Es una **solicitud pendiente**, no cobra ni confirma automáticamente.
- **Mis reservas**: tarjetas individuales con número, fecha, viajeros, total, estado y enlaces a comprobante/adelanto. La cancelación conserva POST, antifalsificación y confirmación previa.
- **Mi perfil**: nueva cabecera e información de identidad mejor organizada. Identificación y correo continúan siendo de solo lectura.
- **Registro e inicio de sesión**: diseño coherente con el resto de la web. Mantiene campos, POST y validaciones existentes.
- **Comprobante**: presentación más cuidada sin alterar la información ni el comportamiento de impresión.
- **Regla de cancelación**: `ReservaPanel.TourPorIniciar` usa `HorarioAgencia.Ahora`, respetando Santo Domingo incluso si el servidor funciona en UTC.
- Nuevo bloque CSS responsive agregado a `wwwroot/css/toursmart-premium-530.css`.

## Qué no cambia
No se modifican contratos de API, SQL Server, estados de pago, reglas de confirmación, cupos ni permisos. No se requiere migración. Se conservan las fotografías subidas del usuario en `TourSmart.Web/wwwroot/uploads/`.

## Actualización local
Detén API y Web con Ctrl+C.
```powershell
git status
git fetch origin
git switch feature/toursmart-visual-530
git pull --ff-only origin feature/toursmart-visual-530
dotnet restore AgenciaToursAPI.sln
dotnet build AgenciaToursAPI.sln
dotnet test .\TourSmart.Tests\TourSmart.Tests.csproj
```
Si el archivo SQL modificado bloquea el pull, primero ejecutar `git stash push -m "Respaldo SQL antes de fase 5.31" -- Script-Sql/AgenciaToursDB.sql`, luego `git pull --ff-only origin feature/toursmart-visual-530` y finalmente `git stash apply 'stash@{0}'` únicamente después de comprobar qué guarda `git stash list`. Si surge un conflicto detenerse. No borrar archivos de uploads ni ejecutar `reset --hard` o `clean -fd`.

Terminal API:
```powershell
dotnet run --project .\AgenciaToursAPI.csproj --launch-profile http
```
Terminal Web:
```powershell
dotnet run --project .\TourSmart.Web\TourSmart.Web.csproj --launch-profile http
```

## Pruebas manuales obligatorias en base de demostración
1. Abrir catálogo y un tour futuro reservable. Solicitar 1 y luego 3 viajeros: el resumen debe mostrar las cantidades y totales coherentes con el precio y porcentaje configurados.
2. Rellenar los nombres completos, elegir método, enviar una sola vez. Debe crearse una reserva **Pendiente**, no Confirmada, y sin cobro.
3. Abrir `/viajero/reservas`, confirmar datos de la tarjeta y abrir comprobante. Probar vista móvil.
4. Revisar enlace `Adelanto / pago` y comportamiento de reserva con estado Confirmada/Cancelada.
5. Probar el botón Cancelar únicamente con reserva de prueba. Debe pedir confirmación y permitir cancelar si todavía no inició el tour.
6. Verificar que la fecha de un tour por iniciar se evalúe en zona `America/Santo_Domingo`.
7. Probar registro y login con cuentas de prueba, comprobando que la verificación de correo siga requiriendo SMTP configurado cuando corresponde.
8. Revisar distribución a 375px, 768px, 1280px, navegación con teclado y `prefers-reduced-motion`.
9. Ejecutar `powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\Test-TourSmartSmoke528.ps1`.

CI revisa compilación y pruebas xUnit, pero no realiza las pruebas de interfaz, SQL Server ni SMTP. No fusionar a rama estable hasta verificar el recorrido con usuario de prueba.
