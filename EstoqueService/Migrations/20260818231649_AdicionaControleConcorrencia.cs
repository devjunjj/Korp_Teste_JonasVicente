using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EstoqueService.Migrations
{
    /// <inheritdoc />
    public partial class AdicionaControleConcorrencia : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "Produtos",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Version",
                table: "Produtos");
        }
    }
}
