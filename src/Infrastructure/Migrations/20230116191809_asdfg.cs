using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class asdfg : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HangfireJobId",
                table: "Notifications");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_BrokerId",
                table: "Notifications",
                column: "BrokerId");

            migrationBuilder.AddForeignKey(
                name: "FK_Notifications_Brokers_BrokerId",
                table: "Notifications",
                column: "BrokerId",
                principalTable: "Brokers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Notifications_Brokers_BrokerId",
                table: "Notifications");

            migrationBuilder.DropIndex(
                name: "IX_Notifications_BrokerId",
                table: "Notifications");

            migrationBuilder.AddColumn<string>(
                name: "HangfireJobId",
                table: "Notifications",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
