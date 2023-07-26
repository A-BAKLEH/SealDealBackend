using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class gmail2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "GmailTokenRefreshJobId",
                table: "ConnectedEmails",
                newName: "historyId");

            migrationBuilder.RenameColumn(
                name: "GmailRefreshToken",
                table: "ConnectedEmails",
                newName: "TokenRefreshJobId");

            migrationBuilder.RenameColumn(
                name: "GmailAccessToken",
                table: "ConnectedEmails",
                newName: "RefreshToken");

            migrationBuilder.AddColumn<string>(
                name: "AccessToken",
                table: "ConnectedEmails",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AccessToken",
                table: "ConnectedEmails");

            migrationBuilder.RenameColumn(
                name: "historyId",
                table: "ConnectedEmails",
                newName: "GmailTokenRefreshJobId");

            migrationBuilder.RenameColumn(
                name: "TokenRefreshJobId",
                table: "ConnectedEmails",
                newName: "GmailRefreshToken");

            migrationBuilder.RenameColumn(
                name: "RefreshToken",
                table: "ConnectedEmails",
                newName: "GmailAccessToken");
        }
    }
}
