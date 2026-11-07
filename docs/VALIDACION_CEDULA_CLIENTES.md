# Validación de cédula en TourSmart RD

## Regla implementada
Los formularios MVC de creación y edición y el modelo Cliente de la API requieren que la cédula contenga **exactamente 11 dígitos numéricos**, sin puntos, espacios ni guiones. Se usa `StringLength(11, MinimumLength = 11)` y `RegularExpression(@"^[0-9]{11}$")`. El campo web tiene `maxlength="11"`, `minlength="11"`, `pattern="[0-9]{11}"` e `inputmode="numeric"`.

**Importante:** esto es validación de formato, no comprobación del dígito verificador ni autenticidad oficial del documento. Un número aleatorio de once dígitos aún podría pasar el formato.

## Pruebas
1. Detener API y MVC; `git status`; con árbol limpio `git pull origin feature/toursmart-integrador-ii`.
2. `dotnet build AgenciaToursAPI.sln` (esperar 0 errores).
3. Encender ambos proyectos y abrir `http://localhost:5291/panel/clientes`.
4. Editar un cliente de prueba: ingresar doce dígitos; el input debe limitar a once.
5. Ingresar diez dígitos; debe impedir guardar y mostrar invalidación.
6. Ingresar letras o guiones; debe impedir guardar.
7. Ingresar exactamente once dígitos (con un valor ficticio exclusivo de pruebas) y guardar; debe pasar el formato, sujeto a la validación de duplicados.
8. Verificar que un POST directo a la API con 12 dígitos obtenga 400 por validación del modelo.
9. Comprobar que los clientes históricos de longitud diferente no se modificaron en SQL Server de forma automática.

No se requiere migración de datos ni de esquema; los registros anteriores deben revisarse por separado si no cumplen la nueva regla.
