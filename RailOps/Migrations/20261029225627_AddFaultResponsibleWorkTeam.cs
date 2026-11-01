using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RailOps.Migrations
{
    /// <inheritdoc />
    public partial class AddFaultResponsibleWorkTeam : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ResponsibleWorkTeamId",
                table: "Faults",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Faults_ResponsibleWorkTeamId",
                table: "Faults",
                column: "ResponsibleWorkTeamId");

            migrationBuilder.AddForeignKey(
                name: "FK_Faults_WorkTeams_ResponsibleWorkTeamId",
                table: "Faults",
                column: "ResponsibleWorkTeamId",
                principalTable: "WorkTeams",
                principalColumn: "WorkTeamId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Faults_WorkTeams_ResponsibleWorkTeamId",
                table: "Faults");

            migrationBuilder.DropIndex(
                name: "IX_Faults_ResponsibleWorkTeamId",
                table: "Faults");

            migrationBuilder.DropColumn(
                name: "ResponsibleWorkTeamId",
                table: "Faults");
        }
    }
}
