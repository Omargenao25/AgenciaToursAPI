using AgenciaToursAPI.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgenciaToursAPI.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20261117210000_RecordatoriosExcursiones")]
public partial class RecordatoriosExcursiones : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<DateTime>(
            name: "RecordatorioIntentadoUtc", table: "Reservas",
            type: "datetime2", nullable: true);
        migrationBuilder.AddColumn<DateTime>(
            name: "RecordatorioEnviadoUtc", table: "Reservas",
            type: "datetime2", nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "RecordatorioIntentadoUtc", table: "Reservas");
        migrationBuilder.DropColumn(name: "RecordatorioEnviadoUtc", table: "Reservas");
    }
}
