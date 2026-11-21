# TourSmart RD — Fase 5.32: seguimiento de reservas y adelantos del cliente

**Rama de trabajo:** `feature/toursmart-visual-530`. La rama estable `feature/toursmart-integrador-ii` queda intacta.

## Cambios incorporados

- **Mis reservas**: cada tarjeta informa qué hacer según su estado (Pendiente, Confirmada o Cancelada), con acceso adicional a la ficha pública de excursión cuando existe un TourId válido. Conserva las acciones POST originales y la protección antifalsificación para cancelar.
- **Adelantos de mi reserva**: nueva secuencia visual informativa (solicitud, adelanto, confirmación), estado de reserva destacado, métricas originales, explicación de revisión, historial con etiquetas visuales y aviso al seleccionar archivo.
- **Carga de comprobantes**: el navegador avisa para archivos vacíos o superiores a 2 MB; el controlador y la API conservan validaciones de tamaño, autorización, saldo y verificación manual. La validación adicional de navegador es orientativa, no sustituye al backend.
- **Comprobante**: enlace de ida y vuelta al seguimiento del adelanto.
- **CSS premium**: estilos responsivos para estas pantallas sin alterar los modelos ni migraciones.

## Regla financiera que no cambia
Reportar una transferencia significa **solicitar la revisión de un comprobante**, no acreditar un pago ni confirmar la reserva. Solo la agencia puede verificar el dinero recibido y registrar el abono mediante el proceso autorizado. No se envían correos ni se modifican pagos por estos cambios visuales.

## Actualizar con cuidado

Detén ambos procesos con Ctrl+C; desde la carpeta del proyecto:

```powershell
git status
git fetch origin
git switch feature/toursmart-visual-530
git pull --ff-only origin feature/toursmart-visual-530
dotnet build AgenciaToursAPI.sln
dotnet test .\TourSmart.Tests\TourSmart.Tests.csproj
```

Si un archivo SQL editado bloquea el cambio de rama o pull, **no descartarlo**. Respalda sus modificaciones:

```powershell
git stash push -m "SQL antes fase 5.32" -- Script-Sql/AgenciaToursDB.sql
git pull --ff-only origin feature/toursmart-visual-530
git stash list
git stash apply 'stash@{0}'
```

Comprueba que `stash@{0}` corresponde al respaldo que acabas de crear; detén el procedimiento si aparece un conflicto. No borrar `TourSmart.Web/wwwroot/uploads/`, ni ejecutar `git reset --hard` o `git clean -fd`.

Inicia API y Web:

```powershell
dotnet run --project .\AgenciaToursAPI.csproj --launch-profile http
```

En otra terminal:

```powershell
dotnet run --project .\TourSmart.Web\TourSmart.Web.csproj --launch-profile http
```

## Pruebas manuales con reserva y comprobantes ficticios

1. Inicia sesión con cuenta de prueba Cliente; visita `http://localhost:5291/viajero/reservas`. Verifica que cada tarjeta muestre el estado correcto y su consejo pertinente, sin enseñar reservas ajenas.
2. Pulsa **Adelanto / pago** de una reserva pendiente. Comprueba la secuencia visual; cifras de total, mínimo, faltante, plazo e historial deben coincidir con los datos reales de la API.
3. Con **comprobante ficticio** y en ambiente de prueba, elige un PDF o imagen de menos de 2 MB. El navegador debe mostrar nombre y tamaño. Con archivo mayor a 2 MB, el navegador debe advertir e impedir el envío; comprobar igualmente que el backend lo rechaza.
4. Verifica que un comprobante pendiente de revisión **no** aparece como dinero aprobado ni confirma el tour.
5. Verifica la apariencia de reservas confirmadas/canceladas, los enlaces a detalles y a comprobante imprimible.
6. Comprueba la pantalla a 375px, 768px y escritorio, y la navegación con teclado.
7. Ejecuta `powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\Test-TourSmartSmoke528.ps1`.

## Alcance de la validación
GitHub Actions comprueba compilación, xUnit y publicación de API/Web. No simula pagos, SMTP, SQL Server ni interacción visual de navegador. Por lo tanto, el recorrido posreserva debe probarse en tu equipo antes de fusionar el diseño.
