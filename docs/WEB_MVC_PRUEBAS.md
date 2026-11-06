# TourSmart RD — Fase 2.1: primera interfaz MVC

## Lo que incluye
- Nuevo proyecto **TourSmart.Web** (ASP.NET Core MVC, .NET 8) añadido a `AgenciaToursAPI.sln`.
- Inicio visual de marca TourSmart RD, catálogo de excursiones, búsqueda por nombre/destino/país y filtro por categoría.
- Detalle con nombre, fecha, precio, guía, transporte, capacidad, plazas disponibles y estado.
- Comunicación **desde el servidor MVC** mediante HttpClient con los GET públicos de AgenciaToursAPI. No expone tokens de administrador.
- Mensaje amistoso si la API no está disponible.
- Estilos responsivos y elementos ilustrados mediante CSS (sin dependencia de imágenes externas).
- Reservas desde la web, autenticación, panel administrativo y recomendaciones: **todavía no implementados**; en esta etapa se muestran solo como próximos pasos y no se crean acciones ficticias.

## Ejecución local en dos terminales
Requiere Visual Studio 2022 con .NET SDK 8 y base de datos SQL Server configurada para AgenciaToursAPI.

1. Detener cualquier API que esté ejecutándose.
2. Desde la carpeta `AgenciaToursAPI`:
```powershell
git status
git pull origin feature/toursmart-integrador-ii
dotnet build AgenciaToursAPI.sln
```
3. Terminal 1:
```powershell
dotnet run --project AgenciaToursAPI.csproj --launch-profile http
```
   API y Swagger: `http://localhost:5290/swagger`.
4. Terminal 2 (sin cerrar Terminal 1):
```powershell
dotnet run --project TourSmart.Web/TourSmart.Web.csproj --launch-profile http
```
   Interfaz visual: `http://localhost:5291/`.

No ejecutar `dotnet run` sin `--project` desde raíz cuando hay dos proyectos.

## Pruebas recomendadas
- Abrir inicio: la página muestra tours disponibles tomados de la API.
- `/tours`: muestra tours, incluidos los no disponibles, con cupos correctos.
- Buscar por `Guadalajara` o `Punta Cana`: filtra los resultados.
- Abrir un tour: muestra fecha, destino, estado, precio y cupos de la API.
- Detener AgenciaToursAPI, recargar el catálogo: aparece un aviso de indisponibilidad, no un error 500.
- Iniciar API de nuevo y recargar: vuelve a aparecer el catálogo.
- Reducir navegador a tamaño móvil: navegación, tarjetas y búsqueda conservan legibilidad.

## Advertencias
- El bootstrap y JS se cargan desde CDN, por lo que se necesita internet para estilos complementarios; el CSS propio funciona localmente.
- La opción `Api:BaseUrl` vale `http://localhost:5290/` solo para desarrollo. Para despliegue usar HTTPS y configurar el backend con una variable `Api__BaseUrl`.
- No se han compilado ni ejecutado pruebas del frontend en este entorno, porque no hay SDK .NET local. Compilar en Visual Studio antes de considerar la fase validada.
- No hay necesidad de migración SQL nueva para esta fase.
