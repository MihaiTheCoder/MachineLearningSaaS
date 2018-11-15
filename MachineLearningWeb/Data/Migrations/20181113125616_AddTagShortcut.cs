using Microsoft.EntityFrameworkCore.Migrations;

namespace MachineLearningWeb.Data.Migrations
{
    public partial class AddTagShortcut : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TagShortcut",
                table: "Tags",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TagShortcut",
                table: "Tags");
        }
    }
}
