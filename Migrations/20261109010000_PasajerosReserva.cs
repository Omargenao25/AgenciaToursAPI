using AgenciaToursAPI.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgenciaToursAPI.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20261109010000_PasajerosReserva")]
public partial class PasajerosReserva : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "PasajerosReserva",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                ReservaId = table.Column<int>(type: "int", nullable: false),
                Orden = table.Column<int>(type: "int", nullable: false),
                NombreCompleto = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_PasajerosReserva", x => x.Id);
                table.CheckConstraint("CK_PasajerosReserva_Orden", "[Orden] BETWEEN 1 AND 100");
                table.ForeignKey(
                    name: "FK_PasajerosReserva_Reservas_ReservaId",
                    column: x => x.ReservaId,
                    principalTable: "Reservas",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });
        migrationBuilder.CreateIndex(
            name: "IX_PasajerosReserva_ReservaId_Orden",
            table: "PasajerosReserva",
            columns: new[] { "ReservaId", "Orden" },
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "PasajerosReserva");
    }
}
