# TourSmart RD — Rediseño visual 5.30 (rama de prueba)

## Estrategia
Rediseño con identidad **Tropical Premium** para el viajero y **Modern Operations** para el personal de la agencia. Se implementa **únicamente** en `feature/toursmart-visual-530`, creada desde la versión estable `feature/toursmart-integrador-ii`. No se fusiona con `master`, no se modifican operaciones de reserva, cupos, dinero o permisos y no se agregan migraciones SQL.

## Cambios reales
1. `Views/Shared/_Layout.cshtml`: navegación con menos enlaces principales y menú desplegable de operaciones. Se conservan rutas, roles, formulario POST de salir y token antifalsificación. Se agrega CSS/JS propio después del CSS vigente, facilitando reversión.
2. `Views/Home/Index.cshtml`: portada de mayor impacto, mensajes turísticos y un buscador real GET que reutiliza el filtro `busqueda` de `ToursController`. La fotografía procede del módulo Portada actual (si está configurada); no se presupone que existan fotos cargadas.
3. `Views/Tours/Index.cshtml`: ordenamiento de resultados ya devueltos por la API, sin recargar, por precio/fecha/cupos. Los filtros originales siguen enviándose por GET y conservan sus validaciones.
4. `Views/Tours/Detalle.cshtml`: nuevo tratamiento de tarjeta informativa para punto de encuentro, instrucciones y qué llevar.
5. `wwwroot/css/toursmart-premium-530.css`: nueva paleta, detalles de tipografía, tarjetas, foto principal, catálogo, formulario, dashboard y adaptaciones móviles; usa fotos existentes.
6. `wwwroot/js/toursmart-premium-530.js`: cabecera dinámica, animaciones de entrada progresivas y menú móvil. Respeta `prefers-reduced-motion` y no cambia los formularios de negocio.
7. `.github/workflows/validar-dotnet.yml`: agrega esta rama al proceso de CI existente (compilar Release, xUnit, publicar API/Web).

## Probar en Windows sin perder SQL ni fotos
**Detener la API y Web con Ctrl+C antes de compilar.** Ejecutar desde la raíz:
```powershell
git status
git fetch origin
git switch --track origin/feature/toursmart-visual-530
dotnet restore AgenciaToursAPI.sln
dotnet build AgenciaToursAPI.sln
dotnet test .\TourSmart.Tests\TourSmart.Tests.csproj
```
Si hay cambios locales en `Script-Sql/AgenciaToursDB.sql` y Git no permite cambiar de rama, **no descartarlos**. Guardar únicamente ese archivo (también se aconseja un respaldo adicional fuera del repositorio):
```powershell
git stash push -m "SQL antes de visualizar TourSmart 5.30" -- Script-Sql/AgenciaToursDB.sql
git fetch origin
git switch --track origin/feature/toursmart-visual-530
git stash apply 'stash@{0}'
```
Antes de aplicar, revisar `git stash list` y confirmar que `stash@{0}` sea el respaldo correcto; si hay conflictos, detenerse. Las fotos sin seguimiento en `TourSmart.Web/wwwroot/uploads/` **no deben eliminarse**. No ejecutar `git reset --hard` ni `git clean -fd`.

Terminal 1:
```powershell
dotnet run --project .\AgenciaToursAPI.csproj --launch-profile http
```
Terminal 2:
```powershell
dotnet run --project .\TourSmart.Web\TourSmart.Web.csproj --launch-profile http
```
Luego visitar `http://localhost:5291/`, `http://localhost:5291/tours`, un detalle `/tours/{id}`, y `/panel` con cuenta de prueba administrativa.

## Checklist funcional / visual
- [ ] Inicio con fotografía configurada y también sin fotografía (fallback decorativo).
- [ ] Buscador del hero filtra resultados reales; cadena vacía también abre catálogo.
- [ ] Catálogo mantiene filtros de país/destino/categoría y ordena sin recargar.
- [ ] Tarjetas son navegables con teclado, hover y móvil.
- [ ] Iniciar sesión/registrarse y cerrar sesión funcionan.
- [ ] Menú de operaciones mantiene rutas y permisos correctos para Cliente, Operador y Administrador.
- [ ] Punto de encuentro y recomendaciones aparecen en el detalle de tour.
- [ ] Crear reserva, revisar adelantos y cancelar reserva siguen funcionando en datos de **prueba**.
- [ ] Responsive a 375, 768 y 1280 px; menús visibles, sin desplazamientos laterales inesperados.
- [ ] Accesibilidad: navegación por teclado, foco visible y opción de movimiento reducido.
- [ ] `scripts/Test-TourSmartSmoke528.ps1` y pruebas xUnit completan sin errores en local.

## Regresar a la versión estable
Detener ambos servidores; resolver o resguardar cambios locales y ejecutar:
```powershell
git switch feature/toursmart-integrador-ii
```
La versión estable original permanece intacta. No fusionar el rediseño hasta completar pruebas visuales en navegador y las pruebas reales de SQL/autorización.

## Limitaciones
La compilación CI **no comprueba visualmente** la aplicación en navegador ni ejecuta SQL Server o SMTP. No se añadieron imágenes nuevas, ni se sobrescribieron las fotos cargadas por el usuario. Los enlaces administrativos deben probarse con los roles pertinentes. Un cambio en la apariencia no implica que estén verificadas las transacciones financieras ni el despliegue a producción.
