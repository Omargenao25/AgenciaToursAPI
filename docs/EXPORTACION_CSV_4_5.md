# TourSmart RD — Fase 4.5: exportación financiera CSV

## Funcionalidad
- En el panel `/panel/reportes/finanzas`, **Descargar CSV del período** utiliza el mismo filtro de fechas UTC que el reporte.
- Web: `GET /panel/reportes/finanzas/exportar-csv?desde=AAAA-MM-DD&hasta=AAAA-MM-DD`.
- API: `GET /api/reportes/finanzas/exportar-csv?desde=AAAA-MM-DD&hasta=AAAA-MM-DD`.
- **Solo Administrador**: se verifica tanto la sesión web como el JWT de la API. Cliente y Operador no tienen acceso.
- El período incluye ambas fechas; máximo 366 días a partir del año 2000.
- El archivo usa UTF-8 con BOM, separador punto y coma y dos decimales con punto (formato invariante). Es compatible con importación CSV en Excel y LibreOffice.
- Columnas: Tipo, FechaUTC, ReservaId, MovimientoId, Cliente, Tour, Metodo, Referencia, Responsable, MontoNetoRD.
- Los **pagos** se exportan con importe positivo; los **reembolsos** con importe negativo. La suma de `MontoNetoRD` coincide con el flujo neto del período, siempre que no se creen nuevos movimientos durante la consulta o descarga.
- Exporta **todos** los movimientos de esas fechas: el límite de 500 corresponde solo a la vista HTML. La API transmite las filas por bloques sin acumular el conjunto completo en memoria.
- El archivo agrupa primero pagos y después reembolsos, ordenados internamente por fecha; **no** presenta un historial global ordenado cronológicamente.
- Se aplican escapes de comillas, delimitadores y saltos de línea. Los textos de usuarios potencialmente interpretables como fórmulas de hojas de cálculo se prefijan con una comilla simple.
- La API envía `Content-Disposition: attachment`, `Cache-Control: private, no-store` y `X-Content-Type-Options: nosniff`.
- Es un exporte de operaciones **registradas manualmente**, no prueba de que el movimiento bancario se ejecutó. No incorpora información de tarjetas ni credenciales.

## Instalación
No requiere migración nueva ni paquetes adicionales.

En raíz del repositorio, detener API y MVC, revisar cambios locales y ejecutar:
```powershell
git status
git pull origin feature/toursmart-integrador-ii
dotnet build AgenciaToursAPI.sln
```
Arrancar ambos proyectos:
```powershell
dotnet run --project .\AgenciaToursAPI.csproj --launch-profile http
dotnet run --project .\TourSmart.Web\TourSmart.Web.csproj --launch-profile http
```
(Ejecutar en terminales diferentes).

## Pruebas guiadas
1. Entrar como Administrador a `http://localhost:5291/panel/reportes/finanzas`.
2. Seleccionar fechas UTC que incluyan los pagos y reembolsos ficticios ya probados, pulsar **Aplicar período** y anotar cobros, devoluciones y neto.
3. Pulsar **Descargar CSV del período** y abrirlo en Excel o LibreOffice. Si se solicita, escoger delimitador **punto y coma** y codificación **UTF-8**.
4. Comprobar que las filas `Pago` tienen signo positivo, las filas `Reembolso` tienen signo negativo y la suma numérica de la columna **MontoNetoRD** coincide con el neto mostrado (si no se registraron movimientos intermedios).
5. Verificar que una reserva ficticia reembolsada y cancelada conserva sus movimientos en el CSV.
6. Filtrar un período sin actividad: descargar un CSV con encabezados, sin movimientos.
7. Probar Operador y Cliente: acceso web denegado y API HTTP 403. No debe exponerse un CSV sin autorización.
8. Probar un texto de referencia ficticio que empiece con `=1+1` y abrir el CSV: debe conservarse como **texto**, sin evaluar una fórmula.
9. Confirmar que exportar no modifica pagos, reembolsos, saldos ni estados de reserva.
10. Confirmar que los totales del HTML siguen siendo correctos si muestra el aviso de más de 500 movimientos.

## Pendientes
- Pruebas de seguridad y rendimiento sobre archivos grandes (concurrencia, límite de descarga, memoria, corte de conexión).
- Registro de auditoría de exportaciones sensibles; roles de acceso granular; protección de información personal al compartir el CSV.
- Exportaciones por tour, método o cliente; reportes contables y conciliación bancaria.
- Compilación y ejecución reales pendientes de verificar en el equipo del usuario.
