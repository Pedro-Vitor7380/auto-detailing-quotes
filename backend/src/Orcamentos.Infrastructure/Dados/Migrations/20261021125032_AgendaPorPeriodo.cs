using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Orcamentos.Infrastructure.Dados.Migrations
{
    /// <inheritdoc />
    public partial class AgendaPorPeriodo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_orcamentos_oficina_id_data_agendada",
                table: "orcamentos");

            migrationBuilder.DropColumn(
                name: "carros_por_dia",
                table: "oficinas");

            migrationBuilder.RenameColumn(
                name: "data_agendada",
                table: "orcamentos",
                newName: "agendado_de");

            migrationBuilder.AddColumn<DateOnly>(
                name: "agendado_ate",
                table: "orcamentos",
                type: "date",
                nullable: true);

            // Escrito à mão (o EF não tem como saber): os agendamentos que já existiam eram de UM dia,
            // então o fim é o próprio dia. Sem isto, eles ficariam com início e sem fim.
            migrationBuilder.Sql("UPDATE orcamentos SET agendado_ate = agendado_de WHERE agendado_de IS NOT NULL;");

            migrationBuilder.AlterColumn<string>(
                name: "telefone",
                table: "oficinas",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_orcamentos_oficina_id_agendado_de_agendado_ate",
                table: "orcamentos",
                columns: new[] { "oficina_id", "agendado_de", "agendado_ate" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_orcamentos_oficina_id_agendado_de_agendado_ate",
                table: "orcamentos");

            migrationBuilder.DropColumn(
                name: "agendado_ate",
                table: "orcamentos");

            migrationBuilder.RenameColumn(
                name: "agendado_de",
                table: "orcamentos",
                newName: "data_agendada");

            migrationBuilder.AlterColumn<string>(
                name: "telefone",
                table: "oficinas",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldNullable: true);

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
    }
}
