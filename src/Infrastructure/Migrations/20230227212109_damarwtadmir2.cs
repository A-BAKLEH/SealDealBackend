using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class damarwtadmir2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ActionTrackers_ActionPlanAssociations_ActionPlanAssociationId",
                table: "ActionTrackers");

            migrationBuilder.DropForeignKey(
                name: "FK_ActionTrackers_Actions_TrackedActionId",
                table: "ActionTrackers");

            migrationBuilder.AddForeignKey(
                name: "FK_ActionTrackers_ActionPlanAssociations_ActionPlanAssociationId",
                table: "ActionTrackers",
                column: "ActionPlanAssociationId",
                principalTable: "ActionPlanAssociations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ActionTrackers_Actions_TrackedActionId",
                table: "ActionTrackers",
                column: "TrackedActionId",
                principalTable: "Actions",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ActionTrackers_ActionPlanAssociations_ActionPlanAssociationId",
                table: "ActionTrackers");

            migrationBuilder.DropForeignKey(
                name: "FK_ActionTrackers_Actions_TrackedActionId",
                table: "ActionTrackers");

            migrationBuilder.AddForeignKey(
                name: "FK_ActionTrackers_ActionPlanAssociations_ActionPlanAssociationId",
                table: "ActionTrackers",
                column: "ActionPlanAssociationId",
                principalTable: "ActionPlanAssociations",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ActionTrackers_Actions_TrackedActionId",
                table: "ActionTrackers",
                column: "TrackedActionId",
                principalTable: "Actions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
