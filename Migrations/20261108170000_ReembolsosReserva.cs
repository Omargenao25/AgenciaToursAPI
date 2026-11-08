using AgenciaToursAPI.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgenciaToursAPI.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20261108170000_ReembolsosReserva")]
    public partial class ReembolsosReserva : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ReembolsosReserva",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReservaId = table.Column<int>(type: "int", nullable: false),
                    MetodoPagoId = table.Column<int>(type: "int", nullable: false),
                    Monto = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    FechaRegistroUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Referencia = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Motivo = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Observacion = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    RegistradoPorUsuarioId = table.Column<int>(type: "int", nullable: false),
                    RegistradoPor = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ClaveOperacion = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReembolsosReserva", x => x.Id);
                    table.CheckConstraint("CK_ReembolsosReserva_Monto", "[Monto] > 0");
                    table.ForeignKey(
                        name: "FK_ReembolsosReserva_Reservas_ReservaId",
                        column: x => x.ReservaId,
                        principalTable: "Reservas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.NoAction);
                    table.ForeignKey(
                        name: "FK_ReembolsosReserva_MetodosPago_MetodoPagoId",
                        column: x => x.MetodoPagoId,
                        principalTable: "MetodosPago",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.NoAction);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ReembolsosReserva_ClaveOperacion",
                table: "ReembolsosReserva",
                column: "ClaveOperacion",
                unique: true);
            migrationBuilder.CreateIndex(
                name: "IX_ReembolsosReserva_ReservaId",
                table: "ReembolsosReserva",
                column: "ReservaId");
            migrationBuilder.CreateIndex(
                name: "IX_ReembolsosReserva_MetodoPagoId",
                table: "ReembolsosReserva",
                column: "MetodoPagoId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "ReembolsosReserva");
        }
    }
}
