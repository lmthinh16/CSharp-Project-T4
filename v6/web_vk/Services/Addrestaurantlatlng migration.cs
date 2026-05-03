using Microsoft.EntityFrameworkCore.Migrations;

namespace web_vk.Services
{
    public partial class AddRestaurantLatLng : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Restaurants",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Lat",
                table: "Restaurants",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Lng",
                table: "Restaurants",
                type: "float",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "Description", table: "Restaurants");
            migrationBuilder.DropColumn(name: "Lat", table: "Restaurants");
            migrationBuilder.DropColumn(name: "Lng", table: "Restaurants");
        }
    }
}

