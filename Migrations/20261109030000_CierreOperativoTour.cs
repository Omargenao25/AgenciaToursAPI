using AgenciaToursAPI.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
namespace AgenciaToursAPI.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20261109030000_CierreOperativoTour")]
public partial class CierreOperativoTour : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "CierresOperativosTour",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                TourId = table.Column<int>(type: "int", nullable: false),
                FechaCierreUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                CerradoPor = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                PlazasConfirmadas = table.Column<int>(type: "int", nullable: false),
                PasajerosPresentes = table.Column<int>(type: "int", nullable: false),
                PasajerosAusentes = table.Column<int>(type: "int", nullable: false),
                Incidencias = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_CierresOperativosTour", x => x.Id);
                table.CheckConstraint("CK_CierresOperativosTour_Contadores",
                    "[PlazasConfirmadas] >= 0 AND [PasajerosPresentes] >= 0 AND [PasajerosAusentes] >= 0 AND [PlazasConfirmadas] = [PasajerosPresentes] + [PasajerosAusentes]");
                table.ForeignKey(
                    name: "FK_CierresOperativosTour_Tours_TourId",
                    column: x => x.TourId, principalTable: "Tours",
                    principalColumn: "Id", onDelete: ReferentialAction.NoAction);
            });
        migrationBuilder.CreateIndex(
            name: "IX_CierresOperativosTour_TourId",
            table: "CierresOperativosTour", column: "TourId", unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "CierresOperativosTour");
    }
}
