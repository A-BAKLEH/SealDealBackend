using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Clean.Architecture.Infrastructure.Migrations
{
    public partial class rehaul : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CheckoutSessions_Brokers_BrokerId",
                table: "CheckoutSessions");

            migrationBuilder.DropColumn(
                name: "IsCompleted",
                table: "CheckoutSessions");

            migrationBuilder.DropColumn(
                name: "IsPaying",
                table: "Agencies");

            migrationBuilder.AlterColumn<string>(
                name: "LeadStatus",
                table: "Leads",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<Guid>(
                name: "BrokerId",
                table: "CheckoutSessions",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AddColumn<int>(
                name: "AgencyId",
                table: "CheckoutSessions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "CheckoutSessionStatus",
                table: "CheckoutSessions",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "AccountActive",
                table: "Brokers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "AgencyStatus",
                table: "Agencies",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "NumberOfBrokersInSubscription",
                table: "Agencies",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_CheckoutSessions_AgencyId",
                table: "CheckoutSessions",
                column: "AgencyId");

            migrationBuilder.AddForeignKey(
                name: "FK_CheckoutSessions_Agencies_AgencyId",
                table: "CheckoutSessions",
                column: "AgencyId",
                principalTable: "Agencies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_CheckoutSessions_Brokers_BrokerId",
                table: "CheckoutSessions",
                column: "BrokerId",
                principalTable: "Brokers",
                principalColumn: "Id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CheckoutSessions_Agencies_AgencyId",
                table: "CheckoutSessions");

            migrationBuilder.DropForeignKey(
                name: "FK_CheckoutSessions_Brokers_BrokerId",
                table: "CheckoutSessions");

            migrationBuilder.DropIndex(
                name: "IX_CheckoutSessions_AgencyId",
                table: "CheckoutSessions");

            migrationBuilder.DropColumn(
                name: "AgencyId",
                table: "CheckoutSessions");

            migrationBuilder.DropColumn(
                name: "CheckoutSessionStatus",
                table: "CheckoutSessions");

            migrationBuilder.DropColumn(
                name: "AccountActive",
                table: "Brokers");

            migrationBuilder.DropColumn(
                name: "AgencyStatus",
                table: "Agencies");

            migrationBuilder.DropColumn(
                name: "NumberOfBrokersInSubscription",
                table: "Agencies");

            migrationBuilder.AlterColumn<int>(
                name: "LeadStatus",
                table: "Leads",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<Guid>(
                name: "BrokerId",
                table: "CheckoutSessions",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsCompleted",
                table: "CheckoutSessions",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsPaying",
                table: "Agencies",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddForeignKey(
                name: "FK_CheckoutSessions_Brokers_BrokerId",
                table: "CheckoutSessions",
                column: "BrokerId",
                principalTable: "Brokers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
