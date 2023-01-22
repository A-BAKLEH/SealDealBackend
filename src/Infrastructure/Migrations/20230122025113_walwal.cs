using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class walwal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "leadSourceDetails",
                table: "Leads");

            migrationBuilder.AddColumn<string>(
                name: "SourceDetails",
                table: "Leads",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SourceDetails",
                table: "Leads");

            migrationBuilder.AddColumn<string>(
                name: "leadSourceDetails",
                table: "Leads",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
