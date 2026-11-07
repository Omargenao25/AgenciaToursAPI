# TourSmart RD — Fase 2.8.1: identificación internacional

## Diseño implementado
- Identificaciones admitidas: `CedulaDominicana`, `Pasaporte`, `Residencia` y `Otro`.
- Nuevos campos de `Clientes`: `TipoDocumento` (nvarchar(30)), `NumeroDocumento` (nvarchar(40)), `PaisEmisorDocumento` (nvarchar(80)).
- Se mantiene el campo `Cedula` legado, ahora nullable: clientes dominicanos guardan el mismo valor en ambos campos; extranjeros guardan `Cedula = NULL`.
- Migración `20261107060000_DocumentosInternacionalesClientes`: copia cada cédula histórica a `NumeroDocumento`, establece tipo `CedulaDominicana` y país `República Dominicana`, y conserva los datos de reservas y clientes. Establece índice único compuesto para tipo + país + número, y convierte índice de cédula en único filtrado para no nulos.
- Formulario MVC con selección de tipo de documento, país emisor y número. El formato de cédula exige 11 dígitos; otros documentos aceptan 5–40 letras/números/guiones.
- Las validaciones de formato se repiten en la API; el listado permite buscar por documento.
- Duplicados: por tipo, país emisor y número de documento; el correo sigue siendo único.
- Importante: la validación no comprueba autenticidad oficial, dígitos de control, vigencia, nacionalidad ni reglas nacionales específicas de pasaportes.

## Antes de aplicar
1. **Realiza una copia de seguridad de `AgenciaToursDB` desde SQL Server Management Studio.**
2. Detén API y web. Dentro del repositorio, comprueba `git status`; con cambios locales resueltos, ejecuta:
   ```powershell
   git pull origin feature/toursmart-integrador-ii
   dotnet build AgenciaToursAPI.sln
   ```
3. Si compila con 0 errores, aplica migración:
   ```powershell
   dotnet ef database update --project .\AgenciaToursAPI.csproj
   ```
   Ejecuta en el contexto habitual donde se encuentra configurada la conexión a tu base de datos (User Secrets). Si no está instalado dotnet-ef, usar la consola Package Manager con `Update-Database`, eligiendo AgenciaToursAPI como proyecto predeterminado.
4. Verificar con SQL:
   ```sql
   SELECT Id, Nombre, Apellido, TipoDocumento, NumeroDocumento, PaisEmisorDocumento, Cedula
   FROM dbo.Clientes
   ORDER BY Id;
   ```
   Los clientes anteriores deben mantener su documento, sus datos de contacto y sus reservas.
5. Iniciar API 5290 y MVC 5291. Abrir `http://localhost:5291/panel/clientes`.

## Pruebas funcionales
- Editar a Manolo (o un cliente ficticio) con `CedulaDominicana`: exactamente 11 dígitos; 10 o 12 deben rechazarse; tipo cédula restringe país a República Dominicana.
- Registrar un **cliente ficticio diferente** con `Pasaporte`, país `México`, un número ficticio alfanumérico, correo ficticio único; comprobar que se guarda y aparece en el directorio.
- Confirmar que el extranjero muestra número/pasaporte/país y no requiere cédula.
- Intentar duplicar el mismo pasaporte con el mismo tipo y país: debe rechazarse.
- Intentar duplicar el correo con otro número de documento: debe rechazarse.
- Cambiar a `Residencia` / `Otro` y verificar validaciones de longitud/formato.
- Crear una reserva para el cliente extranjero desde `/panel/reservas/crear`; debe poder seleccionarse.
- Asegurar que los registros históricos y sus reservas permanecen intactos; acceder como Cliente debe denegar administración.

## Límites y precauciones
- **No ejecutar `database update` antes de confirmar compilación y respaldo.**
- No se debe hacer rollback automático: la migración `Down` deliberadamente impide pérdida de datos de pasaportes extranjeros.
- Los registros históricos con cédulas de formato erróneo se preservan tal como estaban; deben corregirse manualmente cuando se editen.
- El sistema aún no utiliza códigos ISO para país emisor, ni verifica documentos con fuentes oficiales. Para uso productivo deben evaluarse reglas por país, privacidad y retención de documentos, auditoría, HTTPS y cifrado.
- No hay integración de pagos ni validación de autenticidad documental.
- Este cambio no se compiló ni ejecutó sobre la base de datos local desde el asistente; las pruebas reales se hacen en Visual Studio/SSMS.
