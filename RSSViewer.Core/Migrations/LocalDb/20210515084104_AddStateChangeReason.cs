using Microsoft.EntityFrameworkCore.Migrations;

namespace RSSViewer.Migrations.LocalDb
{
    public partial class AddStateChangeReason : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "StateChangeReason",
                table: "RssItems",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "StateChangeReasonExtras",
                table: "RssItems",
                type: "TEXT",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "StateChangeReason",
                table: "RssItems");

            migrationBuilder.DropColumn(
                name: "StateChangeReasonExtras",
                table: "RssItems");
        }
    }
}
