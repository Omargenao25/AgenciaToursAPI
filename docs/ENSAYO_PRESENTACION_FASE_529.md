# TourSmart RD — Fase 5.29: versión candidata y ensayo de defensa

## Estado comprobado
Última ejecución de GitHub Actions de la fase 5.28: **f27915fd** — correcta (compilación Release, pruebas xUnit, publish de API y Web). Esta referencia es una **línea base conocida**, no una versión comercial certificada. Tras modificaciones posteriores, confirmar de nuevo la CI del commit actual. No se crea tag ni se modifica `master`.

## Objetivo de la demostración
Contar la historia completa de una excursión: catálogo → datos de salida → reserva con pasajeros → comprobante de transferencia reportado → verificación humana → confirmación → control administrativo/manifesto → reporte. La agencia **no cobra dinero en línea** ni valida automáticamente transferencias bancarias.

## Datos que preparar antes del ensayo
- Una base SQL **de demostración**, respaldada, distinta de cualquier base con clientes reales.
- Tres cuentas **de prueba** con roles Cliente, Operador y Administrador; no incluir contraseñas en capturas, diapositivas, repositorio ni chat.
- Un tour futuro denominado por ejemplo "Demo TourSmart", con cupos suficientes, horario de Santo Domingo, punto de encuentro, instrucciones y recomendaciones; guardar su ID.
- Una reserva de prueba pendiente y otra confirmada; anotar sus ID. Una reserva de otro cliente para comprobar aislamiento si se ejecuta la auditoría completa.
- Un comprobante bancario **ficticio, visiblemente rotulado PRUEBA / NO VÁLIDO PARA PAGO** para mostrar el flujo de revisión, sin datos financieros personales.
- Fotografías locales respaldadas: `TourSmart.Web/wwwroot/uploads/`.
- Correo propio de prueba, si se desea mostrar mensajes existentes. Durante la exposición es mejor mostrar un correo **previamente recibido** que depender del SMTP en directo.

## Preparación técnica en PowerShell
**Detener API y Web con Ctrl+C** antes de actualizar. Desde la raíz del repositorio:

```powershell
git status
git checkout feature/toursmart-integrador-ii
git pull --ff-only origin feature/toursmart-integrador-ii
dotnet restore AgenciaToursAPI.sln
dotnet build AgenciaToursAPI.sln
dotnet test .\TourSmart.Tests\TourSmart.Tests.csproj
```

Si tus modificaciones en `Script-Sql/AgenciaToursDB.sql` impiden pull, usa el procedimiento seguro ya probado:
```powershell
git stash push -m "Respaldo SQL antes del ensayo" -- Script-Sql/AgenciaToursDB.sql
git pull --ff-only origin feature/toursmart-integrador-ii
git stash apply 'stash@{0}'
```
Verifica que no haya conflictos; `apply` **conserva** el stash. No uses `git reset --hard` ni `git clean -fd`. **La fase 5.29 no crea una migración**; no ejecutar `dotnet ef database update` por esta fase.

Terminal 1 — API:
```powershell
dotnet run --project .\AgenciaToursAPI.csproj --launch-profile http
```

Terminal 2 — Web:
```powershell
dotnet run --project .\TourSmart.Web\TourSmart.Web.csproj --launch-profile http
```

Terminal 3 — smoke HTTP de solo lectura:
```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\Test-TourSmartSmoke528.ps1
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\Test-TourSmartPreDespliegue.ps1
```
Solo si tienes cuentas e IDs correctos de un entorno aislado, ejecuta la auditoría de seguridad con credenciales introducidas localmente:
```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\Invoke-TourSmartAuditoriaFinal.ps1 -TourId 1 -ReservaId 1
```
Sustituye 1 por IDs reales de prueba. No envíes tokens ni contraseñas al chat.

## Guion sugerido: 12–15 minutos

