using Microsoft.EntityFrameworkCore.Migrations;

namespace RSSViewer.Migrations
{
    public partial class AddTag : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "HandlerType",
                table: "MatchRules",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "Tags",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    TagGroupName = table.Column<string>(type: "TEXT", nullable: true),
                    TagName = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tags", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Tags_TagName",
                table: "Tags",
                column: "TagName",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Tags");

            migrationBuilder.DropColumn(
                name: "HandlerType",
                table: "MatchRules");
        }
    }
}
