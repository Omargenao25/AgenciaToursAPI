# TourSmart RD — Fase 3.0: cancelación de reservas por el viajero

## Implementado
- `PATCH /api/MiCuenta/reservas/{id}/cancelar`, autorizado solo para rol `Cliente`.
- La API obtiene el `ClienteId` por el ID del usuario en JWT, consultando la relación `Usuario.ClienteId` en SQL Server.
- En la misma transacción serializable, se busca la reserva con `Id` y `ClienteId` usando bloqueo de actualización. Las reservas de otro cliente son indistinguibles de las inexistentes: devuelve 404.
- Solo se admite `Pendiente` o `Confirmada`, antes del inicio del tour; no permite volver a cancelar, ni cancelar tras el inicio.
- Actualiza `Estado = Cancelada` y `FechaCambioEstado = DateTime.UtcNow`. La reserva permanece en el historial.
- No se altera directamente un contador de plazas: la disponibilidad ya se obtiene excluyendo las reservas canceladas, por lo que se libera la cantidad de personas reservadas.
- MVC: `POST /viajero/reservas/{id}/cancelar`, restringido a `Cliente` y con antiforgery. El JWT permanece del lado del servidor.
- El botón aparece solo si `ReservaPanel.PuedeCancelar`; confirmación explícita del navegador. La API vuelve a comprobar las reglas (no confía en el botón).
- Sin cambios de esquema ni migraciones nuevas.

## Para probarlo
1. Detener ambas aplicaciones, comprobar `git status` y, si no hay modificaciones locales sin resolver, `git pull origin feature/toursmart-integrador-ii`.
2. Ejecutar `dotnet build AgenciaToursAPI.sln`; comprobar 0 errores.
3. Iniciar API: `dotnet run --project .\AgenciaToursAPI.csproj --launch-profile http`.
4. Iniciar MVC: `dotnet run --project .\TourSmart.Web\TourSmart.Web.csproj --launch-profile http`.
5. Entrar con Manuel, ir a `http://localhost:5291/viajero/reservas`. Si su reserva #8 sigue pendiente y su tour aún no inició, debe verse el botón **Cancelar reserva**.
6. Medir capacidad inicial de MexicoLindoTour's y estado #8 en panel administrativo; al cancelar, revisar pantalla de confirmación. Probar primero **Cancelar** en el diálogo: la reserva debe seguir intacta.
7. Confirmar en el diálogo: reserva #8 pasa a `Cancelada`; el historial no desaparece; la capacidad disponible aumenta en **1**.
8. Verificar que el botón desaparece para la reserva cancelada, y que no se puede revivir desde la cuenta del viajero.
9. Entrar con Rohanny: no debe ver la reserva #8. Intentar solicitar `PATCH /api/MiCuenta/reservas/8/cancelar` con su propio JWT (solo en entorno controlado); la API debe devolver **404** sin modificar la reserva.
10. Probar con una reserva de tour que ya inició (puede requerir otro tour de prueba): no debe aparecer botón y la API debe responder **409**.
11. Verificar cancelación de reserva confirmada todavía futura: se conserva registro y libera cupos.

## Seguridad, límites
- El frontend usa fecha del tour para mostrar el botón, pero **la autorización y la comprobación temporal definitivas están en la API**.
- Usar cuentas ficticias. No compartir contraseñas, JWT ni documentos en capturas.
- La cancelación no inicia devoluciones de dinero: todavía no hay pasarela de pagos ni política de reembolsos.
- No se ha ejecutado la compilación ni prueba funcional en el equipo del usuario desde el asistente. Se requiere verificación local.
