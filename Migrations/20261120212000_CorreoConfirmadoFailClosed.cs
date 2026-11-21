using AgenciaToursAPI.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
namespace AgenciaToursAPI.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20261120212000_CorreoConfirmadoFailClosed")]
public partial class CorreoConfirmadoFailClosed : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AlterColumn<bool>(
            name: "CorreoConfirmado", table: "Usuarios", type: "bit",
            nullable: false, defaultValue: false,
            oldClrType: typeof(bool), oldType: "bit", oldDefaultValue: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AlterColumn<bool>(
            name: "CorreoConfirmado", table: "Usuarios", type: "bit",
            nullable: false, defaultValue: true,
            oldClrType: typeof(bool), oldType: "bit", oldDefaultValue: false);
    }
}
