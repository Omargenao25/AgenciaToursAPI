using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgenciaToursAPI.Migrations
{
    /// <inheritdoc />
    public partial class AgregarTelefonoUnicoGuia : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Guias_Telefono",
                table: "Guias",
                column: "Telefono",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Guias_Telefono",
                table: "Guias");
        }
    }
}
