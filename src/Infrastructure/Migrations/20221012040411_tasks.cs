using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    public partial class tasks : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "TaskText",
                table: "ToDoTasks",
                newName: "TaskName");

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "ToDoTasks",
                type: "nvarchar(max)",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Description",
                table: "ToDoTasks");

            migrationBuilder.RenameColumn(
                name: "TaskName",
                table: "ToDoTasks",
                newName: "TaskText");
        }
    }
}
