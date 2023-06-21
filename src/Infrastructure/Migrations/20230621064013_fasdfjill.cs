using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class fasdfjill : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NextScheduledTime",
                table: "RecurrentTasks");

            migrationBuilder.DropColumn(
                name: "taskStatus",
                table: "RecurrentTasks");

            migrationBuilder.AlterColumn<string>(
                name: "HangfireTaskId",
                table: "RecurrentTasks",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "HangfireTaskId",
                table: "RecurrentTasks",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<DateTime>(
                name: "NextScheduledTime",
                table: "RecurrentTasks",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "taskStatus",
                table: "RecurrentTasks",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }
    }
}
