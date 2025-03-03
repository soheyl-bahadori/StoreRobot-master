using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StoreRobot.Migrations
{
    public partial class AddReport : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Price",
                table: "ProductErrors",
                newName: "ErroredPrice");

            migrationBuilder.AddColumn<string>(
                name: "FirstPrice",
                table: "ProductErrors",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "ReportNumber",
                table: "ProductErrors",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FirstPrice",
                table: "ProductErrors");

            migrationBuilder.DropColumn(
                name: "ReportNumber",
                table: "ProductErrors");

            migrationBuilder.RenameColumn(
                name: "ErroredPrice",
                table: "ProductErrors",
                newName: "Price");
        }
    }
}
