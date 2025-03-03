using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StoreRobot.Migrations
{
    public partial class Init : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Products",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    StokeQuantity = table.Column<int>(type: "INTEGER", nullable: false),
                    Sku = table.Column<string>(type: "TEXT", nullable: false),
                    UserPrice = table.Column<int>(type: "INTEGER", nullable: false),
                    ReferencePrice = table.Column<int>(type: "INTEGER", nullable: false),
                    IsPakhshOff = table.Column<bool>(type: "INTEGER", nullable: false),
                    PakhshPriceInOff = table.Column<int>(type: "INTEGER", nullable: false),
                    IsSafirOff = table.Column<bool>(type: "INTEGER", nullable: false),
                    SafirPriceInOff = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Products", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Products");
        }
    }
}
