using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class emailzz : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FolderSyncs");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ConnectedEmails",
                table: "ConnectedEmails");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "ConnectedEmails");

            migrationBuilder.DropColumn(
                name: "EmailStatus",
                table: "ConnectedEmails");

            migrationBuilder.AlterColumn<string>(
                name: "SubsRenewalJobId",
                table: "ConnectedEmails",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "SubsExpiryDate",
                table: "ConnectedEmails",
                type: "datetimeoffset",
                nullable: true,
                oldClrType: typeof(DateTimeOffset),
                oldType: "datetimeoffset");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "LastSync",
                table: "ConnectedEmails",
                type: "datetimeoffset",
                nullable: true,
                oldClrType: typeof(DateTimeOffset),
                oldType: "datetimeoffset");

            migrationBuilder.AlterColumn<Guid>(
                name: "GraphSubscriptionId",
                table: "ConnectedEmails",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "FirstSync",
                table: "ConnectedEmails",
                type: "datetimeoffset",
                nullable: true,
                oldClrType: typeof(DateTimeOffset),
                oldType: "datetimeoffset");

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "ConnectedEmails",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<bool>(
                name: "hasAdminConsent",
                table: "ConnectedEmails",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "tenantId",
                table: "ConnectedEmails",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ConnectedEmails",
                table: "ConnectedEmails",
                column: "Email");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_ConnectedEmails",
                table: "ConnectedEmails");

            migrationBuilder.DropColumn(
                name: "hasAdminConsent",
                table: "ConnectedEmails");

            migrationBuilder.DropColumn(
                name: "tenantId",
                table: "ConnectedEmails");

            migrationBuilder.AlterColumn<string>(
                name: "SubsRenewalJobId",
                table: "ConnectedEmails",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "SubsExpiryDate",
                table: "ConnectedEmails",
                type: "datetimeoffset",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)),
                oldClrType: typeof(DateTimeOffset),
                oldType: "datetimeoffset",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "LastSync",
                table: "ConnectedEmails",
                type: "datetimeoffset",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)),
                oldClrType: typeof(DateTimeOffset),
                oldType: "datetimeoffset",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "GraphSubscriptionId",
                table: "ConnectedEmails",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "FirstSync",
                table: "ConnectedEmails",
                type: "datetimeoffset",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)),
                oldClrType: typeof(DateTimeOffset),
                oldType: "datetimeoffset",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "ConnectedEmails",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AddColumn<int>(
                name: "Id",
                table: "ConnectedEmails",
                type: "int",
                nullable: false,
                defaultValue: 0)
                .Annotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AddColumn<int>(
                name: "EmailStatus",
                table: "ConnectedEmails",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddPrimaryKey(
                name: "PK_ConnectedEmails",
                table: "ConnectedEmails",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "FolderSyncs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ConnectedEmailId = table.Column<int>(type: "int", nullable: false),
                    DeltaToken = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FolderId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FolderName = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FolderSyncs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FolderSyncs_ConnectedEmails_ConnectedEmailId",
                        column: x => x.ConnectedEmailId,
                        principalTable: "ConnectedEmails",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FolderSyncs_ConnectedEmailId",
                table: "FolderSyncs",
                column: "ConnectedEmailId");
        }
    }
}
