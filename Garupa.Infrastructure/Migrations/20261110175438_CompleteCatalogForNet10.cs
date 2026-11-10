using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Garupa.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CompleteCatalogForNet10 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "mensagens",
                columns: new[] { "Id", "Ativa", "Contexto", "Fonte", "Texto", "TipoDeUso" },
                values: new object[,]
                {
                    { 7, true, "noite", "https://vitoria.es.gov.br/setran/dicas-para-motociclistas", "À noite, roupas claras e resistentes ajudam a manter você visível no trânsito.", null },
                    { 8, true, "domingo", "https://www.gov.br/dnit/pt-br/assuntos/noticias/motociclista-no-transito-a-atencao-precisa-ser-redobrada", "Antes de sair no domingo, confira as condições dos freios, luzes e pneus.", null }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "mensagens",
                keyColumn: "Id",
                keyValue: 7);

            migrationBuilder.DeleteData(
                table: "mensagens",
                keyColumn: "Id",
                keyValue: 8);
        }
    }
}
