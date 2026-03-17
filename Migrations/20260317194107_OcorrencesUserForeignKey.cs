using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EcoCityWaste.Migrations
{
    /// <inheritdoc />
    public partial class OcorrencesUserForeignKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "UserId",
                table: "Occurrences",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Occurrences_UserId",
                table: "Occurrences",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Occurrences_Users_UserId",
                table: "Occurrences",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Occurrences_Users_UserId",
                table: "Occurrences");

            migrationBuilder.DropIndex(
                name: "IX_Occurrences_UserId",
                table: "Occurrences");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Occurrences");
        }
    }
}
