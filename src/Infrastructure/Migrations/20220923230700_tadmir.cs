using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    public partial class tadmir : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ActionTrackers_APActions_APActionId",
                table: "ActionTrackers");

            migrationBuilder.DropTable(
                name: "APActions");

            migrationBuilder.DropTable(
                name: "Histories");

            migrationBuilder.DropColumn(
                name: "Content",
                table: "ActionTrackers");

            migrationBuilder.RenameColumn(
                name: "Email",
                table: "Brokers",
                newName: "LoginEmail");

            migrationBuilder.RenameColumn(
                name: "APActionId",
                table: "ActionTrackers",
                newName: "TrackedActionId");

            migrationBuilder.RenameIndex(
                name: "IX_ActionTrackers_APActionId",
                table: "ActionTrackers",
                newName: "IX_ActionTrackers_TrackedActionId");

            migrationBuilder.RenameColumn(
                name: "ActionPlanStatus",
                table: "ActionPlanAssociations",
                newName: "ThisActionPlanStatus");

            migrationBuilder.AddColumn<string>(
                name: "FirstConnectedEmail",
                table: "Brokers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "NotifsForActionPlans",
                table: "Brokers",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SecondaryConnectedEmail",
                table: "Brokers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ActionResultId",
                table: "ActionTrackers",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ExecutionCompletedTime",
                table: "ActionTrackers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HangfireJobId",
                table: "ActionTrackers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "HangfireScheduledStartTime",
                table: "ActionTrackers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ActionsCount",
                table: "ActionPlans",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "AssignToLead",
                table: "ActionPlans",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "FirstActionDelay",
                table: "ActionPlans",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "NotifsToListenTo",
                table: "ActionPlans",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "StopPlanOnInteraction",
                table: "ActionPlans",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Title",
                table: "ActionPlans",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "Triggers",
                table: "ActionPlans",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "ActionPlanTriggeredAt",
                table: "ActionPlanAssociations",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "CustomDelay",
                table: "ActionPlanAssociations",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FirstActionHangfireId",
                table: "ActionPlanAssociations",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TriggerNotificationId",
                table: "ActionPlanAssociations",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "currentTrackedActionId",
                table: "ActionPlanAssociations",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Actions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ActionPlanId = table.Column<int>(type: "int", nullable: false),
                    ActionLevel = table.Column<int>(type: "int", nullable: false),
                    ActionProperties = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NextActionDelay = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Discriminator = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Actions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Actions_ActionPlans_ActionPlanId",
                        column: x => x.ActionPlanId,
                        principalTable: "ActionPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Notifications",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BrokerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LeadId = table.Column<int>(type: "int", nullable: true),
                    NotifCreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NotifType = table.Column<int>(type: "int", nullable: false),
                    ReadByBroker = table.Column<bool>(type: "bit", nullable: false),
                    NotifyBroker = table.Column<bool>(type: "bit", nullable: false),
                    NotifHandlingStatus = table.Column<int>(type: "int", nullable: false),
                    NotifData = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Notifications_Leads_LeadId",
                        column: x => x.LeadId,
                        principalTable: "Leads",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Actions_ActionPlanId",
                table: "Actions",
                column: "ActionPlanId");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_LeadId",
                table: "Notifications",
                column: "LeadId");

            migrationBuilder.AddForeignKey(
                name: "FK_ActionTrackers_Actions_TrackedActionId",
                table: "ActionTrackers",
                column: "TrackedActionId",
                principalTable: "Actions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ActionTrackers_Actions_TrackedActionId",
                table: "ActionTrackers");

            migrationBuilder.DropTable(
                name: "Actions");

            migrationBuilder.DropTable(
                name: "Notifications");

            migrationBuilder.DropColumn(
                name: "FirstConnectedEmail",
                table: "Brokers");

            migrationBuilder.DropColumn(
                name: "NotifsForActionPlans",
                table: "Brokers");

            migrationBuilder.DropColumn(
                name: "SecondaryConnectedEmail",
                table: "Brokers");

            migrationBuilder.DropColumn(
                name: "ActionResultId",
                table: "ActionTrackers");

            migrationBuilder.DropColumn(
                name: "ExecutionCompletedTime",
                table: "ActionTrackers");

            migrationBuilder.DropColumn(
                name: "HangfireJobId",
                table: "ActionTrackers");

            migrationBuilder.DropColumn(
                name: "HangfireScheduledStartTime",
                table: "ActionTrackers");

            migrationBuilder.DropColumn(
                name: "ActionsCount",
                table: "ActionPlans");

            migrationBuilder.DropColumn(
                name: "AssignToLead",
                table: "ActionPlans");

            migrationBuilder.DropColumn(
                name: "FirstActionDelay",
                table: "ActionPlans");

            migrationBuilder.DropColumn(
                name: "NotifsToListenTo",
                table: "ActionPlans");

            migrationBuilder.DropColumn(
                name: "StopPlanOnInteraction",
                table: "ActionPlans");

            migrationBuilder.DropColumn(
                name: "Title",
                table: "ActionPlans");

            migrationBuilder.DropColumn(
                name: "Triggers",
                table: "ActionPlans");

            migrationBuilder.DropColumn(
                name: "ActionPlanTriggeredAt",
                table: "ActionPlanAssociations");

            migrationBuilder.DropColumn(
                name: "CustomDelay",
                table: "ActionPlanAssociations");

            migrationBuilder.DropColumn(
                name: "FirstActionHangfireId",
                table: "ActionPlanAssociations");

            migrationBuilder.DropColumn(
                name: "TriggerNotificationId",
                table: "ActionPlanAssociations");

            migrationBuilder.DropColumn(
                name: "currentTrackedActionId",
                table: "ActionPlanAssociations");

            migrationBuilder.RenameColumn(
                name: "LoginEmail",
                table: "Brokers",
                newName: "Email");

            migrationBuilder.RenameColumn(
                name: "TrackedActionId",
                table: "ActionTrackers",
                newName: "APActionId");

            migrationBuilder.RenameIndex(
                name: "IX_ActionTrackers_TrackedActionId",
                table: "ActionTrackers",
                newName: "IX_ActionTrackers_APActionId");

            migrationBuilder.RenameColumn(
                name: "ThisActionPlanStatus",
                table: "ActionPlanAssociations",
                newName: "ActionPlanStatus");

            migrationBuilder.AddColumn<string>(
                name: "Content",
                table: "ActionTrackers",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "APActions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ParentActionId = table.Column<int>(type: "int", nullable: true),
                    ActionPlanId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_APActions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_APActions_ActionPlans_ActionPlanId",
                        column: x => x.ActionPlanId,
                        principalTable: "ActionPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_APActions_APActions_ParentActionId",
                        column: x => x.ParentActionId,
                        principalTable: "APActions",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Histories",
                columns: table => new
                {
                    HistoryId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AgencyId = table.Column<int>(type: "int", nullable: true),
                    LeadId = table.Column<int>(type: "int", nullable: false),
                    Event = table.Column<int>(type: "int", nullable: false),
                    EventDescription = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EventSubject = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EventTimestamp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FromActionPlan = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Histories", x => x.HistoryId);
                    table.ForeignKey(
                        name: "FK_Histories_Agencies_AgencyId",
                        column: x => x.AgencyId,
                        principalTable: "Agencies",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Histories_Leads_LeadId",
                        column: x => x.LeadId,
                        principalTable: "Leads",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_APActions_ActionPlanId",
                table: "APActions",
                column: "ActionPlanId");

            migrationBuilder.CreateIndex(
                name: "IX_APActions_ParentActionId",
                table: "APActions",
                column: "ParentActionId");

            migrationBuilder.CreateIndex(
                name: "IX_Histories_AgencyId",
                table: "Histories",
                column: "AgencyId");

            migrationBuilder.CreateIndex(
                name: "IX_Histories_LeadId",
                table: "Histories",
                column: "LeadId");

            migrationBuilder.AddForeignKey(
                name: "FK_ActionTrackers_APActions_APActionId",
                table: "ActionTrackers",
                column: "APActionId",
                principalTable: "APActions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
