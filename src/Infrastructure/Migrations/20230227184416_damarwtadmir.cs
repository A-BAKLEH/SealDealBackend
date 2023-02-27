using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class damarwtadmir : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "HangfireReminderId",
                table: "ToDoTasks",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDone",
                table: "ToDoTasks",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "LeadsGeneratedCount",
                table: "Listings",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HangfireReminderId",
                table: "ToDoTasks");

            migrationBuilder.DropColumn(
                name: "IsDone",
                table: "ToDoTasks");

            migrationBuilder.DropColumn(
                name: "LeadsGeneratedCount",
                table: "Listings");
        }
    }
}
