# TourSmart RD — Fase 3.2: cambio de contraseña y revocación de sesiones

## Implementado
- Perfil del viajero → `/viajero/seguridad` con contraseña actual, nueva y confirmación.
- `POST /api/MiCuenta/cambiar-contrasena`, rol Cliente. Verifica la contraseña actual con `IPasswordHasher<Usuario>` y genera un nuevo hash.
- Requiere una contraseña **diferente de la anterior**, de **10 a 100 caracteres**, con mayúscula, minúscula, número y símbolo.
- `Usuario.VersionSesion` es una cadena aleatoria de 32 caracteres renovada en el cambio de contraseña. La actualización del hash y de esta versión se realiza de manera atómica, con condición de versión y hash previos.
- Los nuevos JWT llevan el claim `version_sesion`. El middleware JWT valida, contra SQL Server, que coincida con la versión actual del usuario en **cada solicitud autenticada**.
- La cookie MVC de un viajero también revalida su JWT con `GET /api/MiCuenta/sesion`, rechazando la cookie cuando la API devuelve 401/403, falla la verificación o la API no está disponible.
- El cambio exitoso elimina el JWT actual de la memoria MVC, cierra su cookie e informa al usuario al regresar a login.
- La migración `20261107140000_VersionSesionUsuarios` añade `VersionSesion` y asigna valores aleatorios a los usuarios anteriores. **La migración invalida los tokens de login antiguos; todos deben iniciar sesión nuevamente.**
- No hay recuperación de contraseñas mediante email, códigos de recuperación ni verificación de identidad por correo. Esa es una fase independiente.

## Aplicación
1. Respaldar `AgenciaToursDB` desde SQL Server Management Studio.
2. Detener API y MVC.
3. Desde la raíz del repositorio:
   ```powershell
   git status
   git pull origin feature/toursmart-integrador-ii
   dotnet build AgenciaToursAPI.sln
   ```
   Si hay modificaciones locales, resolverlas antes de hacer pull.
4. SOLO si se obtiene 0 errores y hay respaldo, ejecutar:
   ```powershell
   dotnet ef database update --project .\AgenciaToursAPI.csproj
   ```
5. Iniciar ambas aplicaciones desde dos terminales:
   ```powershell
   dotnet run --project .\AgenciaToursAPI.csproj --launch-profile http
   dotnet run --project .\TourSmart.Web\TourSmart.Web.csproj --launch-profile http
   ```

## Pruebas
1. Entrar como viajero ficticio, abrir `http://localhost:5291/viajero/seguridad`.
2. Abrir sesión con esa misma cuenta en **otro navegador o ventana de incógnito**.
3. Contraseña actual incorrecta: esperar error y **no** modificar credenciales.
4. Nueva contraseña igual a la actual: esperar rechazo.
5. Contraseña sin mayúscula, minúscula, número o símbolo: esperar rechazo.
6. Dos contraseñas nuevas diferentes: formulario rechaza.
7. Cambiar correctamente: el navegador principal debe cerrar la sesión y redirigir a `/cuenta/acceder` con mensaje de éxito.
8. Desde el segundo navegador, refrescar `/viajero/reservas`. Debe enviar al login y no mostrar datos privados.
9. Intentar entrar con la contraseña anterior: debe rechazarla.
10. Entrar con la contraseña nueva: debe funcionar y conservar todas las reservas, datos de perfil y rol Cliente.
11. Confirmar que la cuenta Administrador todavía puede iniciar sesión con sus credenciales.
12. No compartir contraseñas, hashes ni tokens en capturas.

## Consideraciones
- La invalidación de cookies de clientes depende de la disponibilidad de la API y se ejecuta **en cada solicitud**. Es una decisión de seguridad adecuada para el entorno de desarrollo, pero puede aumentar carga y latencia: evaluar una arquitectura de sesión centralizada antes de producción.
- La nueva migración implica que tokens antiguos carecen de `version_sesion`; se forzará login tras desplegar.
- La fase no cambia roles ni reservas.
- No existe rate limiting de login/cambio de contraseña, bloqueo por intentos, verificación de correo, recuperación de acceso, 2FA o notificaciones de seguridad. Son pendientes necesarios antes de exponer el sistema públicamente.
- Código actualizado en GitHub; la compilación y pruebas reales deben realizarse en el entorno del usuario.
