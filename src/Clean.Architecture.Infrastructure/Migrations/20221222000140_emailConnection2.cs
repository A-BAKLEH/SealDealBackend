using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Clean.Architecture.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class emailConnection2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Brokers_FirstConnectedEmail",
                table: "Brokers");

            migrationBuilder.DropColumn(
                name: "BrokerComment",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "NotifData",
                table: "Notifications");

            migrationBuilder.AlterColumn<string>(
                name: "FirstConnectedEmail",
                table: "Brokers",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

            migrationBuilder.CreateTable(
                name: "ConnectedEmails",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BrokerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmailNumber = table.Column<int>(type: "int", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    isMSFT = table.Column<bool>(type: "bit", nullable: false),
                    EmailStatus = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConnectedEmails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConnectedEmails_Brokers_BrokerId",
                        column: x => x.BrokerId,
                        principalTable: "Brokers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LeadInteractions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LeadId = table.Column<int>(type: "int", nullable: false),
                    type = table.Column<int>(type: "int", nullable: false),
                    isReceived = table.Column<bool>(type: "bit", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    isRead = table.Column<bool>(type: "bit", nullable: false),
                    data = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Discriminator = table.Column<string>(type: "nvarchar(max)", nullable: false)
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
                name: "EmailSubscriptions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ConnectedEmailId = table.Column<int>(type: "int", nullable: false),
                    FolderName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    GraphSubscriptionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SubscriptionWorking = table.Column<bool>(type: "bit", nullable: false),
                    HangfireRenewalJobId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SubsExpireTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FolderSyncToken = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastSyncTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FirstSyncTime = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailSubscriptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmailSubscriptions_ConnectedEmails_ConnectedEmailId",
                        column: x => x.ConnectedEmailId,
                        principalTable: "ConnectedEmails",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ConnectedEmails_BrokerId",
                table: "ConnectedEmails",
                column: "BrokerId");

            migrationBuilder.CreateIndex(
                name: "IX_EmailSubscriptions_ConnectedEmailId",
                table: "EmailSubscriptions",
                column: "ConnectedEmailId");

            migrationBuilder.CreateIndex(
                name: "IX_LeadInteractions_LeadId",
                table: "LeadInteractions",
                column: "LeadId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EmailSubscriptions");

            migrationBuilder.DropTable(
                name: "LeadInteractions");

            migrationBuilder.DropTable(
                name: "ConnectedEmails");

            migrationBuilder.AddColumn<string>(
                name: "BrokerComment",
                table: "Notifications",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NotifData",
                table: "Notifications",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "FirstConnectedEmail",
                table: "Brokers",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Brokers_FirstConnectedEmail",
                table: "Brokers",
                column: "FirstConnectedEmail");
        }
    }
}
