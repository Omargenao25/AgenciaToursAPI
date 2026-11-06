# TourSmart RD — Interfaz visual MVC (primera versión)

## Componentes
- `TourSmart.Web`: aplicación independiente ASP.NET Core MVC (.NET 8).
- Home: portada y tours destacados que todavía tienen plazas.
- Tours: catálogo con búsqueda, categoría y página de detalle.
- El frontend obtiene tours del backend `AgenciaToursAPI` mediante `HttpClient` **del lado del servidor**, sin exponer credenciales ni necesitar CORS para estas consultas.
- Las reservas, el login y el panel administrativo visual **no están implementados aún**. No se muestran botones de reserva falsamente funcionales.

## Cómo probar
Detén instancias previas. Abre dos terminales en la carpeta raíz del repositorio.

**Terminal 1 (API):**
```powershell
dotnet run --project .\AgenciaToursAPI.csproj --launch-profile http
```
API esperada: `http://localhost:5290` y Swagger `http://localhost:5290/swagger`.

**Terminal 2 (web):**
```powershell
dotnet run --project .\TourSmart.Web\TourSmart.Web.csproj --launch-profile http
```
Página: `http://localhost:5291/`; catálogo: `http://localhost:5291/tours`.

Para compilar toda la solución:
```powershell
dotnet build AgenciaToursAPI.sln
```

## Pruebas de aceptación
1. Página principal carga sin excepción y presenta encabezado y tours destacados, si existen disponibles.
2. Catálogo muestra tours reales de la API y permite búsqueda por país, destino o nombre.
3. Detalle de tour muestra precio, fecha, plazas ocupadas/disponibles y estado real.
4. Un tour cancelado/lleno/finalizado no aparece como reservable.
5. Detener la API y recargar: el frontend debe mostrar mensaje de conexión en lugar de errores de aplicación.
6. Verificar visualización móvil y escritorio.

## Notas
- El `Api:BaseUrl` de `TourSmart.Web/appsettings.json` apunta a `http://localhost:5290/` (solo para pruebas locales); puede configurarse por entorno.
- La versión actual usa ilustraciones CSS para evitar dependencias de imágenes remotas. Fotografías reales y datos de cada destino se integrarán después.
- El `.sln` incluye ambos proyectos y el backend excluye archivos compilables de `TourSmart.Web` para evitar conflictos.
- **El asistente no ejecutó compilación ni pruebas en el equipo del usuario**; esta guía establece la validación pendiente.
