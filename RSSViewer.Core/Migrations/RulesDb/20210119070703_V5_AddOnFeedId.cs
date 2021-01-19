using Microsoft.EntityFrameworkCore.Migrations;

namespace RSSViewer.Migrations
{
    public partial class V5_AddOnFeedId : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "OnFeedId",
                table: "MatchRules",
                type: "TEXT",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OnFeedId",
                table: "MatchRules");
        }
    }
}
