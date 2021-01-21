using Microsoft.EntityFrameworkCore.Migrations;

namespace RSSViewer.Migrations
{
    public partial class V7_RemoveExtraOptions : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExtraOptions",
                table: "MatchRules");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ExtraOptions",
                table: "MatchRules",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }
    }
}
