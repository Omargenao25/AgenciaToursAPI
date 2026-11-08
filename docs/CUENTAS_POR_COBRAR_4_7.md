# TourSmart RD — Fase 4.7: control de saldos por cobrar

## Objetivo
Distinguir lo pendiente de cobro en reservas **Confirmadas** de las solicitudes **Pendientes** que todavía no constituyen ventas confirmadas.

## Implementación
- API protegida: `GET /api/reportes/cuentas-por-cobrar` con JWT de rol **Administrador**.
- Página: `/panel/reportes/cuentas-por-cobrar`, accesible desde el dashboard del Administrador.
- Muestra únicamente reservas no Canceladas de tours **que todavía no han comenzado**.
- Para cada reserva, consulta total, cobros históricos, reembolsos históricos, dinero neto (`cobrado - devuelto`), saldo (`max(0, total - neto)`), estado y fecha de salida.
- Reserva Confirmada: el saldo figura en **Saldo de reservas confirmadas**.
- Reserva Pendiente: su saldo se muestra por separado como **Saldo de solicitudes pendientes**. No debe presentarse como cobro asegurado.
- El adelanto mínimo está calculado con la política del tour, redondeando hacia arriba al centavo. Cuando una solicitud Pendiente no lo alcanza, muestra cuánto falta.
- Alertas de revisión: reserva Pendiente con fecha límite vencida sin pagos (esperando que la procese el servicio de Fase 4.2), o vencida con pagos (revisión humana y posible conciliación).
- Las filas se ordenan por fecha y hora de inicio del tour. Contienen enlace a `Pagos / saldo`.
- La API suma los cobros y devoluciones por reserva con subconsultas a SQL Server; no confunde el historial bruto con efectivo neto.
- El reporte es únicamente de consulta, sin nuevos campos ni migraciones. No cobra automáticamente ni edita reservas.
- Todas las filas de reservas futuras se consultan en una sola petición. Para un despliegue con gran volumen convendrá paginación y agregados separados.

## Instalación
Detener API y MVC. Ejecutar desde la raíz del repositorio:
```powershell
git status
git pull origin feature/toursmart-integrador-ii
dotnet build AgenciaToursAPI.sln
```
Resolver modificaciones locales pendientes antes del `git pull`.

Sin nueva migración. Arrancar en terminales separadas:
```powershell
dotnet run --project .\AgenciaToursAPI.csproj --launch-profile http
dotnet run --project .\TourSmart.Web\TourSmart.Web.csproj --launch-profile http
```

## Primera prueba
1. Acceder como Administrador al dashboard; abrir **Cuentas por cobrar**.
2. Crear o reutilizar reservas ficticias **de tours futuros**.
3. Reserva Confirmada total RD$ 8,000 con pagos netos RD$ 2,400: debe mostrar saldo confirmado RD$ 5,600.
4. Reserva Pendiente total RD$ 6,000 sin pagos: debe añadir RD$ 6,000 solamente a solicitudes pendientes, no a saldo confirmado.
5. Reserva Pendiente con pagos parciales por debajo del mínimo: debe mostrar `Faltan RD$ ... de adelanto`.
6. Reserva cancelada con reembolso completo: NO debe aparecer.
7. Reserva con salida pasada: NO debe aparecer aunque tenga saldo; el reporte es de **próximas salidas**, no gestión de deuda histórica.
8. Comprobar que los importes netos descuentan cualquier reembolso y que la suma de saldos por estado coincide con los KPI superiores.
9. Abrir **Pagos / saldo** desde una fila y verificar que no cambian las cifras si no registras pagos adicionales.
10. Intentar abrir la página y el endpoint de API con Operador o Cliente: acceso denegado, sin filtrar información.
11. Confirmar que esta consulta no modifica tablas, pagos ni estados.
12. Aún queda pendiente la prueba controlada de vencimiento sin pagos de Fase 4.2, según `docs/VENCIMIENTO_RESERVAS_4_2.md`.

## Limitaciones
- La hora del tour sigue la convención de hora local existente en TourSmart RD, mientras que los plazos de adelanto y la fecha de consulta se guardan en UTC.
- El «saldo confirmado» no implica que el dinero sea jurídicamente exigible según un contrato; únicamente registra cuánto falta por pagar en reservas confirmadas del sistema.
- No contempla fechas de vencimiento individuales para el saldo restante, ni políticas de penalización o vencimiento de deuda histórica.
- La compilación y prueba de integración en el equipo del usuario siguen pendientes.
