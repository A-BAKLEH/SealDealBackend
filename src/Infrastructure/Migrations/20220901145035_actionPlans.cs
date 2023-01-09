using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    public partial class actionPlans : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "FromActionPlan",
                table: "Histories",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "ActionPlans",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BrokerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TimeCreated = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActionPlans", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ActionPlans_Brokers_BrokerId",
                        column: x => x.BrokerId,
                        principalTable: "Brokers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ActionPlanAssociations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ActionPlanId = table.Column<int>(type: "int", nullable: true),
                    LeadId = table.Column<int>(type: "int", nullable: false),
                    ActionPlanStatus = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActionPlanAssociations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ActionPlanAssociations_ActionPlans_ActionPlanId",
                        column: x => x.ActionPlanId,
                        principalTable: "ActionPlans",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ActionPlanAssociations_Leads_LeadId",
                        column: x => x.LeadId,
                        principalTable: "Leads",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "APActions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ActionPlanId = table.Column<int>(type: "int", nullable: false),
                    ParentActionId = table.Column<int>(type: "int", nullable: true)
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
                name: "ActionTrackers",
                columns: table => new
                {
                    APActionId = table.Column<int>(type: "int", nullable: false),
                    ActionPlanAssociationId = table.Column<int>(type: "int", nullable: false),
                    ActionStatus = table.Column<int>(type: "int", nullable: false),
                    ActionStatusInfo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActionTrackers", x => new { x.ActionPlanAssociationId, x.APActionId });
                    table.ForeignKey(
                        name: "FK_ActionTrackers_ActionPlanAssociations_ActionPlanAssociationId",
                        column: x => x.ActionPlanAssociationId,
                        principalTable: "ActionPlanAssociations",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ActionTrackers_APActions_APActionId",
                        column: x => x.APActionId,
                        principalTable: "APActions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ActionPlanAssociations_ActionPlanId",
                table: "ActionPlanAssociations",
                column: "ActionPlanId");

            migrationBuilder.CreateIndex(
                name: "IX_ActionPlanAssociations_LeadId",
                table: "ActionPlanAssociations",
                column: "LeadId");

            migrationBuilder.CreateIndex(
                name: "IX_ActionPlans_BrokerId",
                table: "ActionPlans",
                column: "BrokerId");

            migrationBuilder.CreateIndex(
                name: "IX_ActionTrackers_APActionId",
                table: "ActionTrackers",
                column: "APActionId");

            migrationBuilder.CreateIndex(
                name: "IX_APActions_ActionPlanId",
                table: "APActions",
                column: "ActionPlanId");

            migrationBuilder.CreateIndex(
                name: "IX_APActions_ParentActionId",
                table: "APActions",
                column: "ParentActionId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ActionTrackers");

            migrationBuilder.DropTable(
                name: "ActionPlanAssociations");

            migrationBuilder.DropTable(
                name: "APActions");

            migrationBuilder.DropTable(
                name: "ActionPlans");

            migrationBuilder.DropColumn(
                name: "FromActionPlan",
                table: "Histories");
        }
    }
}
