using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Orcamentos.Infrastructure.Dados.Migrations
{
    /// <inheritdoc />
    public partial class Notificacoes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "aviso_de_entrega_em",
                table: "orcamentos",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "inscricoes_de_notificacao",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    endpoint = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    p256dh = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    auth = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    criado_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    oficina_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_inscricoes_de_notificacao", x => x.id);
                    table.ForeignKey(
                        name: "fk_inscricoes_de_notificacao_oficinas_oficina_id",
                        column: x => x.oficina_id,
                        principalTable: "oficinas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_inscricoes_de_notificacao_endpoint",
                table: "inscricoes_de_notificacao",
                column: "endpoint",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_inscricoes_de_notificacao_oficina_id",
                table: "inscricoes_de_notificacao",
                column: "oficina_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "inscricoes_de_notificacao");

            migrationBuilder.DropColumn(
                name: "aviso_de_entrega_em",
                table: "orcamentos");
        }
    }
}
