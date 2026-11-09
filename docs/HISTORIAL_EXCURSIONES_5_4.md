# TourSmart RD — Fase 5.4: historial de excursiones cerradas

## Alcance
- Endpoint protegido para **Administrador**: `GET /api/reportes/historial-excursiones?desde=AAAA-MM-DD&hasta=AAAA-MM-DD&pagina=1`.
- Panel protegido: `/panel/reportes/historial-excursiones`, accesible desde el dashboard de Administrador.
- Reutiliza `CierresOperativosTour` creado en la Fase 5.3; **no se necesitan migraciones**.
- Incluye solamente excursiones con cierre operativo registrado. No supone que los tours simplemente pasados o finalizados por calendario tengan cierre operativo.
- Filtro por **fecha UTC del cierre**, inclusiva desde/hasta (el límite superior se aplica como siguiente día exclusivo). Por defecto, últimos 90 días hasta hoy; intervalos de máximo 366 días y a partir de 2000.
- Indicadores KPI para todo el período filtrado: total de cierres, plazas confirmadas, presentes, ausentes y excursiones con incidencias.
- Tabla paginada en SQL Server, **25 registros por página**, ordenados del cierre más reciente al más antiguo. Incluye tour, salida, guía, transporte, fecha UTC de cierre, responsable y observaciones.
- Cada fila enlaza a **Ver cierre**, la pantalla de solo lectura de Fase 5.3.
- Datos sensibles limitados al personal autorizado; sin listado de nombres ni teléfonos en el resumen. No se envían notificaciones y no se altera ninguna tabla.
- `Cache-Control: private, no-store` en respuesta API y web.

## Instalación
Detener API y MVC, resolver cambios locales antes del pull, luego:
```powershell
git status
git pull origin feature/toursmart-integrador-ii
dotnet build AgenciaToursAPI.sln
```
No ejecutar `dotnet ef database update` por esta fase.

Arrancar proyectos:
```powershell
dotnet run --project .\AgenciaToursAPI.csproj --launch-profile http
dotnet run --project .\TourSmart.Web\TourSmart.Web.csproj --launch-profile http
```

## Prueba guiada
1. Iniciar sesión como Administrador y abrir Dashboard → **Historial de excursiones**.
2. Usar un período que incluya el cierre de prueba de Fase 5.3. Debe aparecer una fila con datos idénticos a ese registro.
3. Confirmar totales, por ejemplo 5 confirmados, 3 presentes, 2 ausentes y 1 incidencia para un único cierre con observación.
4. Abrir **Ver observación** y verificar el texto original. Abrir **Ver cierre** y comprobar que coincide.
5. Aplicar un período sin cierres: debe mostrar contadores en cero y una tabla sin registros.
6. Filtrar por fechas que incluyen exclusivamente un día: debe incluir los cierres registrados durante todo ese día UTC.
7. Consultar con `desde` posterior a `hasta`: debe rechazarse sin efectuar consulta.
8. Usar fecha de hasta más de 366 días después de desde: debe rechazarse.
9. Cuando haya más de 25 cierres de prueba, verificar paginación y que los KPI permanezcan iguales al cambiar de página.
10. Probar con Operador, Cliente y sin sesión: no deben poder acceder al historial ni al endpoint de API; Operador sí puede ver el cierre individual desde su módulo autorizado.
11. Actualizar/repetir filtros: los datos y las tablas de reservas y pagos no deben modificarse.

## Notas
- No confundir **fecha de salida** con **fecha UTC de cierre operativo**. Los filtros son por la segunda.
- Los indicadores se basan en la instantánea inmutable almacenada durante el cierre, no en un recálculo del check-in actual.
- La vista se actualiza solo al recargar o cambiar los filtros. Los cierres no se cambian desde este módulo.
- No hay compilación .NET ni pruebas de integración disponibles desde la sesión de GitHub: deben ejecutarse en la computadora de desarrollo.
