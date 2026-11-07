using AgenciaToursAPI.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgenciaToursAPI.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20261107060000_DocumentosInternacionalesClientes")]
    public partial class DocumentosInternacionalesClientes : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TipoDocumento", table: "Clientes", type: "nvarchar(30)",
                maxLength: 30, nullable: false, defaultValue: "CedulaDominicana");
            migrationBuilder.AddColumn<string>(
                name: "NumeroDocumento", table: "Clientes", type: "nvarchar(40)",
                maxLength: 40, nullable: false, defaultValue: "");
            migrationBuilder.AddColumn<string>(
                name: "PaisEmisorDocumento", table: "Clientes", type: "nvarchar(80)",
                maxLength: 80, nullable: false, defaultValue: "República Dominicana");

            // Mantener todos los números de cédula anteriores sin reescribir ni perder datos.
            migrationBuilder.Sql(@"
                UPDATE dbo.Clientes
                SET NumeroDocumento = Cedula,
                    TipoDocumento = N'CedulaDominicana',
                    PaisEmisorDocumento = N'República Dominicana'
                WHERE NumeroDocumento = N'';");

            migrationBuilder.DropIndex(name: "IX_Clientes_Cedula", table: "Clientes");
            migrationBuilder.AlterColumn<string>(
                name: "Cedula", table: "Clientes", type: "nvarchar(20)",
                maxLength: 20, nullable: true, oldClrType: typeof(string),
                oldType: "nvarchar(20)", oldMaxLength: 20);
            migrationBuilder.CreateIndex(
                name: "IX_Clientes_Cedula", table: "Clientes",
                column: "Cedula", unique: true, filter: "[Cedula] IS NOT NULL");
            migrationBuilder.CreateIndex(
                name: "IX_Clientes_TipoDocumento_PaisEmisorDocumento_NumeroDocumento",
                table: "Clientes",
                columns: new[] { "TipoDocumento", "PaisEmisorDocumento", "NumeroDocumento" },
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // No se permite reversión automática: pasaportes extranjeros no caben
            // en la antigua columna Cedula NOT NULL sin perder información.
            throw new NotSupportedException(
                "La reversión requiere un plan de migración manual y respaldo de clientes internacionales.");
        }
    }
}
