using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Orcamentos.Infrastructure.Dados.Migrations
{
    /// <inheritdoc />
    public partial class Orcamentos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "contadores_de_orcamentos",
                columns: table => new
                {
                    oficina_id = table.Column<Guid>(type: "uuid", nullable: false),
                    ano = table.Column<int>(type: "integer", nullable: false),
                    ultimo = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_contadores_de_orcamentos", x => new { x.oficina_id, x.ano });
                    table.ForeignKey(
                        name: "fk_contadores_de_orcamentos_oficinas_oficina_id",
                        column: x => x.oficina_id,
                        principalTable: "oficinas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "orcamentos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    ano = table.Column<int>(type: "integer", nullable: false),
                    sequencial = table.Column<int>(type: "integer", nullable: false),
                    cliente_id = table.Column<Guid>(type: "uuid", nullable: false),
                    veiculo_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    desconto_tipo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    desconto_valor = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    validade = table.Column<DateOnly>(type: "date", nullable: false),
                    observacoes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    enviado_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    aprovado_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    recusado_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    criado_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    oficina_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_orcamentos", x => x.id);
                    table.ForeignKey(
                        name: "fk_orcamentos_clientes_cliente_id",
                        column: x => x.cliente_id,
                        principalTable: "clientes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_orcamentos_oficinas_oficina_id",
                        column: x => x.oficina_id,
                        principalTable: "oficinas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_orcamentos_veiculos_veiculo_id",
                        column: x => x.veiculo_id,
                        principalTable: "veiculos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "itens_orcamento",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    servico_id = table.Column<Guid>(type: "uuid", nullable: true),
                    descricao = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    quantidade = table.Column<int>(type: "integer", nullable: false),
                    preco_catalogo = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    valor_unitario = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    tempo_estimado_min = table.Column<int>(type: "integer", nullable: false),
                    observacao_estado = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    orcamento_id = table.Column<Guid>(type: "uuid", nullable: false),
                    criado_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_itens_orcamento", x => x.id);
                    table.ForeignKey(
                        name: "fk_itens_orcamento_orcamentos_orcamento_id",
                        column: x => x.orcamento_id,
                        principalTable: "orcamentos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_itens_orcamento_servicos_servico_id",
                        column: x => x.servico_id,
                        principalTable: "servicos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_itens_orcamento_orcamento_id",
                table: "itens_orcamento",
                column: "orcamento_id");

            migrationBuilder.CreateIndex(
                name: "ix_itens_orcamento_servico_id",
                table: "itens_orcamento",
                column: "servico_id");

            migrationBuilder.CreateIndex(
                name: "ix_orcamentos_cliente_id",
                table: "orcamentos",
                column: "cliente_id");

            migrationBuilder.CreateIndex(
                name: "ix_orcamentos_oficina_id_ano_sequencial",
                table: "orcamentos",
                columns: new[] { "oficina_id", "ano", "sequencial" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_orcamentos_oficina_id_status",
                table: "orcamentos",
                columns: new[] { "oficina_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_orcamentos_veiculo_id",
                table: "orcamentos",
                column: "veiculo_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "contadores_de_orcamentos");

            migrationBuilder.DropTable(
                name: "itens_orcamento");

            migrationBuilder.DropTable(
                name: "orcamentos");
        }
    }
}
