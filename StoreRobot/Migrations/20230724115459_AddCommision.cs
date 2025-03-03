using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StoreRobot.Migrations
{
    public partial class AddCommision : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CommisionMax",
                table: "Products",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "CommisionMin",
                table: "Products",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CommisionMax",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "CommisionMin",
                table: "Products");
        }
    }
}
