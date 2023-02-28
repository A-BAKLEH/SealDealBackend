using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class sdf2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BrokerListingAssignments_Brokers_BrokerId",
                table: "BrokerListingAssignments");

            migrationBuilder.DropForeignKey(
                name: "FK_BrokerListingAssignments_Listings_ListingId",
                table: "BrokerListingAssignments");

            migrationBuilder.AddForeignKey(
                name: "FK_BrokerListingAssignments_Brokers_BrokerId",
                table: "BrokerListingAssignments",
                column: "BrokerId",
                principalTable: "Brokers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_BrokerListingAssignments_Listings_ListingId",
                table: "BrokerListingAssignments",
                column: "ListingId",
                principalTable: "Listings",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BrokerListingAssignments_Brokers_BrokerId",
                table: "BrokerListingAssignments");

            migrationBuilder.DropForeignKey(
                name: "FK_BrokerListingAssignments_Listings_ListingId",
                table: "BrokerListingAssignments");

            migrationBuilder.AddForeignKey(
                name: "FK_BrokerListingAssignments_Brokers_BrokerId",
                table: "BrokerListingAssignments",
                column: "BrokerId",
                principalTable: "Brokers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_BrokerListingAssignments_Listings_ListingId",
                table: "BrokerListingAssignments",
                column: "ListingId",
                principalTable: "Listings",
                principalColumn: "Id");
        }
    }
}
