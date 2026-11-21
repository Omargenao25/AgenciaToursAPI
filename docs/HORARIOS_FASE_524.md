# TourSmart RD — Fase 5.24: hora de excursiones independiente del servidor

## Problema corregido
Las fechas y horas de salida `Tour.Fecha` + `Tour.Hora` son **horas civiles de la agencia en República Dominicana**, y se guardan sin conversión a UTC en el esquema actual. Anteriormente, validaciones de Tours/Reservas/Check-in/Mis reservas y el selector Web comparaban estas fechas contra `DateTime.Now` (hora local del servidor). Si API y Web se ejecutaban en servidores UTC o en zonas distintas, un mismo tour podía verse disponible en un sitio y vencido en otro.

## Implementación
- Agregados `Security/HorarioAgencia.cs` en API y `TourSmart.Web/Services/HorarioAgencia.cs` en Web. Ambos usan `TimeZoneInfo` para `America/Santo_Domingo`, con identificador Windows `SA Western Standard Time` de respaldo, a partir de `DateTime.UtcNow`.
- Los controladores de Tours, Reservas, MiCuenta, Check-in, los formularios Web de reservas y el servicio de recordatorios usan `HorarioAgencia.Ahora` para comparaciones con la salida.
- El check-in captura una sola hora por operación para su actualización condicional en SQL y evitar valores cambiantes entre evaluaciones.
- `VencePendienteUtc`, `FechaCambioEstado`, `RecordatorioIntentadoUtc`, `RecordatorioEnviadoUtc` y otros instantes técnicos continúan utilizando UTC.
- Las fechas civiles existentes de tours y reservas no se reescriben ni se convierten: **no hay migración de base de datos en esta fase**.

## Pasos locales
1. Detén API y Web con Ctrl+C; respalda tus fotos y los datos de demostración.
2. Si `git status` muestra cambios locales, consérvalos antes del pull; no uses reset/clean.
3. Desde la raíz:

```powershell
git checkout feature/toursmart-integrador-ii
git pull --ff-only origin feature/toursmart-integrador-ii
dotnet build AgenciaToursAPI.sln
```

API (terminal 1):
```powershell
dotnet run --project .\AgenciaToursAPI.csproj --launch-profile http
```

Web (terminal 2):
```powershell
dotnet run --project .\TourSmart.Web\TourSmart.Web.csproj --launch-profile http
```

**No ejecutar dotnet ef database update por esta fase**.

## Casos de prueba
- Crea un tour con salida futura en hora de Santo Domingo, por ejemplo 08:00. Verifica que se vea en catálogo, panel administrativo, registro de reserva y página de viajero.
- Verifica que los cupos, porcentaje de adelanto, plazo y reservas anteriores siguen intactos.
- Una excursión cuya hora de salida ya pasó debe rechazar nuevas reservas y check-in.
- Compara Web/API en una máquina cuyo reloj local use UTC (o bajo una configuración controlada de TZ): ambos deben evaluar la disponibilidad según hora dominicana.
- Si los recordatorios están habilitados, utiliza una reserva confirmada de prueba dentro de la ventana configurada, una dirección de correo propia y comprueba que la salida que se muestra en el mensaje corresponde a la hora de Santo Domingo.
- Ejecuta scripts previos de auditoría de reservas y seguridad y confirma las mismas restricciones 401/403.

## Pendientes
Esto unifica una única zona **de agencia** para todos los tours: los destinos con salidas sujetas a la zona local de otro país requerirán un campo `TimeZoneId` por excursión antes de soportarlos comercialmente. La entrega garantizada de recordatorios con reintentos corresponde a la siguiente fase. La CI prueba compilación/publish; no pruebas end-to-end de SQL/SMTP.
