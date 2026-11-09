# TourSmart RD — Fase 5.2: hoja de salida para el guía

## Funcionalidad
- Nueva vista web privada e imprimible: `GET /panel/tours/{id}/hoja-salida`, accesible desde el manifiesto, botón **Hoja de salida / Imprimir**.
- Solo usuarios autenticados con rol **Administrador** u **Operador**. La web reutiliza la sesión y el JWT para consultar `GET /api/tours/{id}/manifiesto`, también restringido a personal.
- La hoja incluye nombre y referencia del tour, fecha y hora de salida, guía, transporte, capacidad, plazas confirmadas, presentes y faltantes.
- **Una fila por plaza confirmada**, con el nombre individual (si está registrado), número de reserva, titular y asistencia en el momento de generar la hoja.
- Reservas **Pendientes** no forman parte de la lista nominal. Solo se muestra un conteo de solicitudes y plazas pendientes para contexto de operaciones.
- Las reservas anteriores a Fase 5.0 que no contienen todos los nombres se muestran explícitamente como **Nombre pendiente de completar**. Se advierte al guía que falta identificación; nunca se inventan nombres.
- Se excluyen reservas Canceladas.
- Por privacidad la impresión **no incluye teléfonos, cédulas, pasaportes, referencias de pagos, saldos ni montos**.
- Botón **Imprimir / Guardar como PDF** invoca el diálogo de impresión del navegador. La vista usa una hoja A4 con estilos de impresión, oculta controles durante la impresión y deja espacio para firmas.
- La hoja muestra la hora de generación. Es una **instantánea de solo lectura**: recargarla antes de imprimir si cambian reservas o check-ins.
- Encabezados privados `Cache-Control: private, no-store`, `Referrer-Policy: no-referrer` y `X-Robots-Tag`. Acceso restringido igualmente en API.
- **Sin migraciones**, sin librerías nuevas, sin crear documentos persistentes. No cambia datos ni registra asistencias automáticamente.

## Instalación
Detener API y MVC. Desde la raíz, comprobar cambios locales y ejecutar:
```powershell
git status
git pull origin feature/toursmart-integrador-ii
dotnet build AgenciaToursAPI.sln
```
No ejecutar `dotnet ef database update` para esta fase.

En dos terminales diferentes:
```powershell
dotnet run --project .\AgenciaToursAPI.csproj --launch-profile http
dotnet run --project .\TourSmart.Web\TourSmart.Web.csproj --launch-profile http
```

## Pruebas
1. Iniciar sesión como Administrador u Operador. Abrir `http://localhost:5291/panel/tours` y seleccionar **Pasajeros** en un tour de prueba.
2. Pulsar **Hoja de salida / Imprimir**. Debe abrir la hoja del tour seleccionado; comprobar nombre, fecha, guía y transporte.
3. Usar una reserva Confirmada de 5 personas con 5 nombres, 3 marcadas como presentes. La hoja debe mostrar 5 filas, **Presentes 3** y **Faltantes 2** si no hay más reservas confirmadas en ese tour.
4. Añadir una solicitud Pendiente: sus nombres **no** deben entrar en la lista imprimible.
5. Cancelar otra reserva de prueba cumpliendo los controles financieros: sus pasajeros ya no deben figurar.
6. Comprobar que una reserva histórica de 2 plazas sin nombres genera dos filas **Nombre pendiente de completar**, y que existe una advertencia visible.
7. Pulsar **Imprimir / Guardar como PDF**. Elegir imprimir o seleccionar «Guardar como PDF» desde el navegador. Comprobar que no aparecen los botones ni el encabezado y pie de navegación generales.
8. Comprobar que la impresión no incluye números de documento, teléfono, cobros, saldos ni devoluciones.
9. Probar con Cliente o anónimo: ruta web denegada; no deben obtener la hoja ni la lista de viajeros.
10. Comprobar que consultar o imprimir no altera pagos, asistencia, estado de reservas o plazas.
11. Marcar una llegada nueva desde el manifiesto digital y recargar la hoja: debe reflejar el estado actualizado.

## Limitaciones
- Una impresión o PDF guardado por el usuario queda **fuera del control de acceso del sistema**; distribuir solo a personal autorizado, retener lo mínimo necesario.
- Esta fase no asigna asientos ni registra salidas efectivas, ausencias finales, incidencias o aprobación del guía.
- Para una comprobación firme de seguridad, probar también el endpoint de manifiesto con roles Cliente y Operador.
- La compilación y prueba real siguen pendientes de verificar en la computadora del usuario.
