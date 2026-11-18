# TourSmart RD — Auditoría final 5.20

Ejecutar exclusivamente contra la API y web locales con cuentas de prueba. El script inicia sesión por POST (puede actualizar metadatos de autenticación), pero las verificaciones de datos usan GET y no crean reservas, pagos ni comprobantes.

Desde la raíz, con ambas aplicaciones activas:
```powershell
.\scripts\Invoke-TourSmartAuditoriaFinal.ps1 -TourId 1 -ReservaId 1
```
Sustituir los IDs por registros existentes de prueba, y añadir -ReservaAjenaId seguido del ID de una reserva existente de otro cliente para verificar aislamiento. Sin ese argumento se omite el aislamiento, y un resultado sin fallos no significa que se haya probado.

El script solicita credenciales de Administrador, Operador y Cliente verificado, sin imprimir JWT ni claves. Restaura las variables de entorno anteriores al terminar. Ejecuta la matriz HTTP y auditoría de reservas. No prueba mutaciones, concurrencia, SMTP ni la entrega de correos.

Antes de presentar, completar manualmente en base aislada: registro y verificación, recuperación y revocación de sesiones, reserva con pasajeros, rechazo/aprobación de comprobantes, pago duplicado, adelanto insuficiente, cancelación y devolución, recordatorio sin duplicado, check-in y cierre. Revisar correo únicamente con buzones propios. No compartir enlaces con tokens. La compilación de CI no certifica seguridad ni ejecución de scripts contra SQL local.
