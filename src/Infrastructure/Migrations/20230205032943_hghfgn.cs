using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class hghfgn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ActionStatusInfo",
                table: "ActionTrackers");

            migrationBuilder.DropColumn(
                name: "FirstActionHangfireId",
                table: "ActionPlanAssociations");

            migrationBuilder.AddColumn<bool>(
                name: "IsActionPlanResult",
                table: "Notifications",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<string>(
                name: "ActionResultId",
                table: "ActionTrackers",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<byte>(
                name: "ActionLevel",
                table: "Actions",
                type: "tinyint",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsActionPlanResult",
                table: "Notifications");

            migrationBuilder.AlterColumn<int>(
                name: "ActionResultId",
                table: "ActionTrackers",
                type: "int",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ActionStatusInfo",
                table: "ActionTrackers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "ActionLevel",
                table: "Actions",
                type: "int",
                nullable: false,
                oldClrType: typeof(byte),
                oldType: "tinyint");

            migrationBuilder.AddColumn<string>(
                name: "FirstActionHangfireId",
                table: "ActionPlanAssociations",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
