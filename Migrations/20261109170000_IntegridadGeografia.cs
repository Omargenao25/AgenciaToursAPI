using AgenciaToursAPI.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgenciaToursAPI.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20261109170000_IntegridadGeografia")]
public partial class IntegridadGeografia : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Fail closed: nunca eliminar, fusionar ni cambiar registros de forma automática.
        migrationBuilder.Sql(@"
IF EXISTS (
    SELECT 1 FROM dbo.Paises
    GROUP BY UPPER(LTRIM(RTRIM(Nombre))) COLLATE Latin1_General_100_CI_AI
    HAVING COUNT(*) > 1
)
    THROW 51001, 'Paises duplicados: revisar Script-Sql/VerificarGeografiaAntesDeMigrar.sql.', 1;

IF EXISTS (
    SELECT 1 FROM dbo.Destinos
    GROUP BY PaisId, UPPER(LTRIM(RTRIM(Nombre))) COLLATE Latin1_General_100_CI_AI
    HAVING COUNT(*) > 1
)
    THROW 51002, 'Destinos duplicados por pais: revisar Script-Sql/VerificarGeografiaAntesDeMigrar.sql.', 1;

IF EXISTS (SELECT 1 FROM dbo.Paises WHERE LEN(LTRIM(RTRIM(Nombre))) < 2 OR Nombre <> LTRIM(RTRIM(Nombre)))
    THROW 51003, 'Paises con nombres invalidos o espacios sobrantes.', 1;

IF EXISTS (SELECT 1 FROM dbo.Destinos WHERE LEN(LTRIM(RTRIM(Nombre))) < 2 OR Nombre <> LTRIM(RTRIM(Nombre)))
    THROW 51004, 'Destinos con nombres invalidos o espacios sobrantes.', 1;

IF EXISTS (SELECT 1 FROM dbo.Destinos WHERE DuracionDias NOT BETWEEN 0 AND 365 OR DuracionHoras NOT BETWEEN 0 AND 23 OR (DuracionDias = 0 AND DuracionHoras = 0))
    THROW 51005, 'Destinos con duracion invalida: corregir antes de migrar.', 1;

IF EXISTS (
    SELECT 1 FROM dbo.Tours AS t
    LEFT JOIN dbo.Destinos AS d ON d.Id = t.DestinoId
    WHERE d.Id IS NULL OR t.PaisId <> d.PaisId
)
    THROW 51006, 'Tours con destino de un pais distinto: revisar relaciones.', 1;
");

        migrationBuilder.CreateIndex(
            name: "IX_Paises_Nombre",
            table: "Paises",
            column: "Nombre",
            unique: true);

        migrationBuilder.DropIndex(
            name: "IX_Destinos_PaisId",
            table: "Destinos");

        migrationBuilder.CreateIndex(
            name: "IX_Destinos_PaisId_Nombre",
            table: "Destinos",
            columns: new[] { "PaisId", "Nombre" },
            unique: true);

        migrationBuilder.AddCheckConstraint(
            name: "CK_Destinos_DuracionPositiva",
            table: "Destinos",
            sql: "[DuracionDias] BETWEEN 0 AND 365 AND [DuracionHoras] BETWEEN 0 AND 23 AND ([DuracionDias] > 0 OR [DuracionHoras] > 0)");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropCheckConstraint(
            name: "CK_Destinos_DuracionPositiva",
            table: "Destinos");

        migrationBuilder.DropIndex(
            name: "IX_Destinos_PaisId_Nombre",
            table: "Destinos");

        migrationBuilder.CreateIndex(
            name: "IX_Destinos_PaisId",
            table: "Destinos",
            column: "PaisId");

        migrationBuilder.DropIndex(
            name: "IX_Paises_Nombre",
            table: "Paises");
    }
}
