using Microsoft.EntityFrameworkCore.Migrations;

namespace RSSViewer.Migrations
{
    public partial class V9_AddParentRef : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_MatchRules_ParentId",
                table: "MatchRules",
                column: "ParentId");

            migrationBuilder.AddForeignKey(
                name: "FK_MatchRules_MatchRules_ParentId",
                table: "MatchRules",
                column: "ParentId",
                principalTable: "MatchRules",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MatchRules_MatchRules_ParentId",
                table: "MatchRules");

            migrationBuilder.DropIndex(
                name: "IX_MatchRules_ParentId",
                table: "MatchRules");
        }
    }
}
