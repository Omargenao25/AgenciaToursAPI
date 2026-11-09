# TourSmart RD — Fase 5.5: indicadores de rendimiento de excursiones

## Nuevo reporte
- API privada: `GET /api/reportes/rendimiento-excursiones?desde=AAAA-MM-DD&hasta=AAAA-MM-DD&pagina=1`.
- Web privada: `/panel/reportes/rendimiento-excursiones`, accesible desde **Dashboard → Rendimiento de excursiones**.
- Solo rol **Administrador** (API y web); se utiliza JWT de sesión existente.
- Reutiliza los cierres definitivos `CierresOperativosTour` de la Fase 5.3. **No requiere migraciones**.
- Filtro por **fecha UTC de cierre**, no por fecha de salida. Por defecto, desde 90 días atrás hasta hoy; máximo 366 días.
- KPI globales de todas las excursiones cerradas en el período: total de excursiones, capacidad agregada, confirmados, presentes, ausentes, excursiones con incidencias, porcentaje de ocupación y porcentaje de asistencia.
- Tabla comparativa de hasta 25 cierres por página: excursión, fecha de salida, capacidad, confirmados, presentes, ausentes, incidencias y barras de ocupación/asistencia. Enlace a cada cierre en modo consulta.
- Los KPI se calculan agregando el período completo en SQL Server; **no dependen de la página**.
- Solo lecturas, sin creación ni modificación de reservas, pasajeros, pagos o cierres.

## Fórmulas
- **Ocupación de una excursión** = `100 × plazas confirmadas / capacidad máxima`.
- **Asistencia de una excursión** = `100 × pasajeros presentes / plazas confirmadas`.
- **Ocupación global ponderada** = `100 × suma de plazas confirmadas / suma de capacidades`.
- **Asistencia global ponderada** = `100 × suma de presentes / suma de confirmados`.
- Si el denominador es 0, se presenta 0%. La visualización redondea a 1 decimal.
- Se consideran exclusivamente **cierres operativos registrados**; los tours finalizados por calendario sin cierre **no** entran.
- La capacidad se toma del tour asociado. La Fase 5.3 bloquea la edición de un tour cerrado, por lo que queda estable para este cálculo.
- El indicador `Con incidencias` cuenta cierres con observaciones no vacías; no infiere su gravedad.

## Instalación
Detener API y MVC, resolver cambios locales y desde la raíz:
```powershell
git status
git pull origin feature/toursmart-integrador-ii
dotnet build AgenciaToursAPI.sln
```
No aplicar migraciones por esta fase. Luego:
```powershell
dotnet run --project .\AgenciaToursAPI.csproj --launch-profile http
dotnet run --project .\TourSmart.Web\TourSmart.Web.csproj --launch-profile http
```

## Pruebas
1. Iniciar sesión como Administrador. Abrir `http://localhost:5291/panel/reportes/rendimiento-excursiones` o pulsar el botón del dashboard.
2. Elegir fechas UTC que incluyan el tour ficticio cerrado en las fases anteriores. Debe aparecer en la tabla.
3. Para un cierre con **capacidad 10, confirmados 5 y presentes 3**, comprobar **50,0% de ocupación** y **60,0% de asistencia**.
4. Comprobar que los ausentes sean **2**, y que el enlace **Ver cierre** abra el registro de origen.
5. Probar varios tours ficticios de capacidades distintas. Los porcentajes globales deben ser ponderados, no promedios aritméticos simples.
6. Probar un período sin cierres: se muestran cero indicadores, 0% y tabla vacía.
7. Probar un rango de más de 366 días o fechas invertidas: se rechaza.
8. Si hay más de 25 cierres, verificar que la segunda página conserva los mismos KPI globales.
9. Probar rol Operador o Cliente: no debe acceder al reporte. Sin sesión se exige autenticación.
10. Recargar la página, comparar con el historial de Fase 5.4 y comprobar que consultar no modifica ningún registro.

## Pendientes
- Compilación real y pruebas de integración en el equipo de desarrollo.
- Más adelante se puede agregar comparativas por destino/categoría y tendencias mensuales, sin alterar los cierres.
- El desempeño financiero es un reporte independiente: la ocupación **no representa ganancias**.
