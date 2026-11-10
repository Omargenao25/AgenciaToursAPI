using AgenciaToursAPI.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgenciaToursAPI.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20261109213000_ImagenesToursYDestinos")]
public partial class ImagenesToursYDestinos : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "ImagenUrl",
            table: "Tours",
            type: "nvarchar(500)",
            maxLength: 500,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "ImagenUrl",
            table: "Destinos",
            type: "nvarchar(500)",
            maxLength: 500,
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "ImagenUrl", table: "Tours");
        migrationBuilder.DropColumn(name: "ImagenUrl", table: "Destinos");
    }
}
