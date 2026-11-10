using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Garupa.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "sos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EventoId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    UsuarioId = table.Column<int>(type: "integer", nullable: false),
                    Tipo = table.Column<int>(type: "integer", nullable: false),
                    Origem = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Latitude = table.Column<double>(type: "double precision", nullable: false),
                    Longitude = table.Column<double>(type: "double precision", nullable: false),
                    Estado = table.Column<int>(type: "integer", nullable: false),
                    TokenAcompanhamento = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    OcorridoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TerminadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_sos_usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_sos_EventoId",
                table: "sos",
                column: "EventoId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_sos_TokenAcompanhamento",
                table: "sos",
                column: "TokenAcompanhamento",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_sos_UsuarioId",
                table: "sos",
                column: "UsuarioId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "sos");
        }
    }
}
