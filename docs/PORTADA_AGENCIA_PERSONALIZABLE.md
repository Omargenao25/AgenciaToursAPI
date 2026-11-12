# TourSmart RD — Imagen general del inicio

## Qué se implementó
- Panel protegido Administrador/Operador: `http://localhost:5291/panel/portada`.
- Subida desde Windows de JPG, PNG o WebP hasta 5 MB, mediante formulario antifalsificación (CSRF).
- Validación de firma binaria real del archivo, nombres aleatorios y almacenamiento bajo `TourSmart.Web/wwwroot/uploads/portada/`.
- Un archivo `actual.txt` registra qué fotografía está activa. Se actualiza solo cuando el archivo ya se guardó.
- La página de inicio lee ese marcador para mostrar la imagen dentro del panel visual de la derecha, conservando las etiquetas flotantes, el titular y las acciones. Si falta una imagen, se muestra la ilustración original.
- La pantalla de administración ofrece vista previa antes de guardar. Se agrega versión a la URL para evitar caché del navegador.
- No requiere una migración nueva ni modifica los datos de tours y destinos.

## Cómo probar
```powershell
git checkout feature/toursmart-integrador-ii
git pull origin feature/toursmart-integrador-ii
dotnet build AgenciaToursAPI.sln
# Terminal 1:
dotnet run --project .\AgenciaToursAPI.csproj --launch-profile http
# Terminal 2:
dotnet run --project .\TourSmart.Web\TourSmart.Web.csproj --launch-profile http
```
1. Iniciar sesión como Administrador u Operador y abrir `/panel/portada`.
2. Elegir una ilustración general de la agencia (viajeros, aventura, naturaleza, sin destinos específicos) en JPG/PNG/WebP de menos de 5 MB.
3. Ver vista previa, pulsar **Guardar imagen de inicio**.
4. Abrir `http://localhost:5291/` y verificar el bloque derecho en escritorio y móvil, títulos, botones y etiquetas.
5. Actualizar a otra portada y confirmar que cambia.
6. Una cuenta Cliente no debe poder entrar al panel ni subir fotos.

## Nota de despliegue
Los archivos se guardan en el servidor de la web y no en SQL Server. Para producción se necesita volumen persistente o almacenamiento de objetos: los despliegues que sobrescriben `wwwroot` pueden borrar las imágenes. Incluye `wwwroot/uploads/portada` en tus respaldos. No se borra automáticamente una imagen anterior para permitir recuperación manual.
