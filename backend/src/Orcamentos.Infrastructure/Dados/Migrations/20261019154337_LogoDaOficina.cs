using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Orcamentos.Infrastructure.Dados.Migrations
{
    /// <inheritdoc />
    public partial class LogoDaOficina : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "logo_url",
                table: "oficinas");

            migrationBuilder.AddColumn<byte[]>(
                name: "logo",
                table: "oficinas",
                type: "bytea",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "logo",
                table: "oficinas");

            migrationBuilder.AddColumn<string>(
                name: "logo_url",
                table: "oficinas",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);
        }
    }
}
