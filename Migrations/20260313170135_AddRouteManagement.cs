using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EcoCityWaste.Migrations
{
    /// <inheritdoc />
    public partial class AddRouteManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Criar a tabela de Routes
            migrationBuilder.CreateTable(
                name: "Routes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AssignedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AssignedEmployeeId = table.Column<int>(type: "int", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EstimatedDistanceKm = table.Column<double>(type: "float", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Routes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Routes_Users_AssignedEmployeeId",
                        column: x => x.AssignedEmployeeId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            // 2. Criar a tabela de RouteContainers
            migrationBuilder.CreateTable(
                name: "RouteContainers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RouteId = table.Column<int>(type: "int", nullable: false),
                    ContainerId = table.Column<int>(type: "int", nullable: false),
                    PickupOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RouteContainers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RouteContainers_Contentores_ContainerId",
                        column: x => x.ContainerId,
                        principalTable: "Contentores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict); // Usar Restrict para evitar ciclos
                    table.ForeignKey(
                        name: "FK_RouteContainers_Routes_RouteId",
                        column: x => x.RouteId,
                        principalTable: "Routes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // 3. Criar os Índices
            migrationBuilder.CreateIndex(
                name: "IX_RouteContainers_ContainerId",
                table: "RouteContainers",
                column: "ContainerId");

            migrationBuilder.CreateIndex(
                name: "IX_RouteContainers_RouteId",
                table: "RouteContainers",
                column: "RouteId");

            migrationBuilder.CreateIndex(
                name: "IX_Routes_AssignedEmployeeId",
                table: "Routes",
                column: "AssignedEmployeeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ContainerStatusHistories_Contentores_ContainerId",
                table: "ContainerStatusHistories");

            migrationBuilder.DropTable(
                name: "RouteContainers");

            migrationBuilder.DropTable(
                name: "Routes");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ContainerStatusHistories",
                table: "ContainerStatusHistories");

            migrationBuilder.DropIndex(
                name: "IX_ContainerStatusHistories_ContainerId",
                table: "ContainerStatusHistories");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "ContainerStatusHistories");

            migrationBuilder.DropColumn(
                name: "ChangedAt",
                table: "ContainerStatusHistories");

            migrationBuilder.DropColumn(
                name: "ChangedBy",
                table: "ContainerStatusHistories");

            migrationBuilder.DropColumn(
                name: "FillLevel",
                table: "ContainerStatusHistories");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "ContainerStatusHistories");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "ContainerStatusHistories");

            migrationBuilder.RenameTable(
                name: "ContainerStatusHistories",
                newName: "ContainerStatusHistory");

            migrationBuilder.AddColumn<DateTime>(
                name: "ChangedAt",
                table: "Notifications",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "ChangedBy",
                table: "Notifications",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "FillLevel",
                table: "Notifications",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Notifications",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "Notifications",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<int>(
                name: "ContainerId",
                table: "ContainerStatusHistory",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_ContainerId",
                table: "Notifications",
                column: "ContainerId");

            migrationBuilder.AddForeignKey(
                name: "FK_ContainerStatusHistory_Contentores_ContainerId",
                table: "ContainerStatusHistory",
                column: "ContainerId",
                principalTable: "Contentores",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
