using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace web_vk.Migrations
{
    /// <inheritdoc />
    public partial class UpdateAudioRelationship : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsGeneratedByTTS",
                table: "Audios",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsGeneratedByTTS",
                table: "Audios");
        }
    }
}
