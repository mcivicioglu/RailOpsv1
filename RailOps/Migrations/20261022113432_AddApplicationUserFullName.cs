using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RailOps.Migrations
{
    /// <inheritdoc />
    public partial class AddApplicationUserFullName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CreatedByUserId",
                table: "Faults",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FullName",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Faults_CreatedByUserId",
                table: "Faults",
                column: "CreatedByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Faults_AspNetUsers_CreatedByUserId",
                table: "Faults",
                column: "CreatedByUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Faults_AspNetUsers_CreatedByUserId",
                table: "Faults");

            migrationBuilder.DropIndex(
                name: "IX_Faults_CreatedByUserId",
                table: "Faults");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "Faults");

            migrationBuilder.DropColumn(
                name: "FullName",
                table: "AspNetUsers");
        }
    }
}
