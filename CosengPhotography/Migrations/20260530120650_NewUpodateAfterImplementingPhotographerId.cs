using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CosengPhotography.Migrations
{
    /// <inheritdoc />
    public partial class NewUpodateAfterImplementingPhotographerId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "OwnerId",
                table: "Galleries",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Galleries_OwnerId",
                table: "Galleries",
                column: "OwnerId");

            migrationBuilder.AddForeignKey(
                name: "FK_Galleries_AspNetUsers_OwnerId",
                table: "Galleries",
                column: "OwnerId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Galleries_AspNetUsers_OwnerId",
                table: "Galleries");

            migrationBuilder.DropIndex(
                name: "IX_Galleries_OwnerId",
                table: "Galleries");

            migrationBuilder.DropColumn(
                name: "OwnerId",
                table: "Galleries");
        }
    }
}
