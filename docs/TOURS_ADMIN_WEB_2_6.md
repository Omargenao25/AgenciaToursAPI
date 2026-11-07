# Fase 2.6 — Gestión administrativa de tours

## Implementado en `feature/toursmart-integrador-ii`
- `GET /panel/tours`: lista tours existentes con fecha, precio, ocupación y estado.
- `GET /panel/tours/crear`: formulario para nueva excursión con catálogos reales (países, destinos, categorías, guías y transportes).
- `POST /panel/tours/guardar`: envía `POST /api/Tours` con JWT del lado servidor y antiforgery.
- `GET /panel/tours/{id}/editar`: precarga tour y catálogos para modificarlo.
- `POST /panel/tours/guardar` con ID existente: envía `PUT /api/Tours/{id}`.
- El dashboard añade acceso **Gestionar tours**.
- API `TourRespuestaDto` incluye IDs de país/destino/categoría/guía/transporte para precargar selección. No requiere migración.
- Los formularios ocultan opciones de destino que no corresponden al país, pero la API sigue validando.
- Acceso solo a Administrador y Operador; la API valida de nuevo el JWT.
- Se respetan los límites: capacidad >= plazas ocupadas; con reservas activas no cambiar país, destino, guía, transporte, fecha ni hora.

## Prueba local
1. Detener ambas aplicaciones. Revisar `git status`; después ejecutar:
   ```powershell
   git pull origin feature/toursmart-integrador-ii
   dotnet build AgenciaToursAPI.sln
   ```
2. Encender la API y la web, cada una en su terminal.
3. Abrir `http://localhost:5291/panel/tours` con usuario Administrador; comprobar tours existentes y estados.
4. Abrir **Crear nuevo tour**, comprobar que se cargan países, destinos, categorías, guías, transportes y que los destinos responden al país.
5. Crear tour de prueba con fecha futura, IDs existentes, precio positivo, capacidad >0; verificar respuesta, mensaje y nuevo tour en listado.
6. Editar ese tour sin reservas: cambiar nombre, precio, capacidad y fecha. Comprobar actualización en SQL/API.
7. Crear reserva de prueba en ese tour. Intentar reducir capacidad debajo de plazas ocupadas y cambiar destino: ambos cambios deben rechazarse sin alterar el tour.
8. En un tour finalizado verificar que no se ofrezca Editar.
9. Salir y abrir `/panel/tours`; debe solicitar login. Cuenta Cliente no debe acceder al módulo.
10. Si API se detiene, el formulario/listado muestra aviso sin afirmar que guardó cambios.

## Alcance y pendientes
- No se incluyó eliminación porque borrar tours con reservas rompería el historial y actualmente la API lo restringe.
- Tampoco hay un estado manual Activo/Inactivo: el estado actual se calcula por las fechas del tour en la API. Implementarlo requiere diseño explícito de la regla de negocio.
- No se cambió la estructura SQL.
- Faltan pruebas locales de compilación y funcionalidad; el código fue subido y revisado en GitHub, no ejecutado por el asistente en Visual Studio.
- Futuras mejoras: gestión independiente de catálogos y guías, inactivación manual auditada, CRUD con subida de imágenes, búsqueda/filtros y pruebas automatizadas.
