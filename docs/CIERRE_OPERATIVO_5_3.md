# TourSmart RD — Fase 5.3: cierre operativo de excursiones

## Propósito
Consolidar el resultado de la lista de asistencia sin confundirlo con la finalización automática de fechas del tour, los estados de la reserva o los pagos.

## Nueva funcionalidad
- Página para personal: `/panel/tours/{id}/cierre-operativo`, accesible desde el manifiesto del tour.
- API: `GET /api/tours/{tourId}/cierre-operativo` para Administrador y Operador; `POST` para **Administrador** únicamente.
- El cierre registra en una **tabla separada**: `TourId`, `FechaCierreUtc`, `CerradoPor`, `PlazasConfirmadas`, `PasajerosPresentes`, `PasajerosAusentes`, `Incidencias` (máximo 1000 caracteres).
- La salida solo se puede cerrar si:
  1. No hay ninguna solicitud `Pendiente` en ese tour.
  2. Existe al menos una plaza confirmada.
  3. Todas las plazas de reservas confirmadas tienen nombres individuales (incluidos registros antiguos).
  4. Al menos un pasajero tiene check-in `Presente`.
  5. La salida aún no tiene cierre operativo.
  6. El Administrador envía una confirmación expresa; la web presenta además un diálogo de confirmación.
- **Ausentes** es la diferencia entre plazas confirmadas y pasajeros presentes cuando se ejecutó el cierre.
- El cierre es único por tour mediante índice SQL `UNIQUE(TourId)`. No se expone editar, eliminar ni revertir desde la web.
- La transacción `Serializable` y el bloqueo SQL del tour serializan el cierre frente a los cambios de check-in. La API de check-in adquiere ese mismo bloqueo y rechaza modificaciones cuando existe cierre.
- La API impide **reservas nuevas**, edición de nombres y modificación administrativa de las reservas vinculadas a un tour ya cerrado, así como cancelaciones de cliente en el tour cerrado. **No modifica** los pagos, reembolsos ni las reservas durante el cierre.
- El cierre de operaciones **no cambia** la propiedad `Tour.Estado`: sigue derivándose de la fecha programada. No significa que la excursión haya finalizado su itinerario completo; solo consolida el control de salida.
- El Operador puede consultar la página y el resultado, pero no autorizar el cierre. Los Clientes y usuarios anónimos no acceden.
- Ninguna operación se envía por correo electrónico y no se genera un PDF automáticamente.

## Migración
`20261109030000_CierreOperativoTour`: crea `CierresOperativosTour` con datos de cierre e índice único por tour. No borra ni modifica tablas ni reservas existentes. Se requiere respaldo de la base de datos.

Detener API y MVC, respaldar `AgenciaToursDB` y ejecutar desde la raíz:
```powershell
git status
git pull origin feature/toursmart-integrador-ii
dotnet build AgenciaToursAPI.sln
dotnet ef database update --project .\AgenciaToursAPI.csproj
```
**No ejecutar la migración si falla la compilación**. Resolver cambios locales pendientes antes del pull.

Iniciar en dos terminales separadas:
```powershell
dotnet run --project .\AgenciaToursAPI.csproj --launch-profile http
dotnet run --project .\TourSmart.Web\TourSmart.Web.csproj --launch-profile http
```

## Prueba guiada (solo datos ficticios)
1. Crear un tour de prueba futuro con capacidad suficiente y una reserva Confirmada de 5 personas nominales. Registrar el adelanto de prueba como en Fase 4.1.
2. Marcar llegadas de 3 de esos 5 pasajeros. Comprobar Presentes 3 y Faltantes 2 en el manifiesto.
3. Resolver cualquier solicitud Pendiente del mismo tour por los flujos normales. El cierre debe rechazar tours con pendientes.
4. Pulsar **Cierre operativo**. Verificar que muestre la lectura de los contadores correctos.
5. Como Administrador, registrar una incidencia ficticia (por ejemplo, «Dos viajeros no se presentaron a la hora de salida»), marcar la confirmación explícita y confirmar el diálogo.
6. Debe mostrar **cerrado**, con 5 confirmados, 3 presentes, 2 ausentes, fecha UTC, usuario responsable y texto de la incidencia.
7. Recargar el panel, comprobar que el registro persiste y no aparece el formulario de cierre.
8. Intentar volver a ejecutar el POST: la API debe devolver HTTP 409, no duplicar registros ni actualizar el cierre ya guardado.
9. Intentar marcar/revertir asistencia de este tour desde la API: debe devolver HTTP 409 y conservar los contadores finales.
10. Intentar crear una reserva nueva o corregir nombres vinculados al tour cerrado: debe rechazarse, sin alterar plazas.
11. Como Operador: consultar el reporte y comprobar que no puede ejecutar POST. Como Cliente: API y web deben denegar acceso.
12. Comprobar que las tablas de pagos, reembolsos y reservas no fueron modificadas por el cierre.

## Consideraciones
- **Prueba irreversible desde la interfaz:** emplear un tour ficticio separado de tus pruebas de check-in y no cerrar salidas reales.
- Si se produce un error de red tras enviar el cierre, consulta primero el estado antes de intentar otra vez.
- No es un parte de fin de recorrido ni un mecanismo de reembolso o cobro.
- La hora de salida usa la convención de hora local existente, y la marca de cierre es UTC.
- Una etapa posterior puede agregar cierre de regreso, auditoría de incidencias y un flujo formal de rectificación de cierres con autorización y bitácora; aquí todavía no existen.
- Compilación de C#, ejecución de migración y pruebas de extremo a extremo pendientes en la PC del usuario.
