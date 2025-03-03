using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StoreRobot.Migrations
{
    public partial class AddDkpc : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Dkpc",
                table: "Products",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Dkpc",
                table: "Products");
        }
    }
}
