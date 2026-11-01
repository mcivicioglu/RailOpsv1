using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RailOps.Migrations
{
    /// <inheritdoc />
    public partial class AddFaultAssignment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "AssignedAtUtc",
                table: "Faults",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AssignedToUserId",
                table: "Faults",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Faults_AssignedToUserId",
                table: "Faults",
                column: "AssignedToUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Faults_AspNetUsers_AssignedToUserId",
                table: "Faults",
                column: "AssignedToUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Faults_AspNetUsers_AssignedToUserId",
                table: "Faults");

            migrationBuilder.DropIndex(
                name: "IX_Faults_AssignedToUserId",
                table: "Faults");

            migrationBuilder.DropColumn(
                name: "AssignedAtUtc",
                table: "Faults");

            migrationBuilder.DropColumn(
                name: "AssignedToUserId",
                table: "Faults");
        }
    }
}
