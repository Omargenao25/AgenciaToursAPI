# TourSmart RD — Fase 4.5: exportación de reporte financiero a CSV

## Objetivo
- Exportar **todos los movimientos del período**, no solo los 500 registros visibles en el panel financiero.
- Mantener filtros de fechas UTC y acceso exclusivo al rol Administrador.
- No alterar pagos, reembolsos, reservas ni saldos.

## Endpoints
- API (JWT Administrador): `GET /api/reportes/finanzas/exportar-csv?desde=2026-11-01&hasta=2026-11-30`.
- Web (cookie válida con rol Administrador): `GET /panel/reportes/finanzas/exportar-csv?desde=2026-11-01&hasta=2026-11-30`.
- Desde la página `/panel/reportes/finanzas` seleccionar rango, pulsar **Aplicar período** y luego **Descargar CSV del período**.
- Si no se indican fechas, el intervalo va del primer día del mes UTC al día UTC actual. Máximo 366 días por consulta.

## Formato
- Nombre: `TourSmart_Finanzas_YYYYMMDD_YYYYMMDD.csv`.
- UTF-8 con BOM para mayor compatibilidad con Excel y LibreOffice.
- Separador `;` (punto y coma); cada valor de texto aparece entre comillas dobles.
- Cabeceras: `Tipo;FechaUTC;ReservaId;MovimientoId;Cliente;Tour;Metodo;Referencia;Responsable;MontoNetoRD`.
- Los pagos tienen un importe **positivo** y los reembolsos un importe **negativo**, con dos decimales y punto decimal invariante. Se pueden sumar para obtener el flujo neto.
- La exportación está organizada en dos bloques cronológicos: primero pagos y luego reembolsos; no es un único orden cronológico entre ambos tipos.
- Se emite el archivo a medida que se consulta a SQL Server; no se carga el conjunto entero en memoria. Los movimientos pueden ser muchos; para archivos enormes conviene seleccionar un rango más pequeño.

## Seguridad
- La API valida el rol **Administrador** aun si alguien intenta el endpoint directamente.
- La web no expone el JWT en la URL, usa su cliente autenticado para obtener el archivo.
- `Cache-Control: private, no-store`, `X-Content-Type-Options: nosniff` y archivo descargable.
- Protección de **CSV formula injection** en campos textuales de origen externo: normaliza saltos de línea/tabulaciones, duplica comillas y añade un apóstrofo si el primer carácter no blanco es `=`, `+`, `-` o `@`. Los importes numéricos controlados por servidor se conservan como números (incluidos negativos).
- Las rutas se construyen con fechas validadas por el servidor y nunca con texto libre enviado como nombre de archivo.
- El CSV contiene nombres de clientes y referencias de transacción. Debe tratarse como **información confidencial** y almacenarse/compartirse con las mismas protecciones que los demás reportes financieros.

## Cómo instalar
1. Detener API y web. Comprobar `git status` y resolver cualquier cambio local pendiente.
2. `git pull origin feature/toursmart-integrador-ii`
3. `dotnet build AgenciaToursAPI.sln` — buscar 0 errores.
4. Reiniciar API y web como en las fases anteriores. **Esta fase no tiene migración** ni requiere `dotnet ef database update`.

## Pruebas
1. Como Administrador, entrar en Reporte financiero y seleccionar el intervalo que contiene los datos de prueba.
2. Aplicar las fechas y descargar CSV. Abrir en LibreOffice Calc, indicando UTF-8 y separador `;` si se solicita.
3. Comprobar cabecera, filas, IDs de reserva, métodos, referencias y responsables.
4. Confirmar que pagos por RD$2,000, devoluciones por RD$500 y RD$1,500 generan números `2000.00`, `-500.00` y `-1500.00`, respectivamente. La suma debe ser 0, si no existen otros movimientos en el intervalo.
5. Seleccionar un período sin movimientos: el CSV conserva su cabecera y ninguna fila de datos.
6. Intentar acceder con Operador o Cliente: acceso denegado por web y API (403).
7. Comprobar un texto de prueba ficticio que comience con `=2+2` en una referencia o nombre: la hoja de cálculo debe mostrarlo como texto y no ejecutar la fórmula.
8. Con más de 500 movimientos en datos de prueba, el panel visual solo muestra 500, pero el CSV debe contener **todos**.
9. Verificar que reembolsos de otros períodos se contabilizan según su fecha de registro, no según la fecha del pago que originó la devolución.
10. Confirmar que consultar y descargar no modifican registros financieros.

## Límites
- No hay PDF ni XLSX generado por la aplicación. Excel/Calc permiten importar el CSV.
- No hay conciliación contra estados de cuenta bancarios reales.
- El CSV se genera desde los registros del momento de descarga; podrían cambiar mientras se exporta si hay nuevos movimientos simultáneos. Para conciliaciones contables cerradas se necesitarían períodos bloqueados/snapshots y auditoría.
- Compilación e integración reales pendientes de validación en la PC del usuario.
