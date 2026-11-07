using AgenciaToursAPI.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgenciaToursAPI.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20261107140000_VersionSesionUsuarios")]
    public partial class VersionSesionUsuarios : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "VersionSesion", table: "Usuarios",
                type: "nvarchar(32)", maxLength: 32,
                nullable: false, defaultValue: "");

            // Los usuarios existentes reciben una versión aleatoria persistente.
            // Los JWT previos no incorporan este claim y dejarán de autorizarse.
            migrationBuilder.Sql(@"
                UPDATE dbo.Usuarios
                SET VersionSesion = LOWER(REPLACE(CONVERT(nvarchar(36), NEWID()), N'-', N''))
                WHERE VersionSesion = N'';");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "VersionSesion", table: "Usuarios");
        }
    }
}
