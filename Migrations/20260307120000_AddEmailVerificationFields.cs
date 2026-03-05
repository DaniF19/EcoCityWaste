using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EcoCityWaste.Migrations
{
    public partial class AddEmailVerificationFields : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "EmailVerified",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "EmailVerificationCodeHash",
                table: "Users",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "EmailVerificationExpiry",
                table: "Users",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "EmailVerificationSentAt",
                table: "Users",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "EmailVerificationBlockedUntil",
                table: "Users",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EmailVerificationAttempts",
                table: "Users",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EmailVerified",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "EmailVerificationCodeHash",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "EmailVerificationExpiry",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "EmailVerificationSentAt",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "EmailVerificationBlockedUntil",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "EmailVerificationAttempts",
                table: "Users");
        }
    }
}
