using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Clean.Architecture.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class asasdad : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "timeZoneId",
                table: "ToDoTasks");

            migrationBuilder.DropColumn(
                name: "IanaTimeZone",
                table: "Brokers");

            migrationBuilder.AddColumn<string>(
                name: "TimeZoneId",
                table: "Brokers",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TimeZoneId",
                table: "Brokers");

            migrationBuilder.AddColumn<string>(
                name: "timeZoneId",
                table: "ToDoTasks",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "IanaTimeZone",
                table: "Brokers",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }
    }
}
