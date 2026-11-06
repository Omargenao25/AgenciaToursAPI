# Fase 2.3 — Dashboard administrativo inicial

## Alcance
- `/panel` muestra una página personal para Cliente; Administrador/Operador ven datos reales de la API.
- KPIs: tours futuros, reservas pendientes, confirmadas, canceladas y plazas disponibles de tours futuros.
- Tabla de últimas ocho reservas; muestra cliente, tour, personas y estado.
- Capacidad y ocupación de seis tours; barras de progreso.
- API de reservas se consulta desde `TourSmart.Web` mediante `HttpClient` con el JWT guardado exclusivamente en caché servidor, no se envía al navegador.
- Si la API no responde, el dashboard muestra un aviso en lugar de métricas inventadas.
- **Aún no hay botones de confirmación/cancelación**, esos formularios y sus protecciones se implementarán después.

## Pasos de prueba
1. Detener los dos proyectos. Verificar `git status`; si está limpio, ejecutar `git pull origin feature/toursmart-integrador-ii`.
2. Ejecutar `dotnet build AgenciaToursAPI.sln`; revisar errores de compilación antes de continuar.
3. En dos terminales:
   ```powershell
   dotnet run --project .\AgenciaToursAPI.csproj --launch-profile http
   dotnet run --project .\TourSmart.Web\TourSmart.Web.csproj --launch-profile http
   ```
   Ejecutar cada comando en una terminal independiente.
4. En `http://localhost:5291/cuenta/acceder`, iniciar sesión con un Administrador.
5. Abrir `http://localhost:5291/panel`: métricas basadas en datos de SQL Server vía API. Comprobar especialmente cantidades de estados y cupos.
6. Pulsar Salir. Abrir manualmente `/panel`: debe redirigir al login.
7. Iniciar sesión como Cliente: no debe mostrar el dashboard operativo.
8. Detener la API y recargar como administrador: debe aparecer error de servicio, no datos falsos.
9. Comprobar la página en móvil y escritorio.

## Seguridad y limitaciones
- Los permisos de la API siguen imponiéndose en `GET /api/Reservas`. El rol del navegador no es suficiente para acceder a los datos.
- Esta versión usa memoria del proceso web para guardar JWT: solo para desarrollo local. Despliegue requiere HTTPS, gestión de sesiones y claves, cache distribuido y controles adicionales.
- Para evitar confusiones, las plazas libres se agregan únicamente para tours cuyo `Estado` es `Disponible`.
- Se utilizan datos reales, no cifras de muestra. Las reservas canceladas siguen apareciendo en el historial, pero no consumen plazas.
- La compilación y pruebas funcionales deben ejecutarse en el equipo local del desarrollador.
