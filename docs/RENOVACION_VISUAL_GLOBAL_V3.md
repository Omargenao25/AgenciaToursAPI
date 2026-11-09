# TourSmart RD — Renovación visual global v3

## Alcance
Nueva capa de diseño `TourSmart.Web/wwwroot/css/toursmart-v3.css`, cargada después de `site.css` y `redesign.css` para que se aplique a toda la web MVC sin modificar los controladores ni el comportamiento de la API.

Se renovaron:
- Navegación compartida para visitantes, clientes, Operador y Administrador; branding y pie de página.
- Página de inicio: mensajes, jerarquía, hero, bloques de beneficios y acceso al catálogo.
- Tarjetas de tours: ubicación, categoría, plazas y precio más claros.
- Catálogo de excursiones: encabezados y organización de búsqueda.
- Panel de operaciones: acciones prioritarias, accesos y métricas.
- Gestión de tours: encabezado y controles tabulares.
- Pantalla de inicio de sesión: presentación, sin alterar el formulario de autenticación.
- Resto de vistas que reutilizan clases compartidas: botones, tarjetas, tablas, formularios, mensajes, secciones, reportes y paneles.
- Navegación móvil, contraste, foco visible y reducción de animaciones si así lo solicita el sistema.

## Validación
1. Cerrar ambos procesos dotnet antes de actualizar y compilar para evitar archivos bloqueados MSB3027.
2. Ejecutar `git pull origin feature/toursmart-integrador-ii` y `dotnet build AgenciaToursAPI.sln`.
3. Iniciar API y Web.
4. Comprobar rutas y roles:
   - `/` portada: enlaces, tarjetas con cero y varios tours.
   - `/tours`: filtros, destino dependiente del país, estado sin resultados.
   - `/cuenta/acceder`: login válido e inválido, formulario y navegación.
   - `/panel`: usuario Cliente y Administrador; accesos con permisos correctos.
   - `/panel/geografia`: guardar, editar, cancelar, filtros, botones en uso.
   - `/panel/tours` y creación: selector dependiente de país/destino, validaciones, guardar.
   - Reservas, pagos, pasajeros, reportes: controles alcanzables, tablas con scroll horizontal en móvil.
5. Revisar al menos en anchos 375px, 768px y 1440px; zoom al 200%, teclado y navegación desplegable. Ningún botón o campo debe quedar bajo otro elemento.
6. Confirmar que los formularios conservan antifalsificación y que todas las llamadas REST siguen funcionando. La compilación CI comprueba sintaxis y .NET, **no** la presentación real de un navegador.

## Ejecutar
API, Terminal 1:
```powershell
dotnet run --project .\AgenciaToursAPI.csproj --launch-profile http
```

Web, Terminal 2:
```powershell
dotnet run --project .\TourSmart.Web\TourSmart.Web.csproj --launch-profile http
```

URL Web: http://localhost:5291

No se introducen migraciones de base de datos ni cambios de autorización en esta renovación.
