using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Clean.Architecture.Infrastructure.Migrations
{
    public partial class lol3 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "UnderlyingEventTimestamp",
                table: "Notifications",
                newName: "UnderlyingEventTimeStamp");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "UnderlyingEventTimeStamp",
                table: "Notifications",
                newName: "UnderlyingEventTimestamp");
        }
    }
}
