# TourSmart RD — Fase 5.33: pulido visual, navegación móvil y accesibilidad

**Rama exclusiva de rediseño:** `feature/toursmart-visual-530`.
**Rama estable protegida:** `feature/toursmart-integrador-ii`.
No se modifican SQL Server, migraciones, reglas de negocio, rutas, pagos ni permisos.

## Implementación
- Navegación inferior móvil, solo para usuarios autenticados con rol **Cliente**: Inicio, Explorar, Mis viajes y Perfil, enlazados a controladores MVC existentes. Resalta la sección activa con `aria-current="page"`.
- No aparece en escritorio (a partir de 768 px) ni en el panel de Operador/Administrador. La barra principal sigue incluyendo su menú móvil desplegable para el resto de las funciones.
- Ajustes responsive, área segura (`env(safe-area-inset-bottom)`), mayor superficie táctil, foco visible con teclado y control del desbordamiento en tarjetas y formularios.
- La navegación móvil desplegable ahora puede cerrarse con **Escape** y el foco vuelve al botón de menú.
- Respeta la preferencia del sistema de **reducir movimiento**. No se dependen de animaciones para enviar formularios ni mostrar pagos.

## Comprobar en el equipo del expositor
1. Detener API/Web con Ctrl+C.
2. Revisar cambios locales con `git status`. Si Git impide actualizar por SQL editado, respaldarlo con `git stash push -m "SQL fase 5.33" -- Script-Sql/AgenciaToursDB.sql`, luego `git pull --ff-only origin feature/toursmart-visual-530` y recuperar con `git stash apply 'stash@{0}'` **solo tras comprobar** que esa es la entrada correcta. Si hay conflicto, detenerse. No borrar `uploads/`.
3. Ejecutar:

```powershell
git switch feature/toursmart-visual-530
git pull --ff-only origin feature/toursmart-visual-530
dotnet build AgenciaToursAPI.sln
dotnet test .\TourSmart.Tests\TourSmart.Tests.csproj
```

4. Iniciar procesos separados:

```powershell
dotnet run --project .\AgenciaToursAPI.csproj --launch-profile http
```

```powershell
dotnet run --project .\TourSmart.Web\TourSmart.Web.csproj --launch-profile http
```

## Matriz de aceptación manual
- [ ] Cliente, ancho 375 px: barra inferior visible; Inicio, Explorar, Mis viajes, Perfil navegan correctamente.
- [ ] Cliente, ancho 320 px: botones no quedan fuera de pantalla y títulos no se cortan.
- [ ] Cliente, ancho 768/1280 px: barra inferior desaparece y navegación superior funciona.
- [ ] Operador y Administrador, móvil: barra inferior de viajero **no** se muestra.
- [ ] Cliente, reservar/adelantos: etiquetas, campos, importe y envío de comprobantes siguen disponibles sin desplazamiento lateral indebido.
- [ ] Cabecera móvil: abrir menú, pulsar Escape, comprobar cierre y foco.
- [ ] Activar reducción de movimiento: los contenidos siguen apareciendo y los botones funcionan.
- [ ] Usar Tab/Shift+Tab para comprobar foco visible y orden de navegación.
- [ ] Efectuar prueba de humo `powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\Test-TourSmartSmoke528.ps1` con API/Web arrancados.
- [ ] Ejecutar un flujo de reserva y adelanto **ficticios** con cliente de prueba y base aislada, sin cobros reales.

## Alcance de CI
GitHub Actions compila y publica API/Web y ejecuta xUnit, pero **no ejecuta pruebas visuales de navegador ni valida navegación real con las tres sesiones de roles**. No fusionar el diseño en la rama estable hasta completar pruebas responsive y funcionales en la computadora de demostración.

Esta fase no requiere migración SQL.
