using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AiNurturing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AINurturings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    LeadId = table.Column<int>(type: "integer", nullable: false),
                    BrokerId = table.Column<Guid>(type: "uuid", nullable: false),
                    TimeCreated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    AnalysisStatus = table.Column<int>(type: "integer", nullable: false),
                    QuestionsCount = table.Column<int>(type: "integer", nullable: false),
                    FollowUpCount = table.Column<int>(type: "integer", nullable: false),
                    LastReplyDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastFollowupDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ThreadId = table.Column<string>(type: "text", nullable: false),
                    LastProcessedMessageTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    InitialMessageSent = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AINurturings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AINurturings_Brokers_BrokerId",
                        column: x => x.BrokerId,
                        principalTable: "Brokers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AINurturings_Leads_LeadId",
                        column: x => x.LeadId,
                        principalTable: "Leads",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AINurturings_BrokerId",
                table: "AINurturings",
                column: "BrokerId");

            migrationBuilder.CreateIndex(
                name: "IX_AINurturings_LeadId",
                table: "AINurturings",
                column: "LeadId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AINurturings");
        }
    }
}
