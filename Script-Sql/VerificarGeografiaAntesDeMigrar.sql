-- TourSmart RD: auditoría SOLO LECTURA antes de crear índices únicos.
-- Ejecutar en la base de PRUEBAS correcta; no elimina ni modifica registros.
SET NOCOUNT ON;

SELECT DB_NAME() AS BaseActual, SYSDATETIME() AS FechaRevision;

-- Coincidencias ignorando diferencias de mayúsculas y acentos.
SELECT UPPER(LTRIM(RTRIM(Nombre))) COLLATE Latin1_General_100_CI_AI AS NombreNormalizado,
       COUNT(*) AS Registros,
       STRING_AGG(CONVERT(varchar(max), Id), ', ') AS Ids
FROM dbo.Paises
GROUP BY UPPER(LTRIM(RTRIM(Nombre))) COLLATE Latin1_General_100_CI_AI
HAVING COUNT(*) > 1;

SELECT PaisId,
       UPPER(LTRIM(RTRIM(Nombre))) COLLATE Latin1_General_100_CI_AI AS NombreNormalizado,
       COUNT(*) AS Registros,
       STRING_AGG(CONVERT(varchar(max), Id), ', ') AS Ids
FROM dbo.Destinos
GROUP BY PaisId, UPPER(LTRIM(RTRIM(Nombre))) COLLATE Latin1_General_100_CI_AI
HAVING COUNT(*) > 1;

-- Nombres inválidos (blancos o con espacios sobrantes).
SELECT 'Pais' AS Tipo, Id, Nombre FROM dbo.Paises
WHERE LEN(LTRIM(RTRIM(Nombre))) < 2 OR Nombre <> LTRIM(RTRIM(Nombre))
UNION ALL
SELECT 'Destino', Id, Nombre FROM dbo.Destinos
WHERE LEN(LTRIM(RTRIM(Nombre))) < 2 OR Nombre <> LTRIM(RTRIM(Nombre));

-- Comprobar si algún tour referencia un destino perteneciente a otro país.
SELECT t.Id AS TourId, t.Nombre AS Tour, t.PaisId AS PaisTour,
       d.PaisId AS PaisDestino, t.DestinoId
FROM dbo.Tours t
LEFT JOIN dbo.Destinos d ON d.Id = t.DestinoId
WHERE d.Id IS NULL OR t.PaisId <> d.PaisId;

-- Comprobar coherencia entre duración del tour y su destino.
SELECT t.Id AS TourId, t.Nombre AS Tour,
       t.DuracionDias AS DiasTour, t.DuracionHoras AS HorasTour,
       d.DuracionDias AS DiasDestino, d.DuracionHoras AS HorasDestino
FROM dbo.Tours t
JOIN dbo.Destinos d ON d.Id = t.DestinoId
WHERE t.DuracionDias <> d.DuracionDias OR t.DuracionHoras <> d.DuracionHoras;

-- Todas las consultas deben devolver CERO filas de incidencias.
-- Si hay filas, hacer respaldo, investigar referencias y corregir manualmente antes de migrar.
