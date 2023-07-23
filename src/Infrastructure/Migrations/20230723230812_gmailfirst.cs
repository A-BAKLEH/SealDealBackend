using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class gmailfirst : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "GmailAccessToken",
                table: "ConnectedEmails",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GmailRefreshToken",
                table: "ConnectedEmails",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GmailTokenRefreshJobId",
                table: "ConnectedEmails",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GmailAccessToken",
                table: "ConnectedEmails");

            migrationBuilder.DropColumn(
                name: "GmailRefreshToken",
                table: "ConnectedEmails");

            migrationBuilder.DropColumn(
                name: "GmailTokenRefreshJobId",
                table: "ConnectedEmails");
        }
    }
}
