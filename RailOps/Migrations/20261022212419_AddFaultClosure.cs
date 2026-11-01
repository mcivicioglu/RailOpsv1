using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RailOps.Migrations
{
    /// <inheritdoc />
    public partial class AddFaultClosure : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ClosedAtUtc",
                table: "Faults",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ClosedByUserId",
                table: "Faults",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Faults_ClosedByUserId",
                table: "Faults",
                column: "ClosedByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Faults_AspNetUsers_ClosedByUserId",
                table: "Faults",
                column: "ClosedByUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Faults_AspNetUsers_ClosedByUserId",
                table: "Faults");

            migrationBuilder.DropIndex(
                name: "IX_Faults_ClosedByUserId",
                table: "Faults");

            migrationBuilder.DropColumn(
                name: "ClosedAtUtc",
                table: "Faults");

            migrationBuilder.DropColumn(
                name: "ClosedByUserId",
                table: "Faults");
        }
    }
}
