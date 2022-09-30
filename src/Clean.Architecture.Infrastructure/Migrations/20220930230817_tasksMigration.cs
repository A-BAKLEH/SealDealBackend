using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Clean.Architecture.Infrastructure.Migrations
{
    public partial class tasksMigration : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SecondaryConnectedEmail",
                table: "Brokers");

            migrationBuilder.AlterColumn<string>(
                name: "FirstConnectedEmail",
                table: "Brokers",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.CreateTable(
                name: "RecurrentTasks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BrokerId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    HangfireTaskId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ScheduledTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    taskStatus = table.Column<int>(type: "int", nullable: false),
                    Discriminator = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LastEmailToken = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecurrentTasks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RecurrentTasks_Brokers_BrokerId",
                        column: x => x.BrokerId,
                        principalTable: "Brokers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Brokers_FirstConnectedEmail",
                table: "Brokers",
                column: "FirstConnectedEmail");

            migrationBuilder.CreateIndex(
                name: "IX_RecurrentTasks_BrokerId",
                table: "RecurrentTasks",
                column: "BrokerId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RecurrentTasks");

            migrationBuilder.DropIndex(
                name: "IX_Brokers_FirstConnectedEmail",
                table: "Brokers");

            migrationBuilder.AlterColumn<string>(
                name: "FirstConnectedEmail",
                table: "Brokers",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AddColumn<string>(
                name: "SecondaryConnectedEmail",
                table: "Brokers",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
