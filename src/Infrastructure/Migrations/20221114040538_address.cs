using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class address : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Address",
                table: "Listings",
                newName: "Address_Street");

            migrationBuilder.AddColumn<string>(
                name: "Address_AppartmentNo",
                table: "Listings",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Address_BuildingNumber",
                table: "Listings",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Address_AppartmentNo",
                table: "Listings");

            migrationBuilder.DropColumn(
                name: "Address_BuildingNumber",
                table: "Listings");

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

            migrationBuilder.RenameColumn(
                name: "Address_Street",
                table: "Listings",
                newName: "Address");
        }
    }
}
