using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RailOps.Migrations
{
    /// <inheritdoc />
    public partial class AddFaultResolutionTimestamp : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ResolvedAtUtc",
                table: "Faults",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ResolvedAtUtc",
                table: "Faults");
        }
    }
}
