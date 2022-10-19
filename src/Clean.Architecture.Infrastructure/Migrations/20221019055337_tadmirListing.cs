using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Clean.Architecture.Infrastructure.Migrations
{
    public partial class tadmirListing : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Listings_Brokers_BrokerId",
                table: "Listings");

            migrationBuilder.DropTable(
                name: "LeadListing");

            migrationBuilder.DropIndex(
                name: "IX_Listings_BrokerId",
                table: "Listings");

            migrationBuilder.DropColumn(
                name: "BrokerId",
                table: "Listings");

            migrationBuilder.AddColumn<int>(
                name: "AssignedBrokersCount",
                table: "Listings",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ListingId",
                table: "Leads",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "BrokerListingAssignments",
                columns: table => new
                {
                    BrokerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ListingId = table.Column<int>(type: "int", nullable: false),
                    assignmentDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BrokerListingAssignments", x => new { x.BrokerId, x.ListingId });
                    table.ForeignKey(
                        name: "FK_BrokerListingAssignments_Brokers_BrokerId",
                        column: x => x.BrokerId,
                        principalTable: "Brokers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BrokerListingAssignments_Listings_ListingId",
                        column: x => x.ListingId,
                        principalTable: "Listings",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Leads_ListingId",
                table: "Leads",
                column: "ListingId");

            migrationBuilder.CreateIndex(
                name: "IX_BrokerListingAssignments_ListingId",
                table: "BrokerListingAssignments",
                column: "ListingId");

            migrationBuilder.AddForeignKey(
                name: "FK_Leads_Listings_ListingId",
                table: "Leads",
                column: "ListingId",
                principalTable: "Listings",
                principalColumn: "Id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Leads_Listings_ListingId",
                table: "Leads");

            migrationBuilder.DropTable(
                name: "BrokerListingAssignments");

            migrationBuilder.DropIndex(
                name: "IX_Leads_ListingId",
                table: "Leads");

            migrationBuilder.DropColumn(
                name: "AssignedBrokersCount",
                table: "Listings");

            migrationBuilder.DropColumn(
                name: "ListingId",
                table: "Leads");

            migrationBuilder.AddColumn<Guid>(
                name: "BrokerId",
                table: "Listings",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "LeadListing",
                columns: table => new
                {
                    LeadId = table.Column<int>(type: "int", nullable: false),
                    ListingId = table.Column<int>(type: "int", nullable: false),
                    ClientComments = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeadListing", x => new { x.LeadId, x.ListingId });
                    table.ForeignKey(
                        name: "FK_LeadListing_Leads_LeadId",
                        column: x => x.LeadId,
                        principalTable: "Leads",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LeadListing_Listings_ListingId",
                        column: x => x.ListingId,
                        principalTable: "Listings",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Listings_BrokerId",
                table: "Listings",
                column: "BrokerId");

            migrationBuilder.CreateIndex(
                name: "IX_LeadListing_ListingId",
                table: "LeadListing",
                column: "ListingId");

            migrationBuilder.AddForeignKey(
                name: "FK_Listings_Brokers_BrokerId",
                table: "Listings",
                column: "BrokerId",
                principalTable: "Brokers",
                principalColumn: "Id");
        }
    }
}
