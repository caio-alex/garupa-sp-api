using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Garupa.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AdicionarSenhaUsuario : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SenhaHash",
                table: "usuarios",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_usuarios_Telefone",
                table: "usuarios",
                column: "Telefone",
                unique: true,
                filter: "\"SenhaHash\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_usuarios_Telefone",
                table: "usuarios");

            migrationBuilder.DropColumn(
                name: "SenhaHash",
                table: "usuarios");
        }
    }
}
