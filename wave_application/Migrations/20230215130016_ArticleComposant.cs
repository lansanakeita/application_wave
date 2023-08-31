using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace waveapplication.Migrations
{
    /// <inheritdoc />
    public partial class ArticleComposant : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ArticleComposant",
                table: "Tremies",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ArticleComposant",
                table: "Tremies");
        }
    }
}
