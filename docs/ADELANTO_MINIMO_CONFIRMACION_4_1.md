# TourSmart RD — Fase 4.1: adelanto mínimo configurable antes de confirmar

## Alcance y reglas
- Cada tour tiene `PorcentajeAdelanto` de 0 a 100, inicial **30** para tours nuevos y existentes mediante migración.
- El porcentaje se configura desde **Gestionar tours → Adelanto** (`/panel/tours/{id}/adelanto`) por Administrador u Operador.
- Por integridad contractual, **no se puede cambiar la política después de que un tour tenga cualquier reserva**, incluso cancelada, ni después de iniciado.
- API personal: `GET /api/Tours/{id}/adelanto` y `PUT /api/Tours/{id}/adelanto`.
- API pública: `GET /api/Tours/{id}/condiciones`, únicamente devuelve el porcentaje; permite informar al turista antes de pedir la reserva.
- En el detalle del tour y en el formulario de autoservicio se informa el adelanto requerido. El importe mostrado en el formulario se actualiza al cambiar cantidad de personas; **no es un cobro**.
- Una solicitud se crea en `Pendiente` y ocupa cupos igual que antes. Registrar pagos no la confirma automáticamente.
- Confirmación desde `PATCH /api/Reservas/{id}/confirmar`: bajo transacción serializable, toma bloqueo de actualización sobre la reserva, verifica fecha y estado, obtiene `Tour.PorcentajeAdelanto` y suma pagos registrados.
- La cantidad mínima en centavos es `Math.Ceiling(Reserva.Total * PorcentajeAdelanto) / 100m`; **se redondea hacia arriba al centavo**. Ej.: RD$ 8,000, al 30% = RD$ 2,400.
- Solo se confirma cuando `Pagado >= AdelantoMinimo`. Si falta adelanto, la API responde HTTP **409 Conflict** con el monto mínimo y faltante. La validación no depende del navegador.
- En **Pagos / saldo** aparecen el porcentaje, el mínimo, faltante y el botón **Confirmar reserva** solo cuando se cumple el pago (y el estado es Pendiente). Los listados de reservas muestran **Revisar adelanto**, no una confirmación prematura.
- Las reservas previamente Confirmadas se mantienen; el sistema no las revierte. Los nuevos intentos de confirmar reservas Pendientes sí están sujetos a la política.
- No se registran pagos automáticamente ni se procesan cargos reales. El personal debe verificar el dinero recibido y registrar cada movimiento.
- Continúan las restricciones de Fase 4.0 sobre cancelaciones con pagos: requieren una futura conciliación/reembolso. No se incluye vencimiento automático de reservas pendientes.

## Instalación
1. Detener API y MVC y realizar **respaldo** de `AgenciaToursDB`.
2. En raíz del repositorio: `git status` y, si árbol limpio, `git pull origin feature/toursmart-integrador-ii`.
3. `dotnet build AgenciaToursAPI.sln` (objetivo **0 errores y 0 advertencias**).
4. Solo con respaldo y compilación correcta: `dotnet ef database update --project .\AgenciaToursAPI.csproj`.
5. Arrancar API y MVC en terminales distintas:
   ```powershell
   dotnet run --project .\AgenciaToursAPI.csproj --launch-profile http
   dotnet run --project .\TourSmart.Web\TourSmart.Web.csproj --launch-profile http
   ```
6. Nueva migración: `20261107210000_AdelantoPorTour`. Columna de tours `PorcentajeAdelanto int NOT NULL DEFAULT 30` con `CHECK 0..100`. No elimina datos.

## Pruebas con información ficticia
1. Crear un **tour de prueba nuevo** sin reservas, con precio RD$ 8,000 y suficiente capacidad. En Gestión de tours → Adelanto, confirmar 30%. Cambiar a 40% y de regreso a 30%; ambos cambios deben guardarse.
2. Abrir el detalle y formulario público de reserva: deben informar 30% y, para una persona, **RD$ 2,400**. Cambiar a dos personas: el formulario debe indicar **RD$ 4,800**.
3. Registrar reserva de prueba por RD$ 8,000: queda `Pendiente`. Abrir **Pagos / saldo**; muestra adelanto mínimo RD$ 2,400 y faltante RD$ 2,400. No debe permitir confirmar.
4. Intentar confirmar directamente por `PATCH /api/Reservas/{id}/confirmar` como personal antes de pagar: esperar **409**, estado Pendiente.
5. Registrar RD$ 2,399: debe faltar RD$ 1 y seguir pendiente.
6. Registrar RD$ 1 más: el mínimo queda cubierto y se ofrece **Confirmar reserva**.
7. Confirmar desde **Pagos / saldo**. Estado cambia a Confirmada con saldo restante **RD$ 5,600**. El total no debe convertirse artificialmente en pagado.
8. Una vez creada la primera reserva, intentar cambiar porcentaje del tour: esperar **409** y porcentaje anterior inalterado.
9. Si el viaje ya comenzó, la API no debe confirmar aunque haya adelanto suficiente.
10. Verificar rol Cliente: no puede modificar porcentajes ni confirmar reservas mediante API.
11. Verificar continuidad: reservas ya confirmadas antes de esta actualización siguen Confirmadas, sin necesidad de nuevos pagos.
12. Probar precisión: precio/reserva de RD$ 100.01 con 30%, el mínimo requerido es **RD$ 30.01**.
13. Comprobar que la solicitud del turista no mueve dinero, y que el personal registra únicamente abonos realmente verificados (simulados en pruebas).

## Pendientes
- Plazo de vencimiento de reservas pendientes, liberación automática de plazas, comunicación con cliente, reembolsos/conciliación, notificaciones y pasarela de pago.
- El sistema aún necesita pruebas de concurrencia de confirmación/pagos en SQL Server.
- Compilación y migración **pendientes en la PC del usuario**.
