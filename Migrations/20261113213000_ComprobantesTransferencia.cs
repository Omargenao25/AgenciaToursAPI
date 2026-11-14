using AgenciaToursAPI.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
namespace AgenciaToursAPI.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20261113213000_ComprobantesTransferencia")]
public partial class ComprobantesTransferencia : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "ComprobantesTransferencia",
            columns: table => new
            {
                Id = table.Column<long>(type: "bigint", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                ReservaId = table.Column<int>(type: "int", nullable: false),
                ClienteId = table.Column<int>(type: "int", nullable: false),
                MontoReportado = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                Referencia = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                Estado = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                CreadoUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                RevisadoUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                RevisadoPorUsuarioId = table.Column<int>(type: "int", nullable: true),
                MotivoRevision = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                PagoReservaId = table.Column<int>(type: "int", nullable: true),
                ArchivoMime = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                Archivo = table.Column<byte[]>(type: "varbinary(max)", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ComprobantesTransferencia", x => x.Id);
                table.ForeignKey("FK_ComprobantesTransferencia_Reserva", x => x.ReservaId,
                    "Reservas", "Id", onDelete: ReferentialAction.NoAction);
                table.ForeignKey("FK_ComprobantesTransferencia_Cliente", x => x.ClienteId,
                    "Clientes", "Id", onDelete: ReferentialAction.NoAction);
                table.ForeignKey("FK_ComprobantesTransferencia_Pago", x => x.PagoReservaId,
                    "PagosReserva", "Id", onDelete: ReferentialAction.NoAction);
                table.CheckConstraint("CK_Comprobantes_Estado", "[Estado] IN (N'Pendiente', N'Aprobado', N'Rechazado')");
                table.CheckConstraint("CK_Comprobantes_Monto", "[MontoReportado] > 0");
            });
        migrationBuilder.CreateIndex("IX_ComprobantesTransferencia_ClienteId",
            "ComprobantesTransferencia", "ClienteId");
        migrationBuilder.CreateIndex("IX_ComprobantesTransferencia_ReservaId_Estado",
            "ComprobantesTransferencia", new[] {"ReservaId", "Estado"});
        migrationBuilder.CreateIndex("IX_ComprobantesTransferencia_Estado_CreadoUtc",
            "ComprobantesTransferencia", new[] {"Estado", "CreadoUtc"});
        migrationBuilder.CreateIndex("IX_ComprobantesTransferencia_PagoReservaId",
            "ComprobantesTransferencia", "PagoReservaId", unique: true,
            filter: "[PagoReservaId] IS NOT NULL");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
        => migrationBuilder.DropTable("ComprobantesTransferencia");
}
