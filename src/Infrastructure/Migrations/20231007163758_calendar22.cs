using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class calendar22 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "HangfireReminderId",
                table: "ToDoTasks",
                newName: "CalendarEvenId");

            migrationBuilder.RenameColumn(
                name: "HasCalendarPermissions",
                table: "ConnectedEmails",
                newName: "isMailbox");

            migrationBuilder.RenameColumn(
                name: "CalendarSyncEnabled",
                table: "ConnectedEmails",
                newName: "isCalendar");

            migrationBuilder.AddColumn<bool>(
                name: "CalendarSyncEnabled",
                table: "Brokers",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "hasConnectedCalendar",
                table: "Brokers",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CalendarSyncEnabled",
                table: "Brokers");

            migrationBuilder.DropColumn(
                name: "hasConnectedCalendar",
                table: "Brokers");

            migrationBuilder.RenameColumn(
                name: "CalendarEvenId",
                table: "ToDoTasks",
                newName: "HangfireReminderId");

            migrationBuilder.RenameColumn(
                name: "isMailbox",
                table: "ConnectedEmails",
                newName: "HasCalendarPermissions");

            migrationBuilder.RenameColumn(
                name: "isCalendar",
                table: "ConnectedEmails",
                newName: "CalendarSyncEnabled");
        }
    }
}
