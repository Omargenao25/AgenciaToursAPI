# TourSmart RD — Fase 4.8: centro de alertas de cobro y reservas

## Funcionalidad
- Enlace «Centro de alertas» en el dashboard del Administrador, dirigido a `/panel/reportes/cuentas-por-cobrar#alertas`.
- Se amplía el endpoint protegido de la Fase 4.7: `GET /api/reportes/cuentas-por-cobrar`, que devuelve la colección `alertas` además de los datos existentes. Mantiene la autorización de rol **Administrador**.
- Sin base de datos adicional ni correos electrónicos: las alertas se calculan al consultar el panel con reservas activas de tours futuros.
- No guarda ni modifica estados, no envía SMS, WhatsApp ni emails, no ejecuta pagos y no duplica notificaciones. El enlace conduce a «Pagos / saldo» para revisión manual.

## Reglas de alertas
- **Prioridad alta:**
  - Reserva Pendiente con plazo de adelanto vencido y algún pago registrado: revisar y conciliar manualmente.
  - Reserva Pendiente con plazo vencido sin pagos: comprobar que el servicio automático de Fase 4.2 complete la cancelación.
  - Reserva Pendiente con adelanto insuficiente y un plazo que vencerá dentro de las siguientes **6 horas**.
  - Reserva Confirmada que comenzará dentro de las siguientes **48 horas** y aún tenga saldo.
- **Prioridad media:**
  - Reserva Pendiente cuyo adelanto neto todavía no alcanza el mínimo, con más de 6 horas de plazo (o sin fecha límite histórica).
  - Reserva Pendiente con adelanto cubierto: revisar la confirmación por personal autorizado.
- Se excluyen reservas Canceladas y tours ya iniciados; no se generan alertas para Confirmadas completamente pagadas.
- El importe indicado para Pendientes corresponde al adelanto que falta, y para Confirmadas próximas a salir al saldo que falta.
- Los montos toman en cuenta el efecto de reembolsos: neto = pagos brutos - devoluciones.
- Las alertas de prioridad alta se muestran antes de las medias; luego se ordenan por plazo y salida.
- Las fechas de vencimiento usan UTC; las fechas/hora de salida respetan la convención local existente en la API.

## Instalación
No requiere migración. Detén la API y la web y ejecuta desde la raíz:
```powershell
git status
git pull origin feature/toursmart-integrador-ii
dotnet build AgenciaToursAPI.sln
```
Soluciona cambios locales pendientes antes del pull. Cuando compile correctamente, inicia API y web en terminales separadas:
```powershell
dotnet run --project .\AgenciaToursAPI.csproj --launch-profile http
dotnet run --project .\TourSmart.Web\TourSmart.Web.csproj --launch-profile http
```

## Pruebas
1. Inicia sesión como Administrador y abre el dashboard. Debe verse el acceso **Centro de alertas**.
2. Abre `http://localhost:5291/panel/reportes/cuentas-por-cobrar#alertas`: comprueba los contadores y la tabla de avisos.
3. Crea una reserva ficticia Pendiente de un tour futuro sin adelanto: debe mostrar alerta media de adelanto insuficiente.
4. Registra un pago de prueba suficiente pero no confirmes: debe mostrar «ListaParaConfirmar». Confirma desde «Pagos / saldo»: la alerta desaparece si la reserva ya está completamente pagada; si mantiene saldo y la excursión sale en menos de 48 horas, debe mostrar alerta alta de saldo.
5. En una base de pruebas respaldada, simula que el plazo de una reserva Pendiente con algún pago ya venció. Debe mostrar «RevisionManual» y no debe cancelarse sola.
6. Con una reserva Pendiente sin pagos vencida, el servicio de Fase 4.2 debe cancelarla; después debe desaparecer del centro de alertas. Evita cambiar fechas en reservas reales.
7. Comprueba que Operador y Cliente no pueden entrar en este panel ni consumir el endpoint protegido (HTTP 403).
8. Consulta las alertas dos veces: no deben crearse cargos, devoluciones, reservas ni registros duplicados.

## Pendientes
- Comunicación al viajero con consentimiento, plantillas, límites de frecuencia y bitácora de entrega; aún **no hay envío de recordatorios**.
- Reglas de saldo restante por fecha de pago (solo existe el control de 48 horas antes de la excursión).
- Pruebas automatizadas de integración, horarios e idempotencia visual.
- Compilación y prueba real en la PC del usuario aún pendientes.
