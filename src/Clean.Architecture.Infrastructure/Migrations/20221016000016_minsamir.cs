using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Clean.Architecture.Infrastructure.Migrations
{
    public partial class minsamir : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Notes_LeadId",
                table: "Notes");

            migrationBuilder.AddColumn<string>(
                name: "Areas",
                table: "Leads",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "leadSourceDetails",
                table: "Leads",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "leadType",
                table: "Leads",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "source",
                table: "Leads",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Notes_LeadId",
                table: "Notes",
                column: "LeadId",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Notes_LeadId",
                table: "Notes");

            migrationBuilder.DropColumn(
                name: "Areas",
                table: "Leads");

            migrationBuilder.DropColumn(
                name: "leadSourceDetails",
                table: "Leads");

            migrationBuilder.DropColumn(
                name: "leadType",
                table: "Leads");

            migrationBuilder.DropColumn(
                name: "source",
                table: "Leads");

            migrationBuilder.CreateIndex(
                name: "IX_Notes_LeadId",
                table: "Notes",
                column: "LeadId");
        }
    }
}
