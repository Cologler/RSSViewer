using Microsoft.EntityFrameworkCore.Migrations;

namespace RSSViewer.Migrations
{
    public partial class V10_AddDisplayName : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DisplayName",
                table: "MatchRules",
                type: "TEXT",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DisplayName",
                table: "MatchRules");
        }
    }
}
