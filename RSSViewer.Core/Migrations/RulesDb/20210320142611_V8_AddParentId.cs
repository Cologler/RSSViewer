using Microsoft.EntityFrameworkCore.Migrations;

namespace RSSViewer.Migrations
{
    public partial class V8_AddParentId : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ParentId",
                table: "MatchRules",
                type: "INTEGER",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ParentId",
                table: "MatchRules");
        }
    }
}
