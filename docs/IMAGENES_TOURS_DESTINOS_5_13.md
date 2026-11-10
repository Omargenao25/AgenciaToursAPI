# TourSmart RD — Imágenes de tours y destinos

## Funcionalidad
- Campo opcional `ImagenUrl` en Tour y Destino, con almacenamiento SQL `nvarchar(500) NULL`.
- Al crear/editar un tour: foto principal por URL HTTPS y vista previa.
- En `/panel/geografia`: foto opcional del destino, usada como respaldo cuando un tour no tiene foto.
- En `/tours` y detalle: imagen del tour -> imagen del destino -> ilustración del tema.
- Una imagen rota no impide ver ni reservar el tour.
- No se modifican reservas, precios, inventario ni permisos; no se almacenan binarios en SQL.
- Solo se aceptan URL HTTPS. El navegador obtiene las imágenes desde su proveedor; utilizar imágenes propias o con licencia/autorización y URLs HTTPS estables. No pegar enlaces privados o que incluyan tokens.

## Instalación
1. **Respaldar la base de datos**, especialmente en ambientes compartidos o con datos reales.
2. Detener API y Web con Ctrl+C; ejecutar desde la raíz de la solución:
```powershell
git pull origin feature/toursmart-integrador-ii
dotnet build AgenciaToursAPI.sln
dotnet ef migrations list --project .\AgenciaToursAPI.csproj
dotnet ef database update --project .\AgenciaToursAPI.csproj
```
3. Asegurarse de que se aplica `20261109213000_ImagenesToursYDestinos` a la MISMA conexión SQL que usa la API.
4. Levantar API y Web:
```powershell
# Terminal 1
dotnet run --project .\AgenciaToursAPI.csproj --launch-profile http
# Terminal 2
dotnet run --project .\TourSmart.Web\TourSmart.Web.csproj --launch-profile http
```
5. Abrir `http://localhost:5291/panel/geografia` (editar un destino, agregar URL HTTPS). Luego ir a `/panel/tours`, crear/editar un tour y agregar otra URL. Abrir `/tours` y el detalle.

## Pruebas importantes
1. Tour con foto y destino con foto: debe ganar la foto del tour.
2. Tour sin foto y destino con foto: debe verse la del destino.
3. Ambos sin foto: debe permanecer la ilustración original.
4. Foto HTTPS rota: el catálogo mantiene fondo legible; texto y acciones siguen disponibles.
5. URL HTTP o inválida: formulario/servidor rechaza la URL.
6. Editar foto no cambia la fecha, las plazas, el precio ni los pagos de una reserva.
7. En móvil: comprobar que la portada cubre la tarjeta sin deformar las proporciones.

## Limitaciones
- En esta fase se insertan **enlaces externos**, no se suben archivos desde el ordenador.
- La foto depende de que el servidor remoto permita carga directa; ciertas webs bloquean hotlinking.
- Los proveedores de fotos pueden recibir información del navegador; se usa `referrerpolicy="no-referrer"` en las imágenes públicas.
- No se implementaron todavía galerías ni imágenes de país/banderas.
- El pipeline de compilación confirma construcción del código; las pruebas funcionales y la migración SQL deben ejecutarse en el entorno del usuario.
