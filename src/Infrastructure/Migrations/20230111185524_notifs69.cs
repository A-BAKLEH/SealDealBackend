using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class notifs69 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NotifCreatedAt",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "NotifHandlingStatus",
                table: "Notifications");

            migrationBuilder.RenameColumn(
                name: "UnderlyingEventTimeStamp",
                table: "Notifications",
                newName: "EventTimeStamp");

            migrationBuilder.AddColumn<bool>(
                name: "IsRecevied",
                table: "Notifications",
                type: "bit",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsRecevied",
                table: "Notifications");

            migrationBuilder.RenameColumn(
                name: "EventTimeStamp",
                table: "Notifications",
                newName: "UnderlyingEventTimeStamp");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "NotifCreatedAt",
                table: "Notifications",
                type: "datetimeoffset",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<int>(
                name: "NotifHandlingStatus",
                table: "Notifications",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }
    }
}
