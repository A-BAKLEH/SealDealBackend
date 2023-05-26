using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Reee : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastSeenTimesTamp",
                table: "Brokers");

            migrationBuilder.RenameColumn(
                name: "LastSeenNotifId",
                table: "Brokers",
                newName: "LastUnassignedLeadIdAnalyzed");

            migrationBuilder.AlterColumn<bool>(
                name: "RepliedTo",
                table: "EmailEvents",
                type: "bit",
                nullable: false,
                defaultValue: false,
                oldClrType: typeof(bool),
                oldType: "bit",
                oldNullable: true);

            migrationBuilder.AlterColumn<bool>(
                name: "NeedsAction",
                table: "EmailEvents",
                type: "bit",
                nullable: false,
                defaultValue: false,
                oldClrType: typeof(bool),
                oldType: "bit",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ConversationId",
                table: "EmailEvents",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AppEventAnalyzerLastId",
                table: "Brokers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "EmailEventAnalyzerLastTimestamp",
                table: "Brokers",
                type: "datetimeoffset",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<int>(
                name: "LastSeenAppEventId",
                table: "Brokers",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ConversationId",
                table: "EmailEvents");

            migrationBuilder.DropColumn(
                name: "AppEventAnalyzerLastId",
                table: "Brokers");

            migrationBuilder.DropColumn(
                name: "EmailEventAnalyzerLastTimestamp",
                table: "Brokers");

            migrationBuilder.DropColumn(
                name: "LastSeenAppEventId",
                table: "Brokers");

            migrationBuilder.RenameColumn(
                name: "LastUnassignedLeadIdAnalyzed",
                table: "Brokers",
                newName: "LastSeenNotifId");

            migrationBuilder.AlterColumn<bool>(
                name: "RepliedTo",
                table: "EmailEvents",
                type: "bit",
                nullable: true,
                oldClrType: typeof(bool),
                oldType: "bit");

            migrationBuilder.AlterColumn<bool>(
                name: "NeedsAction",
                table: "EmailEvents",
                type: "bit",
                nullable: true,
                oldClrType: typeof(bool),
                oldType: "bit");

            migrationBuilder.AddColumn<DateTime>(
                name: "LastSeenTimesTamp",
                table: "Brokers",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }
    }
}
