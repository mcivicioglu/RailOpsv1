using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RailOps.Migrations
{
    /// <inheritdoc />
    public partial class AddUserWorkTeamRelation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "WorkTeamId",
                table: "AspNetUsers",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_WorkTeamId",
                table: "AspNetUsers",
                column: "WorkTeamId");

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_WorkTeams_WorkTeamId",
                table: "AspNetUsers",
                column: "WorkTeamId",
                principalTable: "WorkTeams",
                principalColumn: "WorkTeamId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_WorkTeams_WorkTeamId",
                table: "AspNetUsers");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_WorkTeamId",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "WorkTeamId",
                table: "AspNetUsers");
        }
    }
}
