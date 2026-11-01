using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RailOps.Migrations
{
    /// <inheritdoc />
    public partial class AddFaultNumberAndYearCounter : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FaultNumber",
                table: "Faults",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "FaultYearCounters",
                columns: table => new
                {
                    Year = table.Column<int>(type: "int", nullable: false),
                    LastNumber = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FaultYearCounters", x => x.Year);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Faults_FaultNumber",
                table: "Faults",
                column: "FaultNumber",
                unique: true,
                filter: "[FaultNumber] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FaultYearCounters");

            migrationBuilder.DropIndex(
                name: "IX_Faults_FaultNumber",
                table: "Faults");

            migrationBuilder.DropColumn(
                name: "FaultNumber",
                table: "Faults");
        }
    }
}
