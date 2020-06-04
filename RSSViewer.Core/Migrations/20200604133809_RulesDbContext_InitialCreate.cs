using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace RSSViewer.Migrations
{
    public partial class RulesDbContext_InitialCreate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MatchRules",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Mode = table.Column<int>(nullable: false),
                    Argument = table.Column<string>(nullable: true),
                    ExtraOptions = table.Column<int>(nullable: false),
                    Action = table.Column<int>(nullable: false),
                    IsDisabled = table.Column<bool>(nullable: false),
                    AutoExpiredAfterLastMatched = table.Column<TimeSpan>(nullable: true),
                    AutoDisabledAfterLastMatched = table.Column<TimeSpan>(nullable: true),
                    LastMatched = table.Column<DateTime>(nullable: false),
                    TotalMatchedCount = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MatchRules", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MatchRules");
        }
    }
}
