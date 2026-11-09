# TourSmart RD — Integridad de Países y Destinos (fase geografía)

## Objetivo
Evitar duplicados incluso bajo solicitudes simultáneas y bloquear destinos con duración inválida, sin borrar ni fusionar datos preexistentes.

## Preparación: solo en SQL Server de PRUEBAS
1. Hacer respaldo verificable de la base de pruebas.
2. Ejecutar el script `Script-Sql/VerificarGeografiaAntesDeMigrar.sql` en SSMS o Azure Data Studio. Revisar las cinco consultas de incidencias. Deben devolver **cero filas**. La primera consulta identifica la base actual.
3. Si aparecen duplicados, nombres con espacios, destinos inválidos o relaciones incorrectas, **no ejecutar la migración**. Corregir manualmente tras revisar referencias de los tours y su historial.
4. La validación compara nombres ignorando mayúsculas y acentos. Los índices únicos siguen la intercalación (collation) de la base, por lo que si se requiere unicidad totalmente uniforme entre instalaciones hay que estandarizarla más adelante.

## Aplicar migración solo tras comprobar el respaldo y las incidencias
En PowerShell desde la raíz:
```powershell
git pull origin feature/toursmart-integrador-ii
dotnet restore AgenciaToursAPI.sln
dotnet build AgenciaToursAPI.sln
dotnet ef database update --project .\AgenciaToursAPI.csproj
```
Si el último comando no existe, instalar o actualizar `dotnet-ef` en el equipo. Verificar que la cadena de conexión apunte a la base de PRUEBAS correcta. **No ejecutar la migración sobre producción sin un procedimiento aprobado y un respaldo probado.**

## Nuevas garantías
- Índice único `IX_Paises_Nombre`.
- Índice único `IX_Destinos_PaisId_Nombre`: permite un destino con el mismo nombre en países diferentes, pero no duplicados dentro del mismo.
- Restricción `CK_Destinos_DuracionPositiva`: duración positiva y dentro de rangos.
- La migración detiene el proceso con un error explicativo ante registros incompatibles y no altera automáticamente los datos anteriores.
- La API devuelve HTTP 409 en conflictos de índices o relaciones.
- Se conserva la restricción de no cambiar duración ni país de destinos con tours asociados.

## Pruebas propuestas
- Crear un país de prueba y repetir el mismo nombre: 409.
- Crear un destino y repetirlo en el mismo país: 409.
- Crear un destino de igual nombre en otro país: permitido.
- Intentar eliminar país con destinos o destino con tours: 409.
- Intentar cambiar la duración de un destino vinculado a tours: 409.
- Probar nuevamente creación y edición de tours, reservas y navegación pública para detectar regresiones.

Ningún despliegue o migración se ejecuta automáticamente desde GitHub Actions. CI compila; no inicia SQL Server ni prueba estos flujos con datos reales.
