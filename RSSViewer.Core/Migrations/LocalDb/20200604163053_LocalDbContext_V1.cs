using Microsoft.EntityFrameworkCore.Migrations;

namespace RSSViewer.Migrations.LocalDb
{
    public partial class LocalDbContext_V1 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RssItems",
                columns: table => new
                {
                    FeedId = table.Column<string>(nullable: false),
                    RssId = table.Column<string>(nullable: false),
                    State = table.Column<int>(nullable: false),
                    RawText = table.Column<string>(nullable: true),
                    Title = table.Column<string>(nullable: true),
                    Description = table.Column<string>(nullable: true),
                    Link = table.Column<string>(nullable: true),
                    MagnetLink = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RssItems", x => new { x.FeedId, x.RssId });
                });

            migrationBuilder.CreateTable(
                name: "SyncSourceInfos",
                columns: table => new
                {
                    SyncSourceId = table.Column<string>(nullable: false),
                    LastSyncId = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SyncSourceInfos", x => x.SyncSourceId);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RssItems");

            migrationBuilder.DropTable(
                name: "SyncSourceInfos");
        }
    }
}
