using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class asdadasdasd : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Title",
                table: "ActionPlans",
                newName: "Name");

            migrationBuilder.AddColumn<int>(
                name: "NotifsForActionPlans",
                table: "Leads",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<int>(
                name: "NotifsForActionPlans",
                table: "Brokers",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NotifsForActionPlans",
                table: "Leads");

            migrationBuilder.RenameColumn(
                name: "Name",
                table: "ActionPlans",
                newName: "Title");

            migrationBuilder.AlterColumn<int>(
                name: "NotifsForActionPlans",
                table: "Brokers",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");
        }
    }
}
