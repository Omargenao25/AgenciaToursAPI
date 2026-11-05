# Configuración segura de TourSmart RD

## Importante: clave JWT publicada anteriormente
La versión original de `appsettings.json` contenía `Jwt:Key` dentro del historial público de Git. Quitarla del archivo actual **no borra el historial**, ni invalida por sí solo tokens antiguos. **Considera comprometida la clave anterior**. Antes de usar el sistema de nuevo fuera de una prueba aislada:
1. Genera una clave JWT aleatoria nueva y reemplázala en todos los entornos donde se utilice. Esto invalidará los tokens emitidos con la anterior, siempre que los servicios dejen de aceptarla.
2. Configúrala mediante Secret Manager (desarrollo) o variables de entorno/gestor de secretos (producción).
3. Si la clave antigua se usó en otros servicios, reemplázala también allí. Evalúa la limpieza del historial con el equipo; reescribir historial no sustituye la rotación.

## Desarrollo local con .NET 8
Desde la carpeta que contiene `AgenciaToursAPI.csproj`, ejecuta:

```powershell
dotnet user-secrets set "Jwt:Key" "<CLAVE_ALEATORIA_NUEVA_DE_AL_MENOS_32_BYTES>"
```

Para generar una clave de 64 bytes mediante PowerShell (no la publiques):

```powershell
$bytes = New-Object byte[] 64
[System.Security.Cryptography.RandomNumberGenerator]::Fill($bytes)
$clave = [Convert]::ToBase64String($bytes)
dotnet user-secrets set "Jwt:Key" "$clave"
```

La API espera `Jwt:Key` al iniciar y fallará expresamente cuando no esté configurada. El valor de `UserSecretsId` en el `.csproj` permite a .NET cargar los secretos de desarrollo sin almacenarlos en el repositorio.

## Despliegue
Configurar `Jwt__Key` como variable de entorno secreta en el servidor (sin registrarla en logs o archivos Git). Asegúrate de usar HTTPS, un almacén de secretos y credenciales de base de datos seguras. Inspecciona `ConnectionStrings:Conexion` antes de desplegar: sustituye valores de desarrollo por la cadena apropiada del entorno, preferiblemente como variable `ConnectionStrings__Conexion`.

## Verificación manual
1. Sin `Jwt:Key`, `dotnet run` debe detenerse con error de configuración.
2. Configura `Jwt:Key` nueva y ejecuta `dotnet build` y `dotnet run`.
3. En Swagger, prueba `POST /api/Auth/login` con un usuario de prueba.
4. Verifica que endpoints `[Authorize]` rechazan peticiones sin token con HTTP 401.
5. Revisa que no aparezcan secretos en commits, capturas, logs o documentación.

> Este cambio no ejecutó pruebas ni rotó la clave en instancias desplegadas. Es necesaria la rotación real, y quedan pendientes pruebas automatizadas de seguridad.
