# TourSmart RD — Panel de control de adelantos (fase 5.17)

## Objetivo
Consultar todos los comprobantes presentados sin mezclarlos con el dinero realmente cobrado.

## Nuevo panel
- Ruta de Administrador: `http://localhost:5291/panel/finanzas/adelantos`.
- Filtro UTC desde/hasta, máximo 366 días, fechas inclusivas.
- KPI por período: total, pendientes, aprobados y rechazados, con importes reportados de cada estado.
- Dos indicadores globales de actividad actual (independientes del período): pendientes totales y pendientes cuyo vencimiento original ya pasó.
- Desglose por excursión: cantidades por estado y total de importes aprobados.
- Últimos 75 comprobantes por fecha de presentación y estado, con referencia, reserva, excursión y enlaces a revisión o pagos.
- Navegación directa desde Panel principal y Reporte financiero.

## Definiciones financieras
Un comprobante **Pendiente** es una declaración del cliente, no ingreso. Un comprobante **Aprobado** está vinculado con un registro `PagoReserva` generado después de verificación humana. Los importes aprobados en este tablero se muestran para análisis de comprobantes, pero **NO se suman** a los ingresos del informe financiero: contar ambos duplicaría el mismo dinero.

Se agrupa por fecha UTC de creación del comprobante, incluso cuando su aprobación sea posterior. El reporte de ingresos agrupa pagos y devoluciones por sus fechas de movimiento: no deben esperarse cifras idénticas para el mismo período.

## Instalación
Detener las aplicaciones con Ctrl+C:
```powershell
git checkout feature/toursmart-integrador-ii
git pull origin feature/toursmart-integrador-ii
dotnet build AgenciaToursAPI.sln
```
No hay nuevas tablas ni migraciones en esta fase. Para arrancar:
```powershell
# Terminal 1
dotnet run --project .\AgenciaToursAPI.csproj --launch-profile http
# Terminal 2
dotnet run --project .\TourSmart.Web\TourSmart.Web.csproj --launch-profile http
```

## Pruebas
1. Inicia como Administrador; entra en `/panel/finanzas/adelantos`.
2. Comprueba que aparecen los comprobantes de prueba creados y revisados en fases anteriores; filtros por fecha deben incluir correctamente el día final.
3. Verifica que Pendientes, Aprobados y Rechazados suman el total del período.
4. Compara un adelanto aprobado contra `/panel/reportes/finanzas`: no debe duplicarse como ingreso.
5. Comprueba la lista de los últimos 75 y el desglose por tour.
6. Cambia el período y verifica que los indicadores globales de pendientes no cambien por la selección.
7. Comprueba que Operador y Cliente no puedan abrir esta vista ni el endpoint `api/reportes/comprobantes`.
8. Ensaya períodos inválidos, por ejemplo más de 366 días; el formulario debe mostrar advertencia.

## Limitaciones
- No incluye pagos directamente registrados sin comprobante (esos siguen en Finanzas).
- No incluye una pasarela bancaria ni conciliación bancaria automática.
- La vista muestra como máximo 75 registros recientes y 50 excursiones, mientras que sus KPIs son agregaciones completas.
- El código compilado en GitHub no reemplaza las pruebas con SQL Server y datos reales en tu equipo.
