using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class hello : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Address_City",
                table: "Listings");

            migrationBuilder.DropColumn(
                name: "Address_Country",
                table: "Listings");

            migrationBuilder.DropColumn(
                name: "Address_PostalCode",
                table: "Listings");

            migrationBuilder.DropColumn(
                name: "Address_ProvinceState",
                table: "Listings");

            migrationBuilder.DropColumn(
                name: "Address_City",
                table: "Agencies");

            migrationBuilder.DropColumn(
                name: "Address_Country",
                table: "Agencies");

            migrationBuilder.DropColumn(
                name: "Address_PostalCode",
                table: "Agencies");

            migrationBuilder.DropColumn(
                name: "Address_ProvinceState",
                table: "Agencies");

            migrationBuilder.RenameColumn(
                name: "Address_StreetAddress",
                table: "Listings",
                newName: "Address");

            migrationBuilder.RenameColumn(
                name: "Address_StreetAddress",
                table: "Agencies",
                newName: "Address");

            migrationBuilder.AddColumn<int>(
                name: "APHandlingStatus",
                table: "Notifications",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HangfireJobId",
                table: "Notifications",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "APHandlingStatus",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "HangfireJobId",
                table: "Notifications");

            migrationBuilder.RenameColumn(
                name: "Address",
                table: "Listings",
                newName: "Address_StreetAddress");

            migrationBuilder.RenameColumn(
                name: "Address",
                table: "Agencies",
                newName: "Address_StreetAddress");

            migrationBuilder.AddColumn<string>(
                name: "Address_City",
                table: "Listings",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Address_Country",
                table: "Listings",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Address_PostalCode",
                table: "Listings",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Address_ProvinceState",
                table: "Listings",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Address_City",
                table: "Agencies",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Address_Country",
                table: "Agencies",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Address_PostalCode",
                table: "Agencies",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Address_ProvinceState",
                table: "Agencies",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
