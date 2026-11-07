using AgenciaToursAPI.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgenciaToursAPI.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20261107230000_VencimientoReservasPendientes")]
    public partial class VencimientoReservasPendientes : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "HorasLimiteAdelanto", table: "Tours",
                type: "int", nullable: false, defaultValue: 24);

            // NULL para reservas existentes: no caducan retroactivamente.
            migrationBuilder.AddColumn<DateTime>(
                name: "VencePendienteUtc", table: "Reservas",
                type: "datetime2", nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MotivoCancelacion", table: "Reservas",
                type: "nvarchar(30)", maxLength: 30, nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Tours_HorasLimiteAdelanto",
                table: "Tours",
                sql: "[HorasLimiteAdelanto] BETWEEN 1 AND 168");

            migrationBuilder.CreateIndex(
                name: "IX_Reservas_Estado_VencePendienteUtc",
                table: "Reservas",
                columns: new[] { "Estado", "VencePendienteUtc" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Reservas_Estado_VencePendienteUtc",
                table: "Reservas");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Tours_HorasLimiteAdelanto",
                table: "Tours");

            migrationBuilder.DropColumn(name: "MotivoCancelacion", table: "Reservas");
            migrationBuilder.DropColumn(name: "VencePendienteUtc", table: "Reservas");
            migrationBuilder.DropColumn(name: "HorasLimiteAdelanto", table: "Tours");
        }
    }
}
