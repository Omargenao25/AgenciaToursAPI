# TourSmart RD — Fase 5.10: concurrencia controlada de geografía

## Qué comprueba
`scripts/Test-TourSmartConcurrenciaGeografia.ps1` envía **dos POST simultáneos** para registrar un país ficticio con un nombre aleatorio. Se espera que SQL Server admita exactamente una creación (**201**) y rechace la otra (**409**). Al final, busca el registro temporal y procura eliminarlo.

## Protección obligatoria
Esta prueba **escribe y borra datos**. NO es la auditoría HTTP de solo lectura. No la ejecutes todavía sobre la base de datos habitual del proyecto ni sobre producción.

1. Preparar una base SQL **aislada de pruebas**, con respaldo verificado.
2. Configurar una instancia local de la API cuya cadena de conexión apunte exclusivamente a esa base aislada.
3. Usar un usuario con rol Administrador u Operador de la base de pruebas.
4. El script solo admite API `localhost` o `127.0.0.1` y exige `-BaseAisladaConfirmada`. Esto no puede verificar por sí solo el destino real de la cadena SQL: el operador debe comprobarlo antes.
5. No incluir JWT en capturas, documentación, commits ni chats. Guardarlo temporalmente en variable de entorno de la terminal de pruebas.
6. Si aparece `[ALERTA]`, revisar manualmente la base aislada: la limpieza pudo haber fallado.

## Comandos (solo después de preparar aislamiento)
```powershell
git pull origin feature/toursmart-integrador-ii
$env:TOURSMART_TEST_TOKEN = "<JWT de prueba, solamente en tu PowerShell>"
Set-ExecutionPolicy -Scope Process -ExecutionPolicy Bypass
.\scripts\Test-TourSmartConcurrenciaGeografia.ps1 -BaseAisladaConfirmada
Remove-Item Env:TOURSMART_TEST_TOKEN
```
Resultado esperado: un HTTP 201, un HTTP 409 y registro temporal eliminado.

## Desarrollo normal: dos terminales
Terminal 1:
```powershell
dotnet run --project .\AgenciaToursAPI.csproj --launch-profile http
```
Terminal 2:
```powershell
dotnet run --project .\TourSmart.Web\TourSmart.Web.csproj --launch-profile http
```

**No ejecutar la prueba de concurrencia en la misma API que usa la base de trabajo habitual.** Primero comprobar aislamiento. Si aún no está disponible, continuar con las pruebas de solo lectura.
