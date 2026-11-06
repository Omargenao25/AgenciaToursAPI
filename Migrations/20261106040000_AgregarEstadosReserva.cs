using AgenciaToursAPI.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgenciaToursAPI.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20261106040000_AgregarEstadosReserva")]
    public partial class AgregarEstadosReserva : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Las reservas históricas ya tenían cupos ocupados, por eso se consideran confirmadas.
            migrationBuilder.AddColumn<string>(
                name: "Estado", table: "Reservas", type: "nvarchar(20)",
                maxLength: 20, nullable: false, defaultValue: "Confirmada");

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaCambioEstado", table: "Reservas",
                type: "datetime2", nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Reservas_Estado", table: "Reservas",
                sql: "[Estado] IN (N'Pendiente', N'Confirmada', N'Cancelada')");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(name: "CK_Reservas_Estado", table: "Reservas");
            migrationBuilder.DropColumn(name: "FechaCambioEstado", table: "Reservas");
            migrationBuilder.DropColumn(name: "Estado", table: "Reservas");
        }
    }
}
