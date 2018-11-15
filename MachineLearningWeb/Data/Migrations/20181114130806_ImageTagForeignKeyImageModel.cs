using Microsoft.EntityFrameworkCore.Migrations;

namespace MachineLearningWeb.Data.Migrations
{
    public partial class ImageTagForeignKeyImageModel : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_ImageTags_ImageId",
                table: "ImageTags",
                column: "ImageId");

            migrationBuilder.AddForeignKey(
                name: "FK_ImageTags_ImageModel_ImageId",
                table: "ImageTags",
                column: "ImageId",
                principalTable: "ImageModel",
                principalColumn: "ID",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ImageTags_ImageModel_ImageId",
                table: "ImageTags");

            migrationBuilder.DropIndex(
                name: "IX_ImageTags_ImageId",
                table: "ImageTags");
        }
    }
}
