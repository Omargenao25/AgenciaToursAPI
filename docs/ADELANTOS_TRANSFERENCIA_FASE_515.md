# TourSmart RD — Adelantos por transferencia y comprobantes (Fase 5.15)

## ¿Qué se implementó?
- El cliente abre **Mis reservas → Adelanto / pago** y ve total, adelanto mínimo, saldo y vencimiento.
- Puede reportar una transferencia mediante **referencia, monto y comprobante PDF/JPG/PNG/WebP (máximo 2 MB)**. La API valida la firma binaria, no confía en extensión/MIME.
- Los comprobantes se guardan **privados en SQL Server** y NO son parte de `wwwroot`; se descargan como archivo adjunto exclusivamente por personal autorizado o su dueño desde la API. La web ofrece descarga administrativa.
- El reporte queda `Pendiente`. **NO incrementa PagosReserva, NO confirma reservas y NO equivale a dinero recibido**.
- El personal entra a `/adelantos/revision`, descarga comprobante, comprueba en el banco la transferencia real y decide **aprobar o rechazar con motivo**.
- Aprobar, mediante transacción serializable con el mismo bloqueo sobre Reserva que usan pagos, registra un único `PagoReserva` verificado, relaciona el comprobante con el pago y permite continuar el flujo normal de confirmación.
- Rechazar guarda motivo sin alterar importes. El cliente ve el estado de sus propios comprobantes.
- API valida propiedad por JWT y cliente vinculado; ningún cliente puede subir un comprobante para otro.
- Un comprobante pendiente por reserva como máximo; un nuevo reporte sólo después de una resolución. `PagoReservaId` es único, por lo que un comprobante no puede generar pagos duplicados.
- Para confirmar reserva se mantienen las reglas de porcentaje mínimo, saldo, fecha, cancelación y permisos existentes.
- No se procesan tarjetas ni se realizan transferencias desde TourSmart. **Solo se reporta una operación bancaria externa para revisión**.

## Requisito de la agencia: instrucciones bancarias reales
La web no mostrará cuentas inventadas. Configura el texto con los datos auténticos de la agencia, revisados y aprobados por su titular, mediante User Secrets del proyecto WEB (no API).

En PowerShell desde la raíz del repositorio:
```powershell
dotnet user-secrets set "Pagos:InstruccionesTransferencia" "Banco: [NOMBRE REAL] | Titular: [TITULAR REAL] | Cuenta: [CUENTA REAL] | Concepto: Número de reserva" --project .\TourSmart.Web\TourSmart.Web.csproj
```
Reemplaza las instrucciones de ejemplo **antes** de habilitar pagos reales. Si no configuras esto, el cliente verá un aviso de que no debe transferir a cuentas desconocidas. **No subas cuentas reales, ni credenciales, a GitHub por error**. User Secrets solo se usa en desarrollo y no cifra el archivo: para producción se requieren configuración y controles adecuados.

## Instalación
1. Respaldar SQL Server. No usar una base de producción para las pruebas.
2. Detener API y Web con Ctrl+C.
3. Desde la raíz:
```powershell
git checkout feature/toursmart-integrador-ii
git pull origin feature/toursmart-integrador-ii
dotnet build AgenciaToursAPI.sln
dotnet ef migrations list --project .\AgenciaToursAPI.csproj
dotnet ef database update --project .\AgenciaToursAPI.csproj
```
La nueva migración es `20261113213000_ComprobantesTransferencia`. Crea la tabla `ComprobantesTransferencia`; conserva las reservas y los pagos previos.

Terminal API:
```powershell
dotnet run --project .\AgenciaToursAPI.csproj --launch-profile http
```
Terminal Web:
```powershell
dotnet run --project .\TourSmart.Web\TourSmart.Web.csproj --launch-profile http
```

## Pruebas sin dinero real
1. Asegura que hay un método de pago existente cuyo nombre contenga `Transferencia`. Si falta, créalo desde administración.
2. Crea un tour futuro y una reserva de prueba desde un cliente registrado y verificado.
3. En `/viajero/reservas`, pulsa **Adelanto / pago**. Revisa los importes y su fecha límite.
4. Usa **solamente una base de datos de pruebas** y un comprobante de demostración rotulado como tal. No reportes pagos bancarios ficticios como reales en un ambiente operativo.
5. Verifica que un comprobante reportado esté Pendiente, sin aumentar `PagosReserva` ni confirmar la reserva.
6. Inicia como operador; entra a `/adelantos/revision`; descarga el comprobante. Rechaza la demostración y confirma que no se creó un pago.
7. En un entorno de pruebas aislado, valida aprobación únicamente con un pago simulado bajo condiciones controladas y confirma que se crea una sola fila `PagosReserva` con referencia y usuario revisor.
8. En `/panel/pagos/{reservaId}`, confirma la reserva si el adelanto verificado alcanza el mínimo; revisa correo de confirmación.
9. Prueba concurrencia/repetición: aprobar dos veces devuelve la misma referencia al pago, sin duplicarlo.
10. Prueba seguridad: un cliente distinto no debe consultar ni reportar comprobantes para reservas ajenas.
11. Rechaza un PDF inválido, archivo >2 MB, importe negativo, reserva vencida y comprobante simultáneo.
12. Comprueba que los archivos no se puedan abrir visitando `/uploads` ni una URL pública.

## Política de vencimiento y siguientes mejoras
- **Nuevo margen finito de revisión:** si el cliente presentó un comprobante válido antes del vencimiento y todavía está Pendiente, el proceso automático protege la reserva hasta **2 horas después del plazo original**, siempre que no haya pagos verificados. El plazo no se reinicia por volver a enviar comprobantes, ni se amplía sin límite.
- Una vez transcurridas esas 2 horas, si no hay pago registrado, el proceso periódico cancela la reserva y marca los comprobantes aún pendientes como Rechazados por vencimiento. Si administración rechaza antes de tiempo, la reserva deja de estar protegida y se cancela después del vencimiento normal.
- La aprobación administrativa comprueba también el plazo de gracia. El comprobante **nunca** se registra como pago sin validación humana; el margen tampoco concede confirmación automática.
- Si un comprobante se aprueba, **no se confirma automáticamente** la reserva; el personal revisa el pago y utiliza la acción de confirmación actual.
- Los comprobantes se almacenan en la base; limitar retención y establecer política de privacidad/respaldos antes de producción.
- No hay pasarela de pagos ni validación bancaria automatizada. La aprobación requiere comprobación humana en el banco.
- No hay correo automático de aprobación/rechazo del comprobante: el cliente ve estado al abrir su reserva. Puede incorporarse en la siguiente fase.
- Subida de archivos en servidor web utiliza JSON Base64 hacia la API, válida para un proyecto académico con límite de 2 MB; para producción puede reemplazarse por streaming y escaneo antivirus.
