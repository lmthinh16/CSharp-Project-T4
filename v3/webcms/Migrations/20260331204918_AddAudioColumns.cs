using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace web_vk.Migrations
{
    /// <inheritdoc />
    public partial class AddAudioColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Text",
                table: "Audios",
                newName: "VoiceName");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "Audios",
                newName: "UploadedAt");

            migrationBuilder.RenameColumn(
                name: "AudioUrl",
                table: "Audios",
                newName: "Title");

            migrationBuilder.AddColumn<string>(
                name: "FileName",
                table: "Audios",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "FilePath",
                table: "Audios",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "RestaurantId",
                table: "Audios",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TextContent",
                table: "Audios",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Audios_RestaurantId",
                table: "Audios",
                column: "RestaurantId");

            migrationBuilder.AddForeignKey(
                name: "FK_Audios_Restaurants_RestaurantId",
                table: "Audios",
                column: "RestaurantId",
                principalTable: "Restaurants",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Audios_Restaurants_RestaurantId",
                table: "Audios");

            migrationBuilder.DropIndex(
                name: "IX_Audios_RestaurantId",
                table: "Audios");

            migrationBuilder.DropColumn(
                name: "FileName",
                table: "Audios");

            migrationBuilder.DropColumn(
                name: "FilePath",
                table: "Audios");

            migrationBuilder.DropColumn(
                name: "RestaurantId",
                table: "Audios");

            migrationBuilder.DropColumn(
                name: "TextContent",
                table: "Audios");

            migrationBuilder.RenameColumn(
                name: "VoiceName",
                table: "Audios",
                newName: "Text");

            migrationBuilder.RenameColumn(
                name: "UploadedAt",
                table: "Audios",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "Title",
                table: "Audios",
                newName: "AudioUrl");
        }
    }
}
