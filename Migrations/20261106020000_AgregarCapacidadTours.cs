using AgenciaToursAPI.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgenciaToursAPI.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20261106020000_AgregarCapacidadTours")]
    public partial class AgregarCapacidadTours : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CapacidadMaxima",
                table: "Tours",
                type: "int",
                nullable: false,
                defaultValue: 100);

            // Conserva reservas anteriores incluso si superan los 100 pasajeros.
            migrationBuilder.Sql(@"
                UPDATE t
                SET CapacidadMaxima = r.Reservados
                FROM dbo.Tours t
                INNER JOIN (
                    SELECT TourId, SUM(CantidadPersonas) AS Reservados
                    FROM dbo.Reservas
                    GROUP BY TourId
                ) r ON r.TourId = t.Id
                WHERE r.Reservados > 100;");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Tours_CapacidadMaxima",
                table: "Tours",
                sql: "[CapacidadMaxima] >= 1");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Tours_CapacidadMaxima",
                table: "Tours");
            migrationBuilder.DropColumn(name: "CapacidadMaxima", table: "Tours");
        }
    }
}
