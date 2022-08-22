using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Clean.Architecture.Infrastructure.Migrations
{
    public partial class lol : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CheckoutSessions");

            migrationBuilder.DropColumn(
                name: "SoloBroker",
                table: "Agencies");

            migrationBuilder.RenameColumn(
                name: "AgencyStatus",
                table: "Agencies",
                newName: "StripeSubscriptionStatus");

            migrationBuilder.AddColumn<string>(
                name: "LastCheckoutSessionID",
                table: "Agencies",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "NumberOfBrokersInDatabase",
                table: "Agencies",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "SubscriptionLastValidDate",
                table: "Agencies",
                type: "date",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastCheckoutSessionID",
                table: "Agencies");

            migrationBuilder.DropColumn(
                name: "NumberOfBrokersInDatabase",
                table: "Agencies");

            migrationBuilder.DropColumn(
                name: "SubscriptionLastValidDate",
                table: "Agencies");

            migrationBuilder.RenameColumn(
                name: "StripeSubscriptionStatus",
                table: "Agencies",
                newName: "AgencyStatus");

            migrationBuilder.AddColumn<bool>(
                name: "SoloBroker",
                table: "Agencies",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "CheckoutSessions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AgencyId = table.Column<int>(type: "int", nullable: false),
                    BrokerId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CheckoutSessionStatus = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SessionEndAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SessionStartAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    StripeCheckoutSessionId = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CheckoutSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CheckoutSessions_Agencies_AgencyId",
                        column: x => x.AgencyId,
                        principalTable: "Agencies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CheckoutSessions_Brokers_BrokerId",
                        column: x => x.BrokerId,
                        principalTable: "Brokers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_CheckoutSessions_AgencyId",
                table: "CheckoutSessions",
                column: "AgencyId");

            migrationBuilder.CreateIndex(
                name: "IX_CheckoutSessions_BrokerId",
                table: "CheckoutSessions",
                column: "BrokerId");
        }
    }
}
