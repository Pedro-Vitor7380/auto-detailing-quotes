using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Orcamentos.Infrastructure.Dados.Migrations
{
    /// <inheritdoc />
    public partial class Catalogo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "categorias",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    nome = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    criado_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    oficina_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_categorias", x => x.id);
                    table.ForeignKey(
                        name: "fk_categorias_oficinas_oficina_id",
                        column: x => x.oficina_id,
                        principalTable: "oficinas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "servicos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    categoria_id = table.Column<Guid>(type: "uuid", nullable: false),
                    nome = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    descricao = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    preco_minimo = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    preco_maximo = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    tempo_estimado_min = table.Column<int>(type: "integer", nullable: false),
                    ativo = table.Column<bool>(type: "boolean", nullable: false),
                    criado_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    oficina_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_servicos", x => x.id);
                    table.CheckConstraint("ck_servicos_faixa_de_preco", "preco_minimo >= 0 AND preco_maximo >= preco_minimo");
                    table.ForeignKey(
                        name: "fk_servicos_categorias_categoria_id",
                        column: x => x.categoria_id,
                        principalTable: "categorias",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_servicos_oficinas_oficina_id",
                        column: x => x.oficina_id,
                        principalTable: "oficinas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_categorias_oficina_id_nome",
                table: "categorias",
                columns: new[] { "oficina_id", "nome" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_servicos_categoria_id",
                table: "servicos",
                column: "categoria_id");

            migrationBuilder.CreateIndex(
                name: "ix_servicos_oficina_id_nome",
                table: "servicos",
                columns: new[] { "oficina_id", "nome" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "servicos");

            migrationBuilder.DropTable(
                name: "categorias");
        }
    }
}
