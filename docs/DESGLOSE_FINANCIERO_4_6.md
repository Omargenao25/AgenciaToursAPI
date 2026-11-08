# TourSmart RD — Fase 4.6: análisis financiero por tour y método de pago

## Alcance
- El reporte existente `GET /api/reportes/finanzas?desde=AAAA-MM-DD&hasta=AAAA-MM-DD` agrega dos colecciones nuevas: `porTour` y `porMetodoPago`.
- Ambas colecciones contienen identificador, nombre, cobrado, devuelto, neto, cantidad de cobros y cantidad de devoluciones.
- Los agrupamientos se ejecutan **directamente en SQL Server** sobre la totalidad de movimientos en el período UTC. No dependen del límite de 500 filas del historial visual.
- Se agrupa por el identificador del tour o del método. Dos tours con el mismo nombre siguen figurando como dos entradas.
- La devolución se atribuye al tour de la reserva original; en el reporte por método, a **cómo se devolvió** el dinero, no necesariamente a cómo se recibió.
- `Neto = Cobrado - Devuelto` por grupo. Puede resultar negativo en períodos donde hubo devoluciones de pagos anteriores.
- El panel del Administrador ahora tiene dos tablas: **Rendimiento financiero por tour** y **Movimientos por método**.
- Se respetan los mismos filtros (fechas UTC hasta 366 días) y autorización exclusiva de Administrador.
- **Sin migraciones nuevas**; no modifica pagos ni reembolsos, no implica rentabilidad ni ventas reales cobradas bancariamente.
- El CSV actual de Fase 4.5 permanece igual, como listado detallado de movimientos.

## Instalación
Detener API y MVC. Verificar `git status`. Si el árbol está limpio:
```powershell
git pull origin feature/toursmart-integrador-ii
dotnet build AgenciaToursAPI.sln
```
No se necesita `dotnet ef database update`.

En dos terminales diferentes:
```powershell
dotnet run --project .\AgenciaToursAPI.csproj --launch-profile http
dotnet run --project .\TourSmart.Web\TourSmart.Web.csproj --launch-profile http
```

## Prueba funcional
1. Con rol Administrador, abrir `http://localhost:5291/panel/reportes/finanzas`.
2. Seleccionar el período UTC que incluya movimientos ficticios anteriores.
3. Confirmar que aparecen dos tablas nuevas: por tour y por método.
4. **Sumar las columnas de Cobrado de todos los tours**; debe igualar el KPI **Cobros registrados**. Repetir con Devuelto y Neto.
5. Repetir el mismo control sumando cada método. Los tres totales deben coincidir con los KPI.
6. Verificar un tour con reembolsos: el neto de ese tour debe reducirse, conservando el cobro histórico.
7. Probar un día sin movimientos: tablas vacías y KPI en cero.
8. Verificar que un Operador y un Cliente no pueden acceder al reporte web ni API (HTTP 403).
9. Descargar CSV: debe conservar las mismas columnas detalladas y los importes numéricos, sin alterar las operaciones.

## Pendientes
- Gráficos y tendencias por mes; métricas de ocupación y reservas futuras.
- Indicadores de saldos por cobrar, distinguibles del flujo de caja.
- Exportaciones de agregados específicos (no confundir con CSV detallado existente).
- Pruebas automatizadas de integración con EF Core SQL Server y concurrencia de movimientos.
- Compilación y prueba de interfaz reales pendientes en la PC del usuario.
