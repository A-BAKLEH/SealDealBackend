using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Email69lol : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EmailSubscriptions");

            migrationBuilder.DropColumn(
                name: "ConnectedEmailStatus",
                table: "Brokers");

            migrationBuilder.DropColumn(
                name: "FirstConnectedEmail",
                table: "Brokers");

            migrationBuilder.AddColumn<DateTime>(
                name: "FirstSync",
                table: "ConnectedEmails",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<Guid>(
                name: "GraphSubscriptionId",
                table: "ConnectedEmails",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<DateTime>(
                name: "LastSync",
                table: "ConnectedEmails",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "SubsExpiryDate",
                table: "ConnectedEmails",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "SubsRenewalJobId",
                table: "ConnectedEmails",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "SyncScheduled",
                table: "ConnectedEmails",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "FolderSyncs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ConnectedEmailId = table.Column<int>(type: "int", nullable: false),
                    FolderId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FolderName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DeltaToken = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FolderSyncs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FolderSyncs_ConnectedEmails_ConnectedEmailId",
                        column: x => x.ConnectedEmailId,
                        principalTable: "ConnectedEmails",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ConnectedEmails_GraphSubscriptionId",
                table: "ConnectedEmails",
                column: "GraphSubscriptionId");

            migrationBuilder.CreateIndex(
                name: "IX_FolderSyncs_ConnectedEmailId",
                table: "FolderSyncs",
                column: "ConnectedEmailId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FolderSyncs");

            migrationBuilder.DropIndex(
                name: "IX_ConnectedEmails_GraphSubscriptionId",
                table: "ConnectedEmails");

            migrationBuilder.DropColumn(
                name: "FirstSync",
                table: "ConnectedEmails");

            migrationBuilder.DropColumn(
                name: "GraphSubscriptionId",
                table: "ConnectedEmails");

            migrationBuilder.DropColumn(
                name: "LastSync",
                table: "ConnectedEmails");

            migrationBuilder.DropColumn(
                name: "SubsExpiryDate",
                table: "ConnectedEmails");

            migrationBuilder.DropColumn(
                name: "SubsRenewalJobId",
                table: "ConnectedEmails");

            migrationBuilder.DropColumn(
                name: "SyncScheduled",
                table: "ConnectedEmails");

            migrationBuilder.AddColumn<int>(
                name: "ConnectedEmailStatus",
                table: "Brokers",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FirstConnectedEmail",
                table: "Brokers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "EmailSubscriptions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ConnectedEmailId = table.Column<int>(type: "int", nullable: false),
                    FirstSyncTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FolderName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FolderSyncToken = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    GraphSubscriptionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HangfireRenewalJobId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastSyncTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SubsExpireTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SubscriptionWorking = table.Column<bool>(type: "bit", nullable: false)
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
                name: "IX_EmailSubscriptions_ConnectedEmailId",
                table: "EmailSubscriptions",
                column: "ConnectedEmailId");
        }
    }
}
