using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StoreRobot.Migrations
{
    public partial class DigikalaLimitLogs : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DigikalaLimitLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RequestCount = table.Column<int>(type: "INTEGER", nullable: false),
                    Limited = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreateDateTime = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DigikalaLimitLogs", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DigikalaLimitLogs");
        }
    }
}
