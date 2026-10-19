using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Orcamentos.Infrastructure.Dados.Migrations
{
    /// <inheritdoc />
    public partial class RemoveTempoEstimado : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "tempo_estimado_min",
                table: "servicos");

            migrationBuilder.DropColumn(
                name: "tempo_estimado_min",
                table: "itens_orcamento");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "tempo_estimado_min",
                table: "servicos",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "tempo_estimado_min",
                table: "itens_orcamento",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }
    }
}
