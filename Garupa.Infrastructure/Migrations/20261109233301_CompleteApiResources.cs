using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Garupa.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CompleteApiResources : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "consentimentos",
                columns: table => new
                {
                    UsuarioId = table.Column<int>(type: "integer", nullable: false),
                    LocalizacaoEmUso = table.Column<bool>(type: "boolean", nullable: false),
                    LocalizacaoEmSegundoPlano = table.Column<bool>(type: "boolean", nullable: false),
                    FichaMedica = table.Column<bool>(type: "boolean", nullable: false),
                    HistoricoDeLocais = table.Column<bool>(type: "boolean", nullable: false),
                    PerfilDeUso = table.Column<bool>(type: "boolean", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_consentimentos", x => x.UsuarioId);
                    table.ForeignKey(
                        name: "FK_consentimentos_usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "distritos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false),
                    Nome = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_distritos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "mensagens",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false),
                    Texto = table.Column<string>(type: "character varying(140)", maxLength: 140, nullable: false),
                    Contexto = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    TipoDeUso = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    Fonte = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Ativa = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_mensagens", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "perfis_uso",
                columns: table => new
                {
                    UsuarioId = table.Column<int>(type: "integer", nullable: false),
                    TiposDeUsoJson = table.Column<string>(type: "text", nullable: false),
                    HorariosHabituaisJson = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_perfis_uso", x => x.UsuarioId);
                    table.ForeignKey(
                        name: "FK_perfis_uso_usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "relatos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EventoId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    UsuarioId = table.Column<int>(type: "integer", nullable: false),
                    Tipo = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Latitude = table.Column<double>(type: "double precision", nullable: false),
                    Longitude = table.Column<double>(type: "double precision", nullable: false),
                    Descricao = table.Column<string>(type: "character varying(280)", maxLength: 280, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_relatos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_relatos_usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "vestiveis",
                columns: table => new
                {
                    UsuarioId = table.Column<int>(type: "integer", nullable: false),
                    DispositivoId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_vestiveis", x => x.UsuarioId);
                    table.UniqueConstraint("AK_vestiveis_UsuarioId_DispositivoId", x => new { x.UsuarioId, x.DispositivoId });
                    table.ForeignKey(
                        name: "FK_vestiveis_usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "celulas_risco",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CelulaId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CentroLat = table.Column<double>(type: "double precision", nullable: false),
                    CentroLon = table.Column<double>(type: "double precision", nullable: false),
                    FaixaHoraria = table.Column<int>(type: "integer", nullable: false),
                    TipoDia = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Chuva = table.Column<bool>(type: "boolean", nullable: false),
                    Score = table.Column<double>(type: "double precision", nullable: false),
                    Percentil = table.Column<int>(type: "integer", nullable: false),
                    ViaReferencia = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    DistritoId = table.Column<int>(type: "integer", nullable: false),
                    VersaoModelo = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_celulas_risco", x => x.Id);
                    table.ForeignKey(
                        name: "FK_celulas_risco_distritos_DistritoId",
                        column: x => x.DistritoId,
                        principalTable: "distritos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "distritos_frequentes",
                columns: table => new
                {
                    UsuarioId = table.Column<int>(type: "integer", nullable: false),
                    DistritoId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_distritos_frequentes", x => new { x.UsuarioId, x.DistritoId });
                    table.ForeignKey(
                        name: "FK_distritos_frequentes_distritos_DistritoId",
                        column: x => x.DistritoId,
                        principalTable: "distritos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_distritos_frequentes_perfis_uso_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "perfis_uso",
                        principalColumn: "UsuarioId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "alertas_vestivel",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UsuarioId = table.Column<int>(type: "integer", nullable: false),
                    DispositivoId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Tipo = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    OcorridoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ConsumidoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_alertas_vestivel", x => x.Id);
                    table.ForeignKey(
                        name: "FK_alertas_vestivel_vestiveis_UsuarioId_DispositivoId",
                        columns: x => new { x.UsuarioId, x.DispositivoId },
                        principalTable: "vestiveis",
                        principalColumns: new[] { "UsuarioId", "DispositivoId" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "eventos_vestivel",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UsuarioId = table.Column<int>(type: "integer", nullable: false),
                    DispositivoId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Tipo = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    OcorridoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ConsumidoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_eventos_vestivel", x => x.Id);
                    table.ForeignKey(
                        name: "FK_eventos_vestivel_vestiveis_UsuarioId_DispositivoId",
                        columns: x => new { x.UsuarioId, x.DispositivoId },
                        principalTable: "vestiveis",
                        principalColumns: new[] { "UsuarioId", "DispositivoId" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "distritos",
                columns: new[] { "Id", "Nome" },
                values: new object[,]
                {
                    { 355030801, "Água Rasa" },
                    { 355030802, "Alto de Pinheiros" },
                    { 355030803, "Anhanguera" },
                    { 355030804, "Aricanduva" },
                    { 355030805, "Artur Alvim" },
                    { 355030806, "Barra Funda" },
                    { 355030807, "Bela Vista" },
                    { 355030808, "Belém" },
                    { 355030809, "Bom Retiro" },
                    { 355030810, "Brás" },
                    { 355030811, "Brasilândia" },
                    { 355030812, "Butantã" },
                    { 355030813, "Cachoeirinha" },
                    { 355030814, "Cambuci" },
                    { 355030815, "Campo Belo" },
                    { 355030816, "Campo Grande" },
                    { 355030817, "Campo Limpo" },
                    { 355030818, "Cangaiba" },
                    { 355030819, "Capão Redondo" },
                    { 355030820, "Carrão" },
                    { 355030821, "Casa Verde" },
                    { 355030822, "Cidade Ademar" },
                    { 355030823, "Cidade Dutra" },
                    { 355030824, "Cidade Lider" },
                    { 355030825, "Cidade Tiradentes" },
                    { 355030826, "Consolação" },
                    { 355030827, "Cursino" },
                    { 355030828, "Ermelino Matarazzo" },
                    { 355030829, "Freguesia do Ó" },
                    { 355030830, "Grajaú" },
                    { 355030831, "Guaianases" },
                    { 355030832, "Moema" },
                    { 355030833, "Iguatemi" },
                    { 355030834, "Ipiranga" },
                    { 355030835, "Itaim Bibi" },
                    { 355030836, "Itaim Paulista" },
                    { 355030837, "Itaquera" },
                    { 355030838, "Jabaquara" },
                    { 355030839, "Jaçanã" },
                    { 355030840, "Jaguara" },
                    { 355030841, "Jaguaré" },
                    { 355030842, "Jaraguá" },
                    { 355030843, "Jardim Ângela" },
                    { 355030844, "Jardim Helena" },
                    { 355030845, "Jardim Paulista" },
                    { 355030846, "Jardim São Luís" },
                    { 355030847, "José Bonifácio" },
                    { 355030848, "Lapa" },
                    { 355030849, "Liberdade" },
                    { 355030850, "Limão" },
                    { 355030851, "Mandaqui" },
                    { 355030852, "Marsilac" },
                    { 355030853, "Mooca" },
                    { 355030854, "Morumbi" },
                    { 355030855, "Parelheiros" },
                    { 355030856, "Pari" },
                    { 355030857, "Parque do Carmo" },
                    { 355030858, "Pedreira" },
                    { 355030859, "Penha" },
                    { 355030860, "Perdizes" },
                    { 355030861, "Perus" },
                    { 355030862, "Pinheiros" },
                    { 355030863, "Pirituba" },
                    { 355030864, "Ponte Rasa" },
                    { 355030865, "Raposo Tavares" },
                    { 355030866, "República" },
                    { 355030867, "Rio Pequeno" },
                    { 355030868, "Sacomã" },
                    { 355030869, "Santa Cecília" },
                    { 355030870, "Santana" },
                    { 355030871, "Santo Amaro" },
                    { 355030872, "São Lucas" },
                    { 355030873, "São Mateus" },
                    { 355030874, "São Miguel" },
                    { 355030875, "São Rafael" },
                    { 355030876, "Sapopemba" },
                    { 355030877, "Saúde" },
                    { 355030878, "Sé" },
                    { 355030879, "Socorro" },
                    { 355030880, "Tatuapé" },
                    { 355030881, "Tremembé" },
                    { 355030882, "Tucuruvi" },
                    { 355030883, "Vila Andrade" },
                    { 355030884, "Vila Curuçá" },
                    { 355030885, "Vila Formosa" },
                    { 355030886, "Vila Guilherme" },
                    { 355030887, "Vila Jacuí" },
                    { 355030888, "Vila Leopoldina" },
                    { 355030889, "Vila Maria" },
                    { 355030890, "Vila Mariana" },
                    { 355030891, "Vila Matilde" },
                    { 355030892, "Vila Medeiros" },
                    { 355030893, "Vila Prudente" },
                    { 355030894, "Vila Sônia" },
                    { 355030895, "São Domingos" },
                    { 355030896, "Lajeado" }
                });

            migrationBuilder.InsertData(
                table: "mensagens",
                columns: new[] { "Id", "Ativa", "Contexto", "Fonte", "Texto", "TipoDeUso" },
                values: new object[,]
                {
                    { 1, true, "geral", "https://www.gov.br/dnit/pt-br/assuntos/noticias/motociclista-no-transito-a-atencao-precisa-ser-redobrada", "Mantenha o capacete afivelado e a viseira em boas condições.", null },
                    { 2, true, "chuva", "https://www.gov.br/dnit/pt-br/assuntos/noticias/motociclista-no-transito-a-atencao-precisa-ser-redobrada", "Com chuva, diminua a velocidade: o piso molhado reduz a aderência.", null },
                    { 3, true, "geral", "https://www.gov.br/dnit/pt-br/assuntos/noticias/motociclista-no-transito-a-atencao-precisa-ser-redobrada", "Use luvas, roupa de proteção e calçado fechado ao pilotar.", "trabalho_moto" },
                    { 4, true, "chuva", "https://www.gov.br/dnit/pt-br/assuntos/noticias/motociclista-no-transito-a-atencao-precisa-ser-redobrada", "Na entrega com chuva, reduza a velocidade e preserve sua segurança.", "entregador_app" },
                    { 5, true, "ponto_critico", "https://www.gov.br/dnit/pt-br/assuntos/noticias/motociclista-no-transito-a-atencao-precisa-ser-redobrada", "Mantenha atenção às condições da via e espaço para reagir.", null },
                    { 6, true, "domingo", "https://www.gov.br/dnit/pt-br/assuntos/noticias/motociclista-no-transito-a-atencao-precisa-ser-redobrada", "No passeio, use capacete afivelado e equipamento de proteção.", "lazer" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_alertas_vestivel_DispositivoId_ConsumidoEm_OcorridoEm",
                table: "alertas_vestivel",
                columns: new[] { "DispositivoId", "ConsumidoEm", "OcorridoEm" });

            migrationBuilder.CreateIndex(
                name: "IX_alertas_vestivel_UsuarioId_DispositivoId",
                table: "alertas_vestivel",
                columns: new[] { "UsuarioId", "DispositivoId" });

            migrationBuilder.CreateIndex(
                name: "IX_celulas_risco_CelulaId_FaixaHoraria_TipoDia_Chuva",
                table: "celulas_risco",
                columns: new[] { "CelulaId", "FaixaHoraria", "TipoDia", "Chuva" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_celulas_risco_DistritoId_FaixaHoraria_TipoDia_Chuva_Percent~",
                table: "celulas_risco",
                columns: new[] { "DistritoId", "FaixaHoraria", "TipoDia", "Chuva", "Percentil" });

            migrationBuilder.CreateIndex(
                name: "IX_distritos_frequentes_DistritoId",
                table: "distritos_frequentes",
                column: "DistritoId");

            migrationBuilder.CreateIndex(
                name: "IX_eventos_vestivel_UsuarioId_ConsumidoEm",
                table: "eventos_vestivel",
                columns: new[] { "UsuarioId", "ConsumidoEm" });

            migrationBuilder.CreateIndex(
                name: "IX_eventos_vestivel_UsuarioId_DispositivoId",
                table: "eventos_vestivel",
                columns: new[] { "UsuarioId", "DispositivoId" });

            migrationBuilder.CreateIndex(
                name: "IX_relatos_EventoId",
                table: "relatos",
                column: "EventoId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_relatos_UsuarioId",
                table: "relatos",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_vestiveis_DispositivoId",
                table: "vestiveis",
                column: "DispositivoId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "alertas_vestivel");

            migrationBuilder.DropTable(
                name: "celulas_risco");

            migrationBuilder.DropTable(
                name: "consentimentos");

            migrationBuilder.DropTable(
                name: "distritos_frequentes");

            migrationBuilder.DropTable(
                name: "eventos_vestivel");

            migrationBuilder.DropTable(
                name: "mensagens");

            migrationBuilder.DropTable(
                name: "relatos");

            migrationBuilder.DropTable(
                name: "distritos");

            migrationBuilder.DropTable(
                name: "perfis_uso");

            migrationBuilder.DropTable(
                name: "vestiveis");
        }
    }
}
