using AgenciaToursAPI.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgenciaToursAPI.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20261113183000_VerificacionCorreoClientes")]
public partial class VerificacionCorreoClientes : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<bool>(
            name: "CorreoConfirmado", table: "Usuarios", type: "bit",
            nullable: false, defaultValue: true);

        migrationBuilder.CreateTable(
            name: "VerificacionesCorreo",
            columns: table => new
            {
                Id = table.Column<long>(type: "bigint", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                UsuarioId = table.Column<int>(type: "int", nullable: false),
                TokenHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                CreadoUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                ExpiraUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                ConsumidoUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_VerificacionesCorreo", x => x.Id);
                table.ForeignKey(
                    name: "FK_VerificacionesCorreo_Usuarios_UsuarioId",
                    column: x => x.UsuarioId, principalTable: "Usuarios",
                    principalColumn: "Id", onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(name: "IX_VerificacionesCorreo_TokenHash",
            table: "VerificacionesCorreo", column: "TokenHash", unique: true);
        migrationBuilder.CreateIndex(name: "IX_VerificacionesCorreo_UsuarioId_CreadoUtc",
            table: "VerificacionesCorreo", columns: new[] { "UsuarioId", "CreadoUtc" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "VerificacionesCorreo");
        migrationBuilder.DropColumn(name: "CorreoConfirmado", table: "Usuarios");
    }
}
