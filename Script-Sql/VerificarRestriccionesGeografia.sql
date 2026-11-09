-- TourSmart RD - comprobacion de restricciones instaladas (SOLO LECTURA).
-- En SSMS: conectar al SQL Server del proyecto y seleccionar la base usada por la API.
-- Este script NO crea, elimina ni modifica registros.
SET NOCOUNT ON;

SELECT DB_NAME() AS BaseConsultada, SYSDATETIME() AS FechaConsulta;

SELECT
    'Migracion IntegridadGeografia' AS Revision,
    CASE WHEN EXISTS (
        SELECT 1 FROM dbo.__EFMigrationsHistory
        WHERE MigrationId = N'20261109170000_IntegridadGeografia'
    ) THEN 'OK' ELSE 'FALTA' END AS Estado

UNION ALL

SELECT 'Indice unico de paises (Nombre)',
       CASE WHEN EXISTS (
           SELECT 1
           FROM sys.indexes i
           WHERE i.object_id = OBJECT_ID(N'dbo.Paises')
             AND i.name = N'IX_Paises_Nombre'
             AND i.is_unique = 1 AND i.is_disabled = 0
             AND (SELECT STRING_AGG(c.name, N',') WITHIN GROUP (ORDER BY ic.key_ordinal)
                  FROM sys.index_columns ic
                  JOIN sys.columns c
                    ON c.object_id = ic.object_id AND c.column_id = ic.column_id
                  WHERE ic.object_id = i.object_id
                    AND ic.index_id = i.index_id
                    AND ic.key_ordinal > 0) = N'Nombre'
       ) THEN 'OK' ELSE 'FALTA' END

UNION ALL

SELECT 'Indice unico de destinos (PaisId, Nombre)',
       CASE WHEN EXISTS (
           SELECT 1
           FROM sys.indexes i
           WHERE i.object_id = OBJECT_ID(N'dbo.Destinos')
             AND i.name = N'IX_Destinos_PaisId_Nombre'
             AND i.is_unique = 1 AND i.is_disabled = 0
             AND (SELECT STRING_AGG(c.name, N',') WITHIN GROUP (ORDER BY ic.key_ordinal)
                  FROM sys.index_columns ic
                  JOIN sys.columns c
                    ON c.object_id = ic.object_id AND c.column_id = ic.column_id
                  WHERE ic.object_id = i.object_id
                    AND ic.index_id = i.index_id
                    AND ic.key_ordinal > 0) = N'PaisId,Nombre'
       ) THEN 'OK' ELSE 'FALTA' END

UNION ALL

SELECT 'Restriccion de duracion de destinos',
       CASE WHEN EXISTS (
           SELECT 1 FROM sys.check_constraints
           WHERE parent_object_id = OBJECT_ID(N'dbo.Destinos')
             AND name = N'CK_Destinos_DuracionPositiva'
             AND is_disabled = 0 AND is_not_trusted = 0
       ) THEN 'OK' ELSE 'FALTA' END;

-- Confirmar que las tablas todavia conservan los registros.
SELECT
    (SELECT COUNT(*) FROM dbo.Paises) AS TotalPaises,
    (SELECT COUNT(*) FROM dbo.Destinos) AS TotalDestinos,
    (SELECT COUNT(*) FROM dbo.Tours) AS TotalTours;

-- Esperado: cuatro filas con 'OK'. Si aparece 'FALTA', no migrar ni
-- intentar arreglar automaticamente: compartir el resultado y revisar la base.
