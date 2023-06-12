using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class dammer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Agencies",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AgencyName = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Address_StreetAddress = table.Column<string>(type: "text", nullable: true),
                    Address_apt = table.Column<string>(type: "text", nullable: true),
                    Address_City = table.Column<string>(type: "text", nullable: true),
                    Address_ProvinceState = table.Column<string>(type: "text", nullable: true),
                    Address_Country = table.Column<string>(type: "text", nullable: true),
                    Address_PostalCode = table.Column<string>(type: "text", nullable: true),
                    PhoneNumber = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    SignupDateTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AdminStripeId = table.Column<string>(type: "text", nullable: true),
                    StripeSubscriptionId = table.Column<string>(type: "text", nullable: true),
                    SubscriptionLastValidDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    NumberOfBrokersInSubscription = table.Column<int>(type: "integer", nullable: false),
                    StripeSubscriptionStatus = table.Column<string>(type: "text", nullable: false),
                    HasAdminEmailConsent = table.Column<bool>(type: "boolean", nullable: false),
                    AzureTenantID = table.Column<string>(type: "text", nullable: true),
                    LastCheckoutSessionID = table.Column<string>(type: "text", nullable: true),
                    NumberOfBrokersInDatabase = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Agencies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Areas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AgencyId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    PostalCode = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Areas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Areas_Agencies_AgencyId",
                        column: x => x.AgencyId,
                        principalTable: "Agencies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Brokers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AgencyId = table.Column<int>(type: "integer", nullable: false),
                    FirstName = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    LastName = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Language = table.Column<int>(type: "integer", nullable: false),
                    TimeZoneId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    TempTimeZone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    isSolo = table.Column<bool>(type: "boolean", nullable: false),
                    isAdmin = table.Column<bool>(type: "boolean", nullable: false),
                    AccountActive = table.Column<bool>(type: "boolean", nullable: false),
                    PhoneNumber = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    LoginEmail = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ListenForActionPlans = table.Column<int>(type: "integer", nullable: false),
                    MarkEmailsRead = table.Column<bool>(type: "boolean", nullable: false),
                    LastSeenAppEventId = table.Column<int>(type: "integer", nullable: false),
                    AppEventAnalyzerLastId = table.Column<int>(type: "integer", nullable: false),
                    LastUnassignedLeadIdAnalyzed = table.Column<int>(type: "integer", nullable: false),
                    EmailEventAnalyzerLastTimestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Brokers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Brokers_Agencies_AgencyId",
                        column: x => x.AgencyId,
                        principalTable: "Agencies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Listings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AgencyId = table.Column<int>(type: "integer", nullable: false),
                    Address_StreetAddress = table.Column<string>(type: "text", nullable: false),
                    Address_apt = table.Column<string>(type: "text", nullable: false),
                    Address_City = table.Column<string>(type: "text", nullable: false),
                    Address_ProvinceState = table.Column<string>(type: "text", nullable: false),
                    Address_Country = table.Column<string>(type: "text", nullable: false),
                    Address_PostalCode = table.Column<string>(type: "text", nullable: false),
                    FormattedStreetAddress = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    DateOfListing = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Price = table.Column<int>(type: "integer", nullable: false),
                    AssignedBrokersCount = table.Column<byte>(type: "smallint", nullable: false),
                    URL = table.Column<string>(type: "text", nullable: true),
                    LeadsGeneratedCount = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Listings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Listings_Agencies_AgencyId",
                        column: x => x.AgencyId,
                        principalTable: "Agencies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ActionPlans",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Triggers = table.Column<int>(type: "integer", nullable: false),
                    EventsToListenTo = table.Column<int>(type: "integer", nullable: false),
                    StopPlanOnInteraction = table.Column<bool>(type: "boolean", nullable: false),
                    BrokerId = table.Column<Guid>(type: "uuid", nullable: false),
                    TimeCreated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    isActive = table.Column<bool>(type: "boolean", nullable: false),
                    ActionsCount = table.Column<byte>(type: "smallint", nullable: false),
                    Name = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    AssignToLead = table.Column<bool>(type: "boolean", nullable: false),
                    FirstActionDelay = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActionPlans", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ActionPlans_Brokers_BrokerId",
                        column: x => x.BrokerId,
                        principalTable: "Brokers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ConnectedEmails",
                columns: table => new
                {
                    Email = table.Column<string>(type: "text", nullable: false),
                    BrokerId = table.Column<Guid>(type: "uuid", nullable: false),
                    EmailNumber = table.Column<byte>(type: "smallint", nullable: false),
                    hasAdminConsent = table.Column<bool>(type: "boolean", nullable: false),
                    tenantId = table.Column<string>(type: "text", nullable: false),
                    AssignLeadsAuto = table.Column<bool>(type: "boolean", nullable: false),
                    isMSFT = table.Column<bool>(type: "boolean", nullable: false),
                    GraphSubscriptionId = table.Column<Guid>(type: "uuid", nullable: true),
                    SubsExpiryDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SubsRenewalJobId = table.Column<string>(type: "text", nullable: true),
                    SyncScheduled = table.Column<bool>(type: "boolean", nullable: false),
                    SyncJobId = table.Column<string>(type: "text", nullable: true),
                    LastSync = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FirstSync = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    OpenAITokensUsed = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConnectedEmails", x => x.Email);
                    table.ForeignKey(
                        name: "FK_ConnectedEmails_Brokers_BrokerId",
                        column: x => x.BrokerId,
                        principalTable: "Brokers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RecurrentTasks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BrokerId = table.Column<Guid>(type: "uuid", nullable: true),
                    HangfireTaskId = table.Column<string>(type: "text", nullable: true),
                    NextScheduledTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    taskStatus = table.Column<int>(type: "integer", nullable: false),
                    Discriminator = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecurrentTasks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RecurrentTasks_Brokers_BrokerId",
                        column: x => x.BrokerId,
                        principalTable: "Brokers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Tags",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TagName = table.Column<string>(type: "character varying(25)", maxLength: 25, nullable: false),
                    BrokerId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tags", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Tags_Brokers_BrokerId",
                        column: x => x.BrokerId,
                        principalTable: "Brokers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Templates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    templateText = table.Column<string>(type: "text", nullable: false),
                    BrokerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Modified = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TimesUsed = table.Column<int>(type: "integer", nullable: true),
                    Title = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Discriminator = table.Column<string>(type: "text", nullable: false),
                    EmailTemplateSubject = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Templates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Templates_Brokers_BrokerId",
                        column: x => x.BrokerId,
                        principalTable: "Brokers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BrokerListingAssignments",
                columns: table => new
                {
                    BrokerId = table.Column<Guid>(type: "uuid", nullable: false),
                    ListingId = table.Column<int>(type: "integer", nullable: false),
                    assignmentDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    isSeen = table.Column<bool>(type: "boolean", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BrokerListingAssignments", x => new { x.BrokerId, x.ListingId });
                    table.ForeignKey(
                        name: "FK_BrokerListingAssignments_Brokers_BrokerId",
                        column: x => x.BrokerId,
                        principalTable: "Brokers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_BrokerListingAssignments_Listings_ListingId",
                        column: x => x.ListingId,
                        principalTable: "Listings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Leads",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AgencyId = table.Column<int>(type: "integer", nullable: false),
                    verifyEmailAddress = table.Column<bool>(type: "boolean", nullable: false),
                    LeadFirstName = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    LeadLastName = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    PhoneNumber = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    Budget = table.Column<int>(type: "integer", nullable: true),
                    Language = table.Column<int>(type: "integer", nullable: false),
                    EventsForActionPlans = table.Column<int>(type: "integer", nullable: false),
                    HasActionPlanToStop = table.Column<bool>(type: "boolean", nullable: false),
                    LastNotifsViewedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EntryDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    source = table.Column<int>(type: "integer", nullable: false),
                    leadType = table.Column<int>(type: "integer", nullable: false),
                    SourceDetails = table.Column<string>(type: "text", nullable: false),
                    LeadStatus = table.Column<string>(type: "text", nullable: false),
                    Areas = table.Column<string>(type: "text", nullable: true),
                    BrokerId = table.Column<Guid>(type: "uuid", nullable: true),
                    ListingId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Leads", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Leads_Agencies_AgencyId",
                        column: x => x.AgencyId,
                        principalTable: "Agencies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Leads_Brokers_BrokerId",
                        column: x => x.BrokerId,
                        principalTable: "Brokers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Leads_Listings_ListingId",
                        column: x => x.ListingId,
                        principalTable: "Listings",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Actions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ActionPlanId = table.Column<int>(type: "integer", nullable: false),
                    ActionType = table.Column<int>(type: "integer", nullable: false),
                    ActionLevel = table.Column<byte>(type: "smallint", nullable: false),
                    ActionProperties = table.Column<string>(type: "text", nullable: false),
                    DataTemplateId = table.Column<int>(type: "integer", nullable: true),
                    NextActionDelay = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Actions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Actions_ActionPlans_ActionPlanId",
                        column: x => x.ActionPlanId,
                        principalTable: "ActionPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ActionPlanAssociations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CustomDelay = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    ActionPlanId = table.Column<int>(type: "integer", nullable: true),
                    TriggeredManually = table.Column<bool>(type: "boolean", nullable: false),
                    LeadId = table.Column<int>(type: "integer", nullable: false),
                    ActionPlanTriggeredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ThisActionPlanStatus = table.Column<int>(type: "integer", nullable: false),
                    currentTrackedActionId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActionPlanAssociations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ActionPlanAssociations_ActionPlans_ActionPlanId",
                        column: x => x.ActionPlanId,
                        principalTable: "ActionPlans",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ActionPlanAssociations_Leads_LeadId",
                        column: x => x.LeadId,
                        principalTable: "Leads",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AppEvents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BrokerId = table.Column<Guid>(type: "uuid", nullable: false),
                    LeadId = table.Column<int>(type: "integer", nullable: true),
                    EventTimeStamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EventType = table.Column<int>(type: "integer", nullable: false),
                    ReadByBroker = table.Column<bool>(type: "boolean", nullable: false),
                    NotifyBroker = table.Column<bool>(type: "boolean", nullable: false),
                    DeleteAfterProcessing = table.Column<bool>(type: "boolean", nullable: false),
                    IsActionPlanResult = table.Column<bool>(type: "boolean", nullable: false),
                    ProcessingStatus = table.Column<int>(type: "integer", nullable: false),
                    Props = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppEvents_Brokers_BrokerId",
                        column: x => x.BrokerId,
                        principalTable: "Brokers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AppEvents_Leads_LeadId",
                        column: x => x.LeadId,
                        principalTable: "Leads",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "AreaLead",
                columns: table => new
                {
                    AreasOfInterestId = table.Column<int>(type: "integer", nullable: false),
                    InterestedLeadsId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AreaLead", x => new { x.AreasOfInterestId, x.InterestedLeadsId });
                    table.ForeignKey(
                        name: "FK_AreaLead_Areas_AreasOfInterestId",
                        column: x => x.AreasOfInterestId,
                        principalTable: "Areas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AreaLead_Leads_InterestedLeadsId",
                        column: x => x.InterestedLeadsId,
                        principalTable: "Leads",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "EmailEvents",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    BrokerId = table.Column<Guid>(type: "uuid", nullable: false),
                    BrokerEmail = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    LeadId = table.Column<int>(type: "integer", nullable: true),
                    TimeReceived = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TimesReplyNeededReminded = table.Column<byte>(type: "smallint", nullable: false),
                    ConversationId = table.Column<string>(type: "text", nullable: true),
                    Seen = table.Column<bool>(type: "boolean", nullable: false),
                    RepliedTo = table.Column<bool>(type: "boolean", nullable: false),
                    NeedsAction = table.Column<bool>(type: "boolean", nullable: false),
                    LeadParsedFromEmail = table.Column<bool>(type: "boolean", nullable: false),
                    LeadProviderEmail = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmailEvents_Brokers_BrokerId",
                        column: x => x.BrokerId,
                        principalTable: "Brokers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EmailEvents_Leads_LeadId",
                        column: x => x.LeadId,
                        principalTable: "Leads",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "LeadEmails",
                columns: table => new
                {
                    EmailAddress = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    LeadId = table.Column<int>(type: "integer", nullable: false),
                    IsMain = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeadEmails", x => new { x.EmailAddress, x.LeadId });
                    table.ForeignKey(
                        name: "FK_LeadEmails_Leads_LeadId",
                        column: x => x.LeadId,
                        principalTable: "Leads",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LeadTag",
                columns: table => new
                {
                    LeadsId = table.Column<int>(type: "integer", nullable: false),
                    TagsId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeadTag", x => new { x.LeadsId, x.TagsId });
                    table.ForeignKey(
                        name: "FK_LeadTag_Leads_LeadsId",
                        column: x => x.LeadsId,
                        principalTable: "Leads",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LeadTag_Tags_TagsId",
                        column: x => x.TagsId,
                        principalTable: "Tags",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Notes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    NotesText = table.Column<string>(type: "text", nullable: false),
                    LeadId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Notes_Leads_LeadId",
                        column: x => x.LeadId,
                        principalTable: "Leads",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Notifs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BrokerId = table.Column<Guid>(type: "uuid", nullable: false),
                    LeadId = table.Column<int>(type: "integer", nullable: true),
                    CreatedTimeStamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    NotifType = table.Column<int>(type: "integer", nullable: false),
                    isSeen = table.Column<bool>(type: "boolean", nullable: false),
                    priority = table.Column<byte>(type: "smallint", nullable: false),
                    EventId = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Notifs_Brokers_BrokerId",
                        column: x => x.BrokerId,
                        principalTable: "Brokers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Notifs_Leads_LeadId",
                        column: x => x.LeadId,
                        principalTable: "Leads",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ToDoTasks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Description = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    TaskName = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    IsDone = table.Column<bool>(type: "boolean", nullable: false),
                    TaskDueDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    HangfireReminderId = table.Column<string>(type: "text", nullable: true),
                    BrokerId = table.Column<Guid>(type: "uuid", nullable: false),
                    LeadId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ToDoTasks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ToDoTasks_Brokers_BrokerId",
                        column: x => x.BrokerId,
                        principalTable: "Brokers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ToDoTasks_Leads_LeadId",
                        column: x => x.LeadId,
                        principalTable: "Leads",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ActionTrackers",
                columns: table => new
                {
                    TrackedActionId = table.Column<int>(type: "integer", nullable: false),
                    ActionPlanAssociationId = table.Column<int>(type: "integer", nullable: false),
                    ActionStatus = table.Column<int>(type: "integer", nullable: false),
                    HangfireJobId = table.Column<string>(type: "text", nullable: true),
                    HangfireScheduledStartTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ExecutionCompletedTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ActionResultId = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActionTrackers", x => new { x.ActionPlanAssociationId, x.TrackedActionId });
                    table.ForeignKey(
                        name: "FK_ActionTrackers_ActionPlanAssociations_ActionPlanAssociation~",
                        column: x => x.ActionPlanAssociationId,
                        principalTable: "ActionPlanAssociations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ActionTrackers_Actions_TrackedActionId",
                        column: x => x.TrackedActionId,
                        principalTable: "Actions",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_ActionPlanAssociations_ActionPlanId",
                table: "ActionPlanAssociations",
                column: "ActionPlanId");

            migrationBuilder.CreateIndex(
                name: "IX_ActionPlanAssociations_LeadId",
                table: "ActionPlanAssociations",
                column: "LeadId");

            migrationBuilder.CreateIndex(
                name: "IX_ActionPlans_BrokerId",
                table: "ActionPlans",
                column: "BrokerId");

            migrationBuilder.CreateIndex(
                name: "IX_Actions_ActionPlanId",
                table: "Actions",
                column: "ActionPlanId");

            migrationBuilder.CreateIndex(
                name: "IX_ActionTrackers_TrackedActionId",
                table: "ActionTrackers",
                column: "TrackedActionId");

            migrationBuilder.CreateIndex(
                name: "IX_AppEvents_BrokerId",
                table: "AppEvents",
                column: "BrokerId");

            migrationBuilder.CreateIndex(
                name: "IX_AppEvents_LeadId",
                table: "AppEvents",
                column: "LeadId");

            migrationBuilder.CreateIndex(
                name: "IX_AreaLead_InterestedLeadsId",
                table: "AreaLead",
                column: "InterestedLeadsId");

            migrationBuilder.CreateIndex(
                name: "IX_Areas_AgencyId",
                table: "Areas",
                column: "AgencyId");

            migrationBuilder.CreateIndex(
                name: "IX_BrokerListingAssignments_ListingId",
                table: "BrokerListingAssignments",
                column: "ListingId");

            migrationBuilder.CreateIndex(
                name: "IX_Brokers_AgencyId",
                table: "Brokers",
                column: "AgencyId");

            migrationBuilder.CreateIndex(
                name: "IX_ConnectedEmails_BrokerId",
                table: "ConnectedEmails",
                column: "BrokerId");

            migrationBuilder.CreateIndex(
                name: "IX_ConnectedEmails_GraphSubscriptionId",
                table: "ConnectedEmails",
                column: "GraphSubscriptionId");

            migrationBuilder.CreateIndex(
                name: "IX_EmailEvents_BrokerId",
                table: "EmailEvents",
                column: "BrokerId");

            migrationBuilder.CreateIndex(
                name: "IX_EmailEvents_LeadId",
                table: "EmailEvents",
                column: "LeadId");

            migrationBuilder.CreateIndex(
                name: "IX_LeadEmails_LeadId",
                table: "LeadEmails",
                column: "LeadId");

            migrationBuilder.CreateIndex(
                name: "IX_Leads_AgencyId",
                table: "Leads",
                column: "AgencyId");

            migrationBuilder.CreateIndex(
                name: "IX_Leads_BrokerId",
                table: "Leads",
                column: "BrokerId");

            migrationBuilder.CreateIndex(
                name: "IX_Leads_ListingId",
                table: "Leads",
                column: "ListingId");

            migrationBuilder.CreateIndex(
                name: "IX_LeadTag_TagsId",
                table: "LeadTag",
                column: "TagsId");

            migrationBuilder.CreateIndex(
                name: "IX_Listings_AgencyId_FormattedStreetAddress",
                table: "Listings",
                columns: new[] { "AgencyId", "FormattedStreetAddress" });

            migrationBuilder.CreateIndex(
                name: "IX_Notes_LeadId",
                table: "Notes",
                column: "LeadId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Notifs_BrokerId",
                table: "Notifs",
                column: "BrokerId");

            migrationBuilder.CreateIndex(
                name: "IX_Notifs_LeadId",
                table: "Notifs",
                column: "LeadId");

            migrationBuilder.CreateIndex(
                name: "IX_RecurrentTasks_BrokerId",
                table: "RecurrentTasks",
                column: "BrokerId");

            migrationBuilder.CreateIndex(
                name: "IX_Tags_BrokerId",
                table: "Tags",
                column: "BrokerId");

            migrationBuilder.CreateIndex(
                name: "IX_Templates_BrokerId",
                table: "Templates",
                column: "BrokerId");

            migrationBuilder.CreateIndex(
                name: "IX_ToDoTasks_BrokerId",
                table: "ToDoTasks",
                column: "BrokerId");

            migrationBuilder.CreateIndex(
                name: "IX_ToDoTasks_LeadId",
                table: "ToDoTasks",
                column: "LeadId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ActionTrackers");

            migrationBuilder.DropTable(
                name: "AppEvents");

            migrationBuilder.DropTable(
                name: "AreaLead");

            migrationBuilder.DropTable(
                name: "BrokerListingAssignments");

            migrationBuilder.DropTable(
                name: "ConnectedEmails");

            migrationBuilder.DropTable(
                name: "EmailEvents");

            migrationBuilder.DropTable(
                name: "LeadEmails");

            migrationBuilder.DropTable(
                name: "LeadTag");

            migrationBuilder.DropTable(
                name: "Notes");

            migrationBuilder.DropTable(
                name: "Notifs");

            migrationBuilder.DropTable(
                name: "RecurrentTasks");

            migrationBuilder.DropTable(
                name: "Templates");

            migrationBuilder.DropTable(
                name: "ToDoTasks");

            migrationBuilder.DropTable(
                name: "ActionPlanAssociations");

            migrationBuilder.DropTable(
                name: "Actions");

            migrationBuilder.DropTable(
                name: "Areas");

            migrationBuilder.DropTable(
                name: "Tags");

            migrationBuilder.DropTable(
                name: "Leads");

            migrationBuilder.DropTable(
                name: "ActionPlans");

            migrationBuilder.DropTable(
                name: "Listings");

            migrationBuilder.DropTable(
                name: "Brokers");

            migrationBuilder.DropTable(
                name: "Agencies");
        }
    }
}
