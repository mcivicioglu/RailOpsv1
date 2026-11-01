using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RailOps.Migrations
{
    /// <inheritdoc />
    public partial class AddLineStructure : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LineGroups",
                columns: table => new
                {
                    LineGroupId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LineGroupName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LineGroups", x => x.LineGroupId);
                });

            migrationBuilder.CreateTable(
                name: "Lines",
                columns: table => new
                {
                    LineId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LineCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    LineGroupId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Lines", x => x.LineId);
                    table.ForeignKey(
                        name: "FK_Lines_LineGroups_LineGroupId",
                        column: x => x.LineGroupId,
                        principalTable: "LineGroups",
                        principalColumn: "LineGroupId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LineGroups_LineGroupName",
                table: "LineGroups",
                column: "LineGroupName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Lines_LineCode",
                table: "Lines",
                column: "LineCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Lines_LineGroupId",
                table: "Lines",
                column: "LineGroupId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Lines");

            migrationBuilder.DropTable(
                name: "LineGroups");
        }
    }
}
