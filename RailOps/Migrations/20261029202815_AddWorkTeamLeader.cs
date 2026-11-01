using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RailOps.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkTeamLeader : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TeamLeaderUserId",
                table: "WorkTeams",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_WorkTeams_TeamLeaderUserId",
                table: "WorkTeams",
                column: "TeamLeaderUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_WorkTeams_AspNetUsers_TeamLeaderUserId",
                table: "WorkTeams",
                column: "TeamLeaderUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_WorkTeams_AspNetUsers_TeamLeaderUserId",
                table: "WorkTeams");

            migrationBuilder.DropIndex(
                name: "IX_WorkTeams_TeamLeaderUserId",
                table: "WorkTeams");

            migrationBuilder.DropColumn(
                name: "TeamLeaderUserId",
                table: "WorkTeams");
        }
    }
}
