using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class lang : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_LeadEmails",
                table: "LeadEmails");

            migrationBuilder.RenameColumn(
                name: "Languge",
                table: "Leads",
                newName: "Language");

            migrationBuilder.RenameColumn(
                name: "Languge",
                table: "Brokers",
                newName: "Language");

            migrationBuilder.AddPrimaryKey(
                name: "PK_LeadEmails",
                table: "LeadEmails",
                columns: new[] { "EmailAddress", "LeadId" });

            migrationBuilder.CreateIndex(
                name: "IX_LeadEmails_LeadId",
                table: "LeadEmails",
                column: "LeadId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_LeadEmails",
                table: "LeadEmails");

            migrationBuilder.DropIndex(
                name: "IX_LeadEmails_LeadId",
                table: "LeadEmails");

            migrationBuilder.RenameColumn(
                name: "Language",
                table: "Leads",
                newName: "Languge");

            migrationBuilder.RenameColumn(
                name: "Language",
                table: "Brokers",
                newName: "Languge");

            migrationBuilder.AddPrimaryKey(
                name: "PK_LeadEmails",
                table: "LeadEmails",
                columns: new[] { "LeadId", "EmailAddress" });
        }
    }
}
