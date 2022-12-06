using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Clean.Architecture.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class emailConnection : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "FirstConnectedEmail",
                table: "Brokers",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AddColumn<int>(
                name: "ConnectedEmailStatus",
                table: "Brokers",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AzureTenantID",
                table: "Agencies",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "HasAdminEmailConsent",
                table: "Agencies",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ConnectedEmailStatus",
                table: "Brokers");

            migrationBuilder.DropColumn(
                name: "AzureTenantID",
                table: "Agencies");

            migrationBuilder.DropColumn(
                name: "HasAdminEmailConsent",
                table: "Agencies");

            migrationBuilder.AlterColumn<string>(
                name: "FirstConnectedEmail",
                table: "Brokers",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);
        }
    }
}
