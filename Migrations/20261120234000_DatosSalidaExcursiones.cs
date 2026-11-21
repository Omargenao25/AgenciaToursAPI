using AgenciaToursAPI.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
namespace AgenciaToursAPI.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20261120234000_DatosSalidaExcursiones")]
public partial class DatosSalidaExcursiones : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(name: "PuntoEncuentro", table: "Tours",
            type: "nvarchar(250)", maxLength: 250, nullable: true);
        migrationBuilder.AddColumn<string>(name: "InstruccionesSalida", table: "Tours",
            type: "nvarchar(1000)", maxLength: 1000, nullable: true);
        migrationBuilder.AddColumn<string>(name: "QueLlevar", table: "Tours",
            type: "nvarchar(500)", maxLength: 500, nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "PuntoEncuentro", table: "Tours");
        migrationBuilder.DropColumn(name: "InstruccionesSalida", table: "Tours");
        migrationBuilder.DropColumn(name: "QueLlevar", table: "Tours");
    }
}
