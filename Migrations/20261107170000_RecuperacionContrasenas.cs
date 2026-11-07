using AgenciaToursAPI.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgenciaToursAPI.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20261107170000_RecuperacionContrasenas")]
    public partial class RecuperacionContrasenas : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RecuperacionesContrasena",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UsuarioId = table.Column<int>(type: "int", nullable: false),
                    TokenHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ExpiraUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreadoUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ConsumidoUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecuperacionesContrasena", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RecuperacionesContrasena_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RecuperacionesContrasena_TokenHash",
                table: "RecuperacionesContrasena", column: "TokenHash", unique: true);
            migrationBuilder.CreateIndex(
                name: "IX_RecuperacionesContrasena_UsuarioId_CreadoUtc",
                table: "RecuperacionesContrasena",
                columns: new[] { "UsuarioId", "CreadoUtc" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "RecuperacionesContrasena");
        }
    }
}