| Orden | Responsable propuesto | Pantalla confirmada | Demostración y mensaje principal |
| --- | --- | --- | --- |
| 1 | Omar | `http://localhost:5291/` | Qué problema resuelve TourSmart RD y cómo se separan API/Web/SQL |
| 2 | Omar | `http://localhost:5291/tours` | Buscar tour, comprobar disponibilidad, fotografías y precio |
| 3 | Omar | `http://localhost:5291/tours/{tourId}` | Mostrar punto de encuentro, instrucciones y qué llevar |
| 4 | Omar | `http://localhost:5291/cuenta/acceder` | Iniciar sesión con Cliente de prueba; no mostrar la contraseña |
| 5 | Omar | `http://localhost:5291/viajero/reservas` | Mostrar reserva previamente preparada; opcional: `/viajero/reservar/{tourId}` con datos ficticios |
| 6 | Russell | `http://localhost:5291/panel` | Explicar roles: administrador/operador y restricciones |
| 7 | Russell | `http://localhost:5291/panel/tours` | Mostrar editor de logística y catálogos; no sobrescribir el tour de demostración |
| 8 | Russell | `http://localhost:5291/panel/reservas` | Estados Pendiente/Confirmada/Cancelada y cupos |
| 9 | Russell | `http://localhost:5291/panel/finanzas/adelantos` | Diferencia entre comprobante reportado y dinero aprobado tras verificación |
| 10 | Ambos | `http://localhost:5291/panel/tours/{tourId}/manifiesto` | Pasajeros y asistencia; no cerrar un tour salvo que la prueba lo requiera |
| 11 | Ambos | Mostrar resultados de xUnit y GitHub Actions | Explicar control de cupos, redondeo, autorización y CI |
| 12 | Ambos | Cierre | Límites honestos: SMTP no asegura recepción ni una sola entrega; despliegue comercial requiere pruebas adicionales |

El reparto es orientativo y se puede adaptar al grupo real. **Las rutas entre llaves deben sustituirse por IDs existentes.** Algunas acciones financieras se realizan desde botones en las vistas; no se debe prometer una URL que no se haya comprobado.

## Plan de contingencia
- **API no responde:** verificar que se inició la terminal 1 en `http://localhost:5290`; no seguir haciendo clic en formularios que escriben datos. Mostrar explicación de arquitectura.
- **Web no responde:** comprobar terminal 2 `http://localhost:5291`; tener capturas preparadas de pantallas ya probadas, eliminando nombres/correos reales.
- **SQL Server falla:** no editar ni restaurar datos en plena exposición; explicar flujos con capturas y la evidencia de GitHub Actions. Una compilación correcta no demuestra operaciones de SQL.
- **Gmail tarda o falla:** mostrar correos de prueba recibidos previamente. No reenviar repetidamente ni activar recordatorios sobre reservas reales.
- **Permisos:** usar tres sesiones separadas o cerrar sesión antes de cambiar de rol; no poner tokens en la consola proyectada.
- **Problema en pago o cancelación:** no improvisar transferencias reales. Mostrar una reserva de ejemplo ya preparada y cómo queda auditada.

## Criterios de cierre de la versión candidata
- [ ] GitHub Actions del commit elegido en verde.
- [ ] `dotnet build`, `dotnet test` y smoke HTTP local con 0 errores.
- [ ] Migraciones pendientes de 5.25/5.26 ya aplicadas sobre copia de prueba, con respaldo verificado.
- [ ] Catálogo y datos de salida coinciden entre administrador y cliente.
- [ ] Reservas, capacidad y adelantos comprobados sin usar pagos reales.
- [ ] Roles y aislamiento de reserva entre clientes comprobados mediante auditoría local.
- [ ] Fotos y base SQL respaldadas; evitar cambios de última hora.
- [ ] Ensayo cronometrado realizado y diapositivas actualizadas al funcionamiento real.

**Regla de congelación:** Cuando todo esté comprobado en la PC del expositor, anotar el hash mediante `git rev-parse HEAD`, evitar modificaciones de código y preparar una copia de las capturas. No crear etiquetas ni fusionar a `master` sin revisión y aprobación explícita.
