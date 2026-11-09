using AgenciaToursAPI.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgenciaToursAPI.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20261109020000_CheckInPasajeros")]
public partial class CheckInPasajeros : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<DateTime>(
            name: "LlegadaUtc", table: "PasajerosReserva",
            type: "datetime2", nullable: true);
        migrationBuilder.AddColumn<string>(
            name: "RegistradoPor", table: "PasajerosReserva",
            type: "nvarchar(100)", maxLength: 100, nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "LlegadaUtc", table: "PasajerosReserva");
        migrationBuilder.DropColumn(name: "RegistradoPor", table: "PasajerosReserva");
    }
}
