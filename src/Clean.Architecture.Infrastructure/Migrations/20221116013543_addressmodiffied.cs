using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Clean.Architecture.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class addressmodiffied : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Address_AppartmentNo",
                table: "Listings");

            migrationBuilder.DropColumn(
                name: "Address_BuildingNumber",
                table: "Listings");

            migrationBuilder.RenameColumn(
                name: "Address_Street",
                table: "Listings",
                newName: "Address_StreetAddress");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Address_StreetAddress",
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
        }
    }
}
