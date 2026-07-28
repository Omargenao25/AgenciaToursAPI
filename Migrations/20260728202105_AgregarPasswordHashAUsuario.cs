using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgenciaToursAPI.Migrations
{
    /// <inheritdoc />
    public partial class AgregarPasswordHashAUsuario : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Contrasena",
                table: "Usuarios",
                newName: "PasswordHash");

            migrationBuilder.AlterColumn<string>(
                name: "Correo",
                table: "Usuarios",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_Correo",
                table: "Usuarios",
                column: "Correo",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Usuarios_Correo",
                table: "Usuarios");

            migrationBuilder.RenameColumn(
                name: "PasswordHash",
                table: "Usuarios",
                newName: "Contrasena");

            migrationBuilder.AlterColumn<string>(
                name: "Correo",
                table: "Usuarios",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(150)",
                oldMaxLength: 150);
        }
    }
}
