# TourSmart RD — Transportes y fotos subidas desde Windows

## Cambios
- Nueva pantalla `/panel/transportes` (Administrador/Operador): listar, buscar, crear, editar y eliminar transportes no utilizados.
- API `api/Transportes` ya existía. Los tours que utilizan un transporte impiden su eliminación.
- Accesos desde el panel, la barra de navegación y el formulario de tours.
- `/panel/tours/crear` y `/panel/geografia` ofrecen **seleccionar foto desde Windows** además de URLs HTTPS.
- Las fotos JPG, PNG y WebP (hasta 5 MB) son verificadas por firma binaria y guardadas con nombre aleatorio de 32 caracteres hexadecimales en `TourSmart.Web/wwwroot/uploads/tours`.
- La subida requiere autenticación Administrador/Operador y antifalsificación (CSRF). El archivo se guarda en el proyecto web y se publica en `/uploads/tours/<id>.<ext>`.
- El campo `ImagenUrl` conserva URLs HTTPS públicas y admite únicamente rutas locales estrictamente restringidas a `/uploads/tours/<nombrealeatorio>.{jpg,png,webp}`.
- El formulario muestra un mensaje tras la subida; es necesario pulsar **Guardar** para vincularla al tour o destino.

## Instalación local
Detener Web y API con Ctrl+C, luego:
```powershell
git checkout feature/toursmart-integrador-ii
git pull origin feature/toursmart-integrador-ii
dotnet build AgenciaToursAPI.sln
```

**No existe migración nueva para transportes ni subida de imágenes.** Si todavía no aplicaste la migración de fotografías creada antes:
```powershell
dotnet ef migrations list --project .\AgenciaToursAPI.csproj
dotnet ef database update --project .\AgenciaToursAPI.csproj
```

Arrancar ambas apps:
```powershell
# Terminal 1, API
dotnet run --project .\AgenciaToursAPI.csproj --launch-profile http
# Terminal 2, Web
dotnet run --project .\TourSmart.Web\TourSmart.Web.csproj --launch-profile http
```

## Pruebas sugeridas
1. Crear transporte tipo `Autobús`, descripción `45 pasajeros, aire acondicionado`; confirmar que aparece en el desplegable de nuevo tour.
2. Editar transporte, sin borrar su relación con tours previos.
3. Crear un tour de prueba asociado y comprobar que el botón Eliminar del transporte se desactiva.
4. Subir JPG y guardar tour; abrir `/tours` y su detalle. Ver imagen real.
5. Subir WebP como imagen del destino; un tour sin foto individual debe mostrarla.
6. Rechazar PDF o archivo >5 MB. Rechazar ruta inventada / URL HTTP.
7. Verificar que un cliente sin rol Administrador/Operador no pueda subir archivos ni gestionar transportes.
8. Validar que ninguna subida genere registros en SQL hasta pulsar Guardar, aunque sí genere archivo local.

## Importante para producción
Este almacenamiento es **local al servidor Web**, no al equipo del viajero ni a SQL Server. Si publicas en un servicio que borra archivos en cada despliegue, las fotografías subirán pero se perderán al reiniciar/desplegar. Para producción usa volumen persistente o almacenamiento de objetos (Azure Blob, S3, Cloudinary), política de tamaño y límites adicionales. Es importante respaldar `wwwroot/uploads` junto con la base de datos. No se elimina automáticamente una foto al cambiarla por otra, para evitar borrar imágenes compartidas; eso requiere una limpieza administrada posterior.

No se ha probado todavía el navegador de Windows ni SQL Server local a través de GitHub Actions.
