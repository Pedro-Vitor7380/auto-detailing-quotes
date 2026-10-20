using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Orcamentos.Infrastructure.Dados.Migrations
{
    /// <inheritdoc />
    public partial class VeiculoCorAnoEFormasDePagamento : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ano_fabricacao",
                table: "veiculos",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "cor",
                table: "veiculos",
                type: "character varying(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<string[]>(
                name: "formas_de_pagamento",
                table: "oficinas",
                type: "text[]",
                nullable: false,
                defaultValue: new string[0]);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ano_fabricacao",
                table: "veiculos");

            migrationBuilder.DropColumn(
                name: "cor",
                table: "veiculos");

            migrationBuilder.DropColumn(
                name: "formas_de_pagamento",
                table: "oficinas");
        }
    }
}
