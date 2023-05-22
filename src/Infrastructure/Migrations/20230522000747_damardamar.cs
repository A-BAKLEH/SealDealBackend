using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class damardamar : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RecurrentTasks_Brokers_BrokerId",
                table: "RecurrentTasks");

            migrationBuilder.DropTable(
                name: "LeadInteractions");

            migrationBuilder.DropTable(
                name: "Notifications");

            migrationBuilder.DropTable(
                name: "TestBase");

            migrationBuilder.DropColumn(
                name: "LastEmailToken",
                table: "RecurrentTasks");

            migrationBuilder.DropColumn(
                name: "Discriminator",
                table: "Actions");

            migrationBuilder.RenameColumn(
                name: "NotifsForActionPlans",
                table: "Leads",
                newName: "EventsForActionPlans");

            migrationBuilder.RenameColumn(
                name: "NotifsForActionPlans",
                table: "Brokers",
                newName: "ListenForActionPlans");

            migrationBuilder.RenameColumn(
                name: "DataId",
                table: "Actions",
                newName: "DataTemplateId");

            migrationBuilder.RenameColumn(
                name: "NotifsToListenTo",
                table: "ActionPlans",
                newName: "EventsToListenTo");

            migrationBuilder.AlterColumn<Guid>(
                name: "BrokerId",
                table: "RecurrentTasks",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AddColumn<bool>(
                name: "HasActionPlanToStop",
                table: "Leads",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "LastSeenNotifId",
                table: "Brokers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastSeenTimesTamp",
                table: "Brokers",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<bool>(
                name: "isSeen",
                table: "BrokerListingAssignments",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "ActionType",
                table: "Actions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "AppEvents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BrokerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LeadId = table.Column<int>(type: "int", nullable: true),
                    EventTimeStamp = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    EventType = table.Column<int>(type: "int", nullable: false),
                    ReadByBroker = table.Column<bool>(type: "bit", nullable: false),
                    NotifyBroker = table.Column<bool>(type: "bit", nullable: false),
                    DeleteAfterProcessing = table.Column<bool>(type: "bit", nullable: false),
                    IsActionPlanResult = table.Column<bool>(type: "bit", nullable: false),
                    ProcessingStatus = table.Column<int>(type: "int", nullable: false),
                    Props = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppEvents_Brokers_BrokerId",
                        column: x => x.BrokerId,
                        principalTable: "Brokers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AppEvents_Leads_LeadId",
                        column: x => x.LeadId,
                        principalTable: "Leads",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "EmailEvents",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    BrokerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BrokerEmail = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    LeadId = table.Column<int>(type: "int", nullable: true),
                    TimeReceived = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Seen = table.Column<bool>(type: "bit", nullable: false),
                    RepliedTo = table.Column<bool>(type: "bit", nullable: true),
                    NeedsAction = table.Column<bool>(type: "bit", nullable: true),
                    LeadParsedFromEmail = table.Column<bool>(type: "bit", nullable: false),
                    LeadProviderEmail = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmailEvents_Brokers_BrokerId",
                        column: x => x.BrokerId,
                        principalTable: "Brokers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EmailEvents_Leads_LeadId",
                        column: x => x.LeadId,
                        principalTable: "Leads",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Notifs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BrokerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LeadId = table.Column<int>(type: "int", nullable: true),
                    CreatedTimeStamp = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    NotifType = table.Column<int>(type: "int", nullable: false),
                    isSeen = table.Column<bool>(type: "bit", nullable: false),
                    priority = table.Column<byte>(type: "tinyint", nullable: false),
                    EventId = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Notifs_Brokers_BrokerId",
                        column: x => x.BrokerId,
                        principalTable: "Brokers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Notifs_Leads_LeadId",
                        column: x => x.LeadId,
                        principalTable: "Leads",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_AppEvents_BrokerId",
                table: "AppEvents",
                column: "BrokerId");

            migrationBuilder.CreateIndex(
                name: "IX_AppEvents_LeadId",
                table: "AppEvents",
                column: "LeadId");

            migrationBuilder.CreateIndex(
                name: "IX_EmailEvents_BrokerId",
                table: "EmailEvents",
                column: "BrokerId");

            migrationBuilder.CreateIndex(
                name: "IX_EmailEvents_LeadId",
                table: "EmailEvents",
                column: "LeadId");

            migrationBuilder.CreateIndex(
                name: "IX_Notifs_BrokerId",
                table: "Notifs",
                column: "BrokerId");

            migrationBuilder.CreateIndex(
                name: "IX_Notifs_LeadId",
                table: "Notifs",
                column: "LeadId");

            migrationBuilder.AddForeignKey(
                name: "FK_RecurrentTasks_Brokers_BrokerId",
                table: "RecurrentTasks",
                column: "BrokerId",
                principalTable: "Brokers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RecurrentTasks_Brokers_BrokerId",
                table: "RecurrentTasks");

            migrationBuilder.DropTable(
                name: "AppEvents");

            migrationBuilder.DropTable(
                name: "EmailEvents");

            migrationBuilder.DropTable(
                name: "Notifs");

            migrationBuilder.DropColumn(
                name: "HasActionPlanToStop",
                table: "Leads");

            migrationBuilder.DropColumn(
                name: "LastSeenNotifId",
                table: "Brokers");

            migrationBuilder.DropColumn(
                name: "LastSeenTimesTamp",
                table: "Brokers");

            migrationBuilder.DropColumn(
                name: "isSeen",
                table: "BrokerListingAssignments");

            migrationBuilder.DropColumn(
                name: "ActionType",
                table: "Actions");

            migrationBuilder.RenameColumn(
                name: "EventsForActionPlans",
                table: "Leads",
                newName: "NotifsForActionPlans");

            migrationBuilder.RenameColumn(
                name: "ListenForActionPlans",
                table: "Brokers",
                newName: "NotifsForActionPlans");

            migrationBuilder.RenameColumn(
                name: "DataTemplateId",
                table: "Actions",
                newName: "DataId");

            migrationBuilder.RenameColumn(
                name: "EventsToListenTo",
                table: "ActionPlans",
                newName: "NotifsToListenTo");

            migrationBuilder.AlterColumn<Guid>(
                name: "BrokerId",
                table: "RecurrentTasks",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastEmailToken",
                table: "RecurrentTasks",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Discriminator",
                table: "Actions",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "LeadInteractions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Discriminator = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LeadId = table.Column<int>(type: "int", nullable: false),
                    Timestamp = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    data = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    isRead = table.Column<bool>(type: "bit", nullable: false),
                    isReceived = table.Column<bool>(type: "bit", nullable: false),
                    type = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeadInteractions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LeadInteractions_Leads_LeadId",
                        column: x => x.LeadId,
                        principalTable: "Leads",
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
                    APHandlingStatus = table.Column<int>(type: "int", nullable: true),
                    DeleteAfterProcessing = table.Column<bool>(type: "bit", nullable: false),
                    EventTimeStamp = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    IsActionPlanResult = table.Column<bool>(type: "bit", nullable: false),
                    IsRecevied = table.Column<bool>(type: "bit", nullable: true),
                    NotifProps = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NotifType = table.Column<int>(type: "int", nullable: false),
                    NotifyBroker = table.Column<bool>(type: "bit", nullable: false),
                    ProcessingStatus = table.Column<int>(type: "int", nullable: false),
                    ReadByBroker = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Notifications_Brokers_BrokerId",
                        column: x => x.BrokerId,
                        principalTable: "Brokers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Notifications_Leads_LeadId",
                        column: x => x.LeadId,
                        principalTable: "Leads",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "TestBase",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Discriminator = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NextActionDelay = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    testJSON = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TestBase", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LeadInteractions_LeadId",
                table: "LeadInteractions",
                column: "LeadId");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_BrokerId",
                table: "Notifications",
                column: "BrokerId");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_LeadId",
                table: "Notifications",
                column: "LeadId");

            migrationBuilder.AddForeignKey(
                name: "FK_RecurrentTasks_Brokers_BrokerId",
                table: "RecurrentTasks",
                column: "BrokerId",
                principalTable: "Brokers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
