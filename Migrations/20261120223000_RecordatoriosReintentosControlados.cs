using AgenciaToursAPI.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
namespace AgenciaToursAPI.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20261120223000_RecordatoriosReintentosControlados")]
public partial class RecordatoriosReintentosControlados : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "RecordatorioEstado", table: "Reservas",
            type: "nvarchar(20)", maxLength: 20, nullable: false,
            defaultValue: "Pendiente");
        migrationBuilder.AddColumn<int>(
            name: "RecordatorioIntentos", table: "Reservas",
            type: "int", nullable: false, defaultValue: 0);
        migrationBuilder.AddColumn<DateTime>(
            name: "RecordatorioProximoIntentoUtc", table: "Reservas",
            type: "datetime2", nullable: true);
        migrationBuilder.AddColumn<DateTime>(
            name: "RecordatorioLeaseHastaUtc", table: "Reservas",
            type: "datetime2", nullable: true);
        migrationBuilder.AddColumn<string>(
            name: "RecordatorioLeaseToken", table: "Reservas",
            type: "nvarchar(32)", maxLength: 32, nullable: true);

        // Un envío heredado confirmado por SMTP no vuelve a despacharse.
        migrationBuilder.Sql("""
            UPDATE dbo.Reservas
            SET RecordatorioEstado = N'Enviado', RecordatorioIntentos = 1
            WHERE RecordatorioEnviadoUtc IS NOT NULL;
            """);
        // Un intento heredado sin respuesta confirmada se deja en revisión:
        // pudo haberse enviado antes de una interrupción del proceso.
        migrationBuilder.Sql("""
            UPDATE dbo.Reservas
            SET RecordatorioEstado = N'Revisar', RecordatorioIntentos = 1
            WHERE RecordatorioIntentadoUtc IS NOT NULL AND RecordatorioEnviadoUtc IS NULL;
            """);

        migrationBuilder.CreateIndex(
            name: "IX_Reservas_RecordatorioEstado_RecordatorioProximoIntentoUtc",
            table: "Reservas", columns: new[] { "RecordatorioEstado", "RecordatorioProximoIntentoUtc" });
        migrationBuilder.AddCheckConstraint(
            name: "CK_Reservas_RecordatorioEstado", table: "Reservas",
            sql: "[RecordatorioEstado] IN (N'Pendiente', N'Enviando', N'Fallido', N'Enviado', N'Revisar', N'Agotado')");
        migrationBuilder.AddCheckConstraint(
            name: "CK_Reservas_RecordatorioIntentos", table: "Reservas",
            sql: "[RecordatorioIntentos] BETWEEN 0 AND 3");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropCheckConstraint("CK_Reservas_RecordatorioEstado", "Reservas");
        migrationBuilder.DropCheckConstraint("CK_Reservas_RecordatorioIntentos", "Reservas");
        migrationBuilder.DropIndex(
            "IX_Reservas_RecordatorioEstado_RecordatorioProximoIntentoUtc", "Reservas");
        migrationBuilder.DropColumn("RecordatorioEstado", "Reservas");
        migrationBuilder.DropColumn("RecordatorioIntentos", "Reservas");
        migrationBuilder.DropColumn("RecordatorioProximoIntentoUtc", "Reservas");
        migrationBuilder.DropColumn("RecordatorioLeaseHastaUtc", "Reservas");
        migrationBuilder.DropColumn("RecordatorioLeaseToken", "Reservas");
    }
}
