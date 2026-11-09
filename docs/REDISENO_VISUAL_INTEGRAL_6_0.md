# TourSmart RD — Rediseño visual integral (Fase UI 6.0)

## Alcance
Se agregó `TourSmart.Web/wwwroot/css/redesign.css` como capa de estilos que se carga después de `site.css` en el diseño compartido de la web MVC. Afecta consistentemente:
- Encabezado con navegación diferenciada para viajeros y personal, pie de página, accesibilidad (enlace para saltar al contenido, foco visible y movimiento reducido).
- Inicio: hero, nueva llamada a descubrir destinos, tarjetas de excursiones y pasos.
- Catálogo: buscador, filtros, tarjetas y estados.
- Detalle de excursión: resumen, cartel, información y reserva.
- Panel de control y secciones administrativas: estadísticas, tablas, tarjetas de capacidad, filtros, accesos rápidos, botones y formularios.
- Gestión de países y destinos, clientes, tours, reservas, pasajeros, pagos, reembolsos y reportes a través de componentes reutilizados.
- Login, registro, recuperación de contraseña y pantallas de la cuenta del viajero.

No se modificaron contratos de la API, persistencia de SQL, permisos de roles ni flujo de pagos. El rediseño modifica presentación y algunos enlaces de navegación existentes.

## Verificación
GitHub Actions valida restauración y compilación de la solución con advertencias como errores. Eso NO reemplaza revisión visual y pruebas de interacción con navegador.

### Checklist visual recomendado
1. Abre inicio y catálogo como visitante; prueba filtros por país, destino, categoría y disponibilidad.
2. Comprueba el detalle de un tour y su botón de reserva con un usuario Cliente.
3. Entra como Administrador: revisa el panel, menú, Países/Destinos, tours, clientes y reservas.
4. Revisa el formulario de crear/editar tour y el de país/destino; confirma clics y selección de combos.
5. Revisa pagos, reembolsos, manifiesto, hoja de salida, cierre operativo y reportes (solo en entorno de prueba).
6. Repite en resolución estrecha (ancho 375px) y en tableta; comprueba scroll horizontal solo en tablas anchas, no en toda la página.
7. Prueba teclado (Tab), menú móvil y botones de cancelar/guardar.
8. Si el navegador conserva estilos anteriores, recarga con Ctrl + F5: `asp-append-version` invalida caché al cambiar el CSS.

### PowerShell
Detén ambos proyectos con Ctrl+C **antes de compilar** para evitar MSB3021/MSB3027 por bloqueo de DLLs/EXEs.

```powershell
git checkout feature/toursmart-integrador-ii
git pull origin feature/toursmart-integrador-ii
dotnet build AgenciaToursAPI.sln
```

Terminal 1:
```powershell
dotnet run --project .\AgenciaToursAPI.csproj --launch-profile http
```
Terminal 2:
```powershell
dotnet run --project .\TourSmart.Web\TourSmart.Web.csproj --launch-profile http
```
Web: http://localhost:5291

## Reversión
Los cambios están en `feature/toursmart-integrador-ii`; no se alteró `master`. Para desactivar únicamente la capa visual se puede quitar en `_Layout.cshtml` la referencia a `redesign.css`; eso no revierte los cambios puntuales de contenido.
