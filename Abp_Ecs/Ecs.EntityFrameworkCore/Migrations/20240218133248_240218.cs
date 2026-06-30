using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ecs.Migrations
{
    public partial class _240218 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_StationParas",
                table: "StationParas");

            migrationBuilder.RenameTable(
                name: "StationParas",
                newName: "Variables");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Variables",
                table: "Variables",
                column: "Id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_Variables",
                table: "Variables");

            migrationBuilder.RenameTable(
                name: "Variables",
                newName: "StationParas");

            migrationBuilder.AddPrimaryKey(
                name: "PK_StationParas",
                table: "StationParas",
                column: "Id");
        }
    }
}
