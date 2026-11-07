using AgenciaToursAPI.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgenciaToursAPI.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20261107100000_VincularUsuariosClientes")]
    public partial class VincularUsuariosClientes : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ClienteId", table: "Usuarios",
                type: "int", nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_ClienteId", table: "Usuarios",
                column: "ClienteId", unique: true,
                filter: "[ClienteId] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_Usuarios_Clientes_ClienteId",
                table: "Usuarios", column: "ClienteId",
                principalTable: "Clientes", principalColumn: "Id",
                onDelete: ReferentialAction.NoAction);

            // IMPORTANTE: no se enlazan cuentas por coincidencia de correo.
            // Los usuarios antiguos necesitan una verificación de titularidad separada.
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Usuarios_Clientes_ClienteId", table: "Usuarios");
            migrationBuilder.DropIndex(
                name: "IX_Usuarios_ClienteId", table: "Usuarios");
            migrationBuilder.DropColumn(name: "ClienteId", table: "Usuarios");
        }
    }
}
