# TourSmart RD — Prueba de login MVC

## Implementación
- `GET /cuenta/acceder`: formulario responsive con validación.
- `POST /cuenta/acceder`: valida antiforgery, llama a `POST /api/Auth/login` por `HttpClient` del servidor; no se genera ni almacena una contraseña local.
- La respuesta de la API determina el rol Administrador, Operador o Cliente.
- Se crea una cookie de autenticación ASP.NET Core (HttpOnly, SameSite Lax), sin token JWT dentro del ticket.
- El JWT se mantiene en memoria del proceso web vinculado a una ID de sesión aleatoria, con vencimiento igual al de la API. La cookie se invalida cuando la sesión ya no existe en caché.
- `POST /cuenta/salir`: borra el token del caché y cierra sesión.
- `GET /panel`: vista privada contextual según rol. **Todavía no contiene CRUD administrativo ni mis reservas**.
- El acceso en el encabezado ahora enlaza al formulario y se transforma en Mi espacio / Salir al iniciar sesión.

## Pruebas locales
1. Detener ambas aplicaciones y en la carpeta del repositorio ejecutar:
   ```powershell
   git status
   git pull origin feature/toursmart-integrador-ii
   dotnet build AgenciaToursAPI.sln
   ```
2. Iniciar API:
   ```powershell
   dotnet run --project .\AgenciaToursAPI.csproj --launch-profile http
   ```
3. En otra terminal, iniciar MVC:
   ```powershell
   dotnet run --project .\TourSmart.Web\TourSmart.Web.csproj --launch-profile http
   ```
4. Abrir `http://localhost:5291` y pulsar **Acceder**.
5. Contraseña incorrecta => mensaje genérico, sin iniciar sesión.
6. Credenciales válidas de Administrador => `/panel` con rol Administrador y opción Salir.
7. `POST Salir` => vuelve a inicio; navegar manualmente a `/panel` debe redirigir al login.
8. Credenciales de Cliente => panel de cliente, no el de operaciones.
9. Detener la API y reintentar login => mensaje de servicio no disponible.
10. Revisar navegación y accesibilidad móvil.

## Seguridad y limitaciones
- Para esta fase se utiliza HTTP únicamente en `localhost`, evitando problemas de certificados de desarrollo. **Antes de producción, usar HTTPS en API + web, CookieSecurePolicy.Always, un almacén de sesiones distribuido y persistencia de claves Data Protection.**
- Caché de JWT en memoria: las sesiones se pierden al reiniciar la web; no admite balanceo entre varias instancias. Diseñada solo para pruebas locales.
- Los roles en la cookie no se actualizan automáticamente al cambiar el rol en SQL Server: requieren nuevo inicio de sesión; futuras operaciones administrativas **deben comprobarse nuevamente desde la API usando el JWT**.
- Se conserva la validación de rol y permisos en la API como fuente definitiva de autorización.
- Pendientes: registro visual, recuperación de contraseña, revalidación de sesión, rate limiting, dashboard real, reservas propias del cliente, UI de administración y pruebas automatizadas.
- Nunca publicar capturas del JWT, contraseñas o credenciales personales.

> Los cambios se han verificado en GitHub; el asistente no ejecutó la compilación en la máquina local. Confirmar el resultado de `dotnet build` antes de darlos por finalizados.
