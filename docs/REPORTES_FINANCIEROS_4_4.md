# TourSmart RD — Fase 4.4: reporte financiero (caja registrada)

## Funcionalidad
- Panel en `/panel/reportes/finanzas`, accesible desde el dashboard del Administrador.
- API read-only: `GET /api/reportes/finanzas?desde=2026-11-01&hasta=2026-11-30` (JWT Administrador; HTTP 403 para Operador y Cliente).
- Filtro por fechas calendario **UTC**, ambos días incluidos. Si no se pasan fechas, utiliza del primer día del mes UTC al día actual UTC. Límite de 366 días.
- KPI 1: **Cobrado** = suma de importes de `PagosReserva` registrados en el período.
- KPI 2: **Devuelto** = suma de importes de `ReembolsosReserva` registrados en el período.
- KPI 3: **Flujo neto registrado** = cobrado - devuelto. Puede ser negativo si la devolución se registró en un mes distinto al pago original; esto es correcto para un reporte de movimientos por fecha de registro.
- Cantidad de pagos y cantidad de devoluciones del período.
- Listado unificado de movimientos, orden descendente, con tipo, importe, fecha UTC, método, referencia, responsable, cliente, tour y enlace al detalle de reserva.
- Por rendimiento solo lista los 500 movimientos más recientes del período. Los **totales no se truncan**: la API los calcula sobre todas las filas dentro del período.
- Ni el reporte ni sus consultas crean/modifican pagos, reembolsos o reservas.
- Respuestas API y web configuradas con `Cache-Control: private, no-store`.
- El reporte muestra **movimientos registrados por la agencia**, no conciliación bancaria, ventas devengadas, beneficio neto ni comprobantes fiscales. Solo considera fechas de registro de los movimientos.
- No hay migración nueva en esta fase.

## Instalación
1. Detener la API y el proyecto MVC.
2. Ejecutar `git status`. Si hay cambios locales pendientes, resolverlos antes del pull.
3. `git pull origin feature/toursmart-integrador-ii`
4. `dotnet build AgenciaToursAPI.sln`
5. Volver a iniciar la API y MVC. No hay `dotnet ef database update` nuevo para esta fase.

## Pruebas guiadas
1. Con Administrador, entrar en `http://localhost:5291/panel` y pulsar **Reporte financiero**.
2. Seleccionar un intervalo UTC que incluya los movimientos ficticios creados en la Fase 4.3.
3. Comprobar que un pago ficticio de RD$ 2,000 y dos devoluciones de RD$ 500 y RD$ 1,500 sumen Cobrado RD$ 2,000, Devuelto RD$ 2,000 y Neto RD$ 0 (si **solo** existen estos movimientos en el intervalo; si hay otros, sumarlos también).
4. Ajustar el rango a un día sin movimientos; comprobar todos los totales en 0.
5. Comprobar que un movimiento de otro mes no se incluye en la selección actual.
6. Abrir la referencia de reserva # desde un movimiento: debe navegar a Pagos / saldo sin perder el registro.
7. Con Operador, la web no debe ofrecer el enlace y debe negar el reporte. Con Cliente, igualmente prohibido. La API directa también debe responder 403.
8. Comprobar que se preservan los pagos y las devoluciones originales: el reporte es solo lectura.
9. Si se necesitan intervalos superiores a 366 días, solicitar un reporte por segmentos mensuales o anuales.
10. Si hay más de 500 movimientos en un intervalo, el panel muestra advertencia: el listado es limitado, pero totales siguen siendo completos.
11. Ejecutar la prueba de vencimiento seguro de Fase 4.2, aún pendiente, siguiendo `docs/VENCIMIENTO_RESERVAS_4_2.md`.

## Pendientes
- Exportación CSV añadida en Fase 4.5: consulta `docs/EXPORTACION_CSV_4_5.md`. La exportación PDF todavía está pendiente.
- Agrupación por tour, método, cliente y períodos; indicadores de cobros por cobrar.
- Conciliación bancaria contra extractos reales, comprobantes y autorizaciones de reembolsos.
- Pruebas automatizadas de integración y performance; compilación y prueba real en la PC del usuario pendientes.
