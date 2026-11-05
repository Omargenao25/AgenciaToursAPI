using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using AgenciaToursAPI.Data;

#nullable disable

namespace AgenciaToursAPI.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20261105230000_AgregarRolesUsuario")]
    public partial class AgregarRolesUsuario : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Rol",
                table: "Usuarios",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Cliente");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Usuarios_Rol",
                table: "Usuarios",
                sql: "[Rol] IN (N'Administrador', N'Operador', N'Cliente')");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Usuarios_Rol",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "Rol",
                table: "Usuarios");
        }
    }
}
