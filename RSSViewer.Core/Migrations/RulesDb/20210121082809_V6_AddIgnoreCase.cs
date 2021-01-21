using Microsoft.EntityFrameworkCore.Migrations;

namespace RSSViewer.Migrations
{
    public partial class V6_AddIgnoreCase : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IgnoreCase",
                table: "MatchRules",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IgnoreCase",
                table: "MatchRules");
        }
    }
}
