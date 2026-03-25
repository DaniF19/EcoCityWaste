using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EcoCityWaste.Migrations
{
    /// <inheritdoc />
    public partial class UpdateNotifications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Tornar ContainerId nullable
            migrationBuilder.AlterColumn<int>(
                name: "ContainerId",
                table: "Notifications",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            // Adicionar RouteId nullable
            migrationBuilder.AddColumn<int>(
                name: "RouteId",
                table: "Notifications",
                type: "int",
                nullable: true);

            // Adicionar UserId nullable
            migrationBuilder.AddColumn<int>(
                name: "UserId",
                table: "Notifications",
                type: "int",
                nullable: true); // <-- aqui é nullable

            // Criar índice para UserId
            migrationBuilder.CreateIndex(
                name: "IX_Notifications_UserId",
                table: "Notifications",
                column: "UserId");

            // Criar FK para Users, com Restrict para evitar conflito com dados existentes
            migrationBuilder.AddForeignKey(
                name: "FK_Notifications_Users_UserId",
                table: "Notifications",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict); // <-- não usar Cascade
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Remover FK
            migrationBuilder.DropForeignKey(
                name: "FK_Notifications_Users_UserId",
                table: "Notifications");

            // Remover índice
            migrationBuilder.DropIndex(
                name: "IX_Notifications_UserId",
                table: "Notifications");

            // Remover colunas adicionadas
            migrationBuilder.DropColumn(
                name: "RouteId",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Notifications");

            // Voltar ContainerId a não nullable
            migrationBuilder.AlterColumn<int>(
                name: "ContainerId",
                table: "Notifications",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);
        }
    }
}