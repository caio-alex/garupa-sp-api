using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Garupa.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ProtectSosTokensAndRetention : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_sos_TokenAcompanhamento",
                table: "sos");

            migrationBuilder.AlterColumn<string>(
                name: "TokenAcompanhamento",
                table: "sos",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(32)",
                oldMaxLength: 32);

            migrationBuilder.AlterColumn<double>(
                name: "Longitude",
                table: "sos",
                type: "double precision",
                nullable: true,
                oldClrType: typeof(double),
                oldType: "double precision");

            migrationBuilder.AlterColumn<double>(
                name: "Latitude",
                table: "sos",
                type: "double precision",
                nullable: true,
                oldClrType: typeof(double),
                oldType: "double precision");

            migrationBuilder.AddColumn<string>(
                name: "TokenHash",
                table: "sos",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_sos_TokenHash",
                table: "sos",
                column: "TokenHash",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            throw new NotSupportedException("A conversao de tokens e a retencao exigem restauracao de backup para retornar ao modelo anterior.");
        }
    }
}
