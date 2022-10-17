using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Clean.Architecture.Infrastructure.Migrations
{
    public partial class templates : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SmsTemplates_Brokers_BrokerId",
                table: "SmsTemplates");

            migrationBuilder.DropTable(
                name: "EmailTemplates");

            migrationBuilder.DropPrimaryKey(
                name: "PK_SmsTemplates",
                table: "SmsTemplates");

            migrationBuilder.RenameTable(
                name: "SmsTemplates",
                newName: "Templates");

            migrationBuilder.RenameColumn(
                name: "TemplateText",
                table: "Templates",
                newName: "templateText");

            migrationBuilder.RenameIndex(
                name: "IX_SmsTemplates_BrokerId",
                table: "Templates",
                newName: "IX_Templates_BrokerId");

            migrationBuilder.AddColumn<string>(
                name: "Discriminator",
                table: "Templates",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "EmailTemplateSubject",
                table: "Templates",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "Modified",
                table: "Templates",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "TimesUsed",
                table: "Templates",
                type: "int",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_Templates",
                table: "Templates",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Templates_Brokers_BrokerId",
                table: "Templates",
                column: "BrokerId",
                principalTable: "Brokers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Templates_Brokers_BrokerId",
                table: "Templates");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Templates",
                table: "Templates");

            migrationBuilder.DropColumn(
                name: "Discriminator",
                table: "Templates");

            migrationBuilder.DropColumn(
                name: "EmailTemplateSubject",
                table: "Templates");

            migrationBuilder.DropColumn(
                name: "Modified",
                table: "Templates");

            migrationBuilder.DropColumn(
                name: "TimesUsed",
                table: "Templates");

            migrationBuilder.RenameTable(
                name: "Templates",
                newName: "SmsTemplates");

            migrationBuilder.RenameColumn(
                name: "templateText",
                table: "SmsTemplates",
                newName: "TemplateText");

            migrationBuilder.RenameIndex(
                name: "IX_Templates_BrokerId",
                table: "SmsTemplates",
                newName: "IX_SmsTemplates_BrokerId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_SmsTemplates",
                table: "SmsTemplates",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "EmailTemplates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BrokerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmailTemplateSubject = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EmailTemplateText = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailTemplates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmailTemplates_Brokers_BrokerId",
                        column: x => x.BrokerId,
                        principalTable: "Brokers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EmailTemplates_BrokerId",
                table: "EmailTemplates",
                column: "BrokerId");

            migrationBuilder.AddForeignKey(
                name: "FK_SmsTemplates_Brokers_BrokerId",
                table: "SmsTemplates",
                column: "BrokerId",
                principalTable: "Brokers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
