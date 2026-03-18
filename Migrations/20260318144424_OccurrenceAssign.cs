using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EcoCityWaste.Migrations
{
    /// <inheritdoc />
    public partial class OccurrenceAssign : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "AssignedAt",
                table: "Occurrences",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AssignedEmployeeId",
                table: "Occurrences",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Occurrences_AssignedEmployeeId",
                table: "Occurrences",
                column: "AssignedEmployeeId");

            migrationBuilder.AddForeignKey(
                name: "FK_Occurrences_Users_AssignedEmployeeId",
                table: "Occurrences",
                column: "AssignedEmployeeId",
                principalTable: "Users",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Occurrences_Users_AssignedEmployeeId",
                table: "Occurrences");

            migrationBuilder.DropIndex(
                name: "IX_Occurrences_AssignedEmployeeId",
                table: "Occurrences");

            migrationBuilder.DropColumn(
                name: "AssignedAt",
                table: "Occurrences");

            migrationBuilder.DropColumn(
                name: "AssignedEmployeeId",
                table: "Occurrences");
        }
    }
}
