using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Orcamentos.Infrastructure.Dados.Migrations
{
    /// <inheritdoc />
    public partial class Agendamentos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "data_agendada",
                table: "orcamentos",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "carros_por_dia",
                table: "oficinas",
                type: "integer",
                nullable: false,
                defaultValue: 3);

            migrationBuilder.CreateIndex(
                name: "ix_orcamentos_oficina_id_data_agendada",
                table: "orcamentos",
                columns: new[] { "oficina_id", "data_agendada" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_orcamentos_oficina_id_data_agendada",
                table: "orcamentos");

            migrationBuilder.DropColumn(
                name: "data_agendada",
                table: "orcamentos");

            migrationBuilder.DropColumn(
                name: "carros_por_dia",
                table: "oficinas");
        }
    }
}
