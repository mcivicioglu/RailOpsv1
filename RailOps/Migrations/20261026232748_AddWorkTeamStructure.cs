using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RailOps.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkTeamStructure : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "WorkTeams",
                columns: table => new
                {
                    WorkTeamId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SectionId = table.Column<int>(type: "int", nullable: false),
                    LineGroupId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkTeams", x => x.WorkTeamId);
                    table.ForeignKey(
                        name: "FK_WorkTeams_LineGroups_LineGroupId",
                        column: x => x.LineGroupId,
                        principalTable: "LineGroups",
                        principalColumn: "LineGroupId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkTeams_Sections_SectionId",
                        column: x => x.SectionId,
                        principalTable: "Sections",
                        principalColumn: "SectionId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WorkTeams_LineGroupId",
                table: "WorkTeams",
                column: "LineGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkTeams_SectionId_LineGroupId",
                table: "WorkTeams",
                columns: new[] { "SectionId", "LineGroupId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WorkTeams");
        }
    }
}
