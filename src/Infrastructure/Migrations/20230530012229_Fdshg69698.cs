using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Fdshg69698 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TriggerNotificationId",
                table: "ActionPlanAssociations");

            migrationBuilder.AddColumn<bool>(
                name: "TriggeredManually",
                table: "ActionPlanAssociations",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TriggeredManually",
                table: "ActionPlanAssociations");

            migrationBuilder.AddColumn<int>(
                name: "TriggerNotificationId",
                table: "ActionPlanAssociations",
                type: "int",
                nullable: true);
        }
    }
}
