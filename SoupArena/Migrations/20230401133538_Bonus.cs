using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SoupArena.Migrations
{
    /// <inheritdoc />
    public partial class Bonus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "LastTimeClaimedBonus",
                table: "Players",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastTimeClaimedBonus",
                table: "Players");
        }
    }
}
