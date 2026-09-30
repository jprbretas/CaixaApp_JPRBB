using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CaixaProjeto.ApiService.Data.Migrations
{
    /// <inheritdoc />
    public partial class Inicial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Clientes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Nif = table.Column<string>(type: "TEXT", maxLength: 9, nullable: false),
                    DataRegisto = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Clientes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Pedidos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Numero = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    ClienteId = table.Column<int>(type: "INTEGER", nullable: true),
                    Nif = table.Column<string>(type: "TEXT", nullable: true),
                    Idade = table.Column<int>(type: "INTEGER", nullable: false),
                    RendimentoMensalLiquido = table.Column<double>(type: "REAL", nullable: false),
                    PrestacoesAtuais = table.Column<double>(type: "REAL", nullable: false),
                    ValorPretendido = table.Column<double>(type: "REAL", nullable: false),
                    PrazoMeses = table.Column<int>(type: "INTEGER", nullable: false),
                    SituacaoProfissional = table.Column<string>(type: "TEXT", nullable: true),
                    IncidentesCredito = table.Column<bool>(type: "INTEGER", nullable: false),
                    PrestacaoEstimada = table.Column<double>(type: "REAL", nullable: true),
                    TaxaEsforco = table.Column<double>(type: "REAL", nullable: true),
                    IdadeFinalContrato = table.Column<double>(type: "REAL", nullable: true),
                    LimiteMontante = table.Column<double>(type: "REAL", nullable: true),
                    DecisaoAutomatica = table.Column<string>(type: "TEXT", nullable: false),
                    EstadoAtual = table.Column<string>(type: "TEXT", nullable: false),
                    DataSubmissao = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pedidos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Pedidos_Clientes_ClienteId",
                        column: x => x.ClienteId,
                        principalTable: "Clientes",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Simulacoes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ClienteId = table.Column<int>(type: "INTEGER", nullable: true),
                    Nif = table.Column<string>(type: "TEXT", nullable: true),
                    Idade = table.Column<int>(type: "INTEGER", nullable: false),
                    RendimentoMensalLiquido = table.Column<double>(type: "REAL", nullable: false),
                    PrestacoesAtuais = table.Column<double>(type: "REAL", nullable: false),
                    ValorPretendido = table.Column<double>(type: "REAL", nullable: false),
                    PrazoMeses = table.Column<int>(type: "INTEGER", nullable: false),
                    SituacaoProfissional = table.Column<string>(type: "TEXT", nullable: true),
                    IncidentesCredito = table.Column<bool>(type: "INTEGER", nullable: false),
                    Decisao = table.Column<string>(type: "TEXT", nullable: false),
                    DataSimulacao = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Simulacoes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Simulacoes_Clientes_ClienteId",
                        column: x => x.ClienteId,
                        principalTable: "Clientes",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "HistoricoEstados",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PedidoId = table.Column<int>(type: "INTEGER", nullable: false),
                    EstadoAnterior = table.Column<string>(type: "TEXT", nullable: true),
                    EstadoNovo = table.Column<string>(type: "TEXT", nullable: false),
                    Data = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Utilizador = table.Column<string>(type: "TEXT", nullable: false),
                    Observacao = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HistoricoEstados", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HistoricoEstados_Pedidos_PedidoId",
                        column: x => x.PedidoId,
                        principalTable: "Pedidos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MotivosPedido",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PedidoId = table.Column<int>(type: "INTEGER", nullable: false),
                    Regra = table.Column<int>(type: "INTEGER", nullable: false),
                    Descricao = table.Column<string>(type: "TEXT", nullable: false),
                    Decisao = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MotivosPedido", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MotivosPedido_Pedidos_PedidoId",
                        column: x => x.PedidoId,
                        principalTable: "Pedidos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Clientes_Nif",
                table: "Clientes",
                column: "Nif",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_HistoricoEstados_PedidoId",
                table: "HistoricoEstados",
                column: "PedidoId");

            migrationBuilder.CreateIndex(
                name: "IX_MotivosPedido_PedidoId",
                table: "MotivosPedido",
                column: "PedidoId");

            migrationBuilder.CreateIndex(
                name: "IX_Pedidos_ClienteId",
                table: "Pedidos",
                column: "ClienteId");

            migrationBuilder.CreateIndex(
                name: "IX_Pedidos_DataSubmissao",
                table: "Pedidos",
                column: "DataSubmissao");

            migrationBuilder.CreateIndex(
                name: "IX_Pedidos_Numero",
                table: "Pedidos",
                column: "Numero",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Simulacoes_ClienteId",
                table: "Simulacoes",
                column: "ClienteId");

            migrationBuilder.CreateIndex(
                name: "IX_Simulacoes_DataSimulacao",
                table: "Simulacoes",
                column: "DataSimulacao");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HistoricoEstados");

            migrationBuilder.DropTable(
                name: "MotivosPedido");

            migrationBuilder.DropTable(
                name: "Simulacoes");

            migrationBuilder.DropTable(
                name: "Pedidos");

            migrationBuilder.DropTable(
                name: "Clientes");
        }
    }
}
