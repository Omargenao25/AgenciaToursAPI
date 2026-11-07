# TourSmart RD — Fase 3.4: comprobantes de reserva para viajeros

## Funcionalidad
- El turista autenticado entra en `/viajero/reservas` y pulsa **Ver comprobante** en cualquiera de sus reservas.
- MVC expone `GET /viajero/reservas/{id}/comprobante` con `[Authorize(Roles="Cliente")]`.
- La API expone `GET /api/MiCuenta/reservas/{id}/comprobante` con la misma restricción de rol, y filtra por el `ClienteId` derivado del usuario del JWT. Un ID inexistente o ajeno devuelve 404, sin exponer datos.
- Los datos proceden del servidor: titular, excursión, fecha/hora del tour, fecha de creación, personas, método de pago indicado, total y estado actual.
- Diseño imprimible A4: botón **Imprimir / Guardar PDF** utiliza el diálogo de impresión del navegador, sin nuevas dependencias ni librerías PDF.
- Los comprobantes se pueden consultar aunque estén cancelados, manteniendo su estado histórico.
- Estados claramente diferenciados: pendiente NO garantiza plaza confirmada; confirmada es confirmación de agencia, NO comprobante de pago; cancelada NO permite usar el tour.
- Sin migraciones de base de datos, ni conexión de correo, ni nuevo proveedor de pagos.

## Pasos de prueba
1. Detener API y MVC. `git status` y, si no hay cambios locales pendientes, `git pull origin feature/toursmart-integrador-ii`.
2. `dotnet build AgenciaToursAPI.sln` — objetivo: 0 errores y 0 advertencias.
3. Iniciar API en el perfil HTTP 5290 y MVC en HTTP 5291.
4. Abrir sesión con Manuel y entrar a `http://localhost:5291/viajero/reservas`.
5. Abrir comprobante de la reserva **#8**, previamente cancelada. Debe indicar **Cancelada**, 1 persona, RD$8,000 y advertencia histórica.
6. Pulsar Imprimir / Guardar PDF: la vista impresa debe ocultar menú, pie y acciones; mostrar sólo comprobante.
7. Crear una reserva nueva de prueba y abrirla pendiente. Debe distinguir solicitud pendiente de confirmación final.
8. Desde el panel administrativo confirmar esa reserva y volver al comprobante del turista: debe indicar **Confirmada**.
9. Acceder con Rohanny a `/viajero/reservas/8/comprobante`: esperar 404 (nunca datos de Manuel).
10. Sin sesión o desde usuario administrativo, acceso al comprobante del cliente debe bloquearse.
11. Revisar versión móvil y comprobar que no se muestren datos de identidad sensibles adicionales.

## Seguridad y límites
- Enlaces imprimibles solo estando autenticado, sin tokens en la URL; el JWT continúa en memoria del servidor MVC.
- `Cache-Control: private, no-store` en API y MVC para la página del comprobante.
- No es factura fiscal, ni recibo de pago, ni pase de entrada automático.
- Queda pendiente validar la compilación y todas las pruebas en el equipo del usuario.
