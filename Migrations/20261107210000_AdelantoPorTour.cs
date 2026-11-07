using AgenciaToursAPI.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgenciaToursAPI.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20261107210000_AdelantoPorTour")]
    public partial class AdelantoPorTour : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PorcentajeAdelanto",
                table: "Tours",
                type: "int",
                nullable: false,
                defaultValue: 30);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Tours_PorcentajeAdelanto",
                table: "Tours",
                sql: "[PorcentajeAdelanto] BETWEEN 0 AND 100");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Tours_PorcentajeAdelanto",
                table: "Tours");

            migrationBuilder.DropColumn(
                name: "PorcentajeAdelanto",
                table: "Tours");
        }
    }
}
