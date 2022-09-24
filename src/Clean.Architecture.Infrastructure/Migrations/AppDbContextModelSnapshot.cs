﻿// <auto-generated />
using System;
using Clean.Architecture.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace Clean.Architecture.Infrastructure.Migrations
{
    [DbContext(typeof(AppDbContext))]
    partial class AppDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "6.0.6")
                .HasAnnotation("Relational:MaxIdentifierLength", 128);

            SqlServerModelBuilderExtensions.UseIdentityColumns(modelBuilder, 1L, 1);

            modelBuilder.Entity("AreaLead", b =>
                {
                    b.Property<int>("AreasOfInterestId")
                        .HasColumnType("int");

                    b.Property<int>("InterestedLeadsId")
                        .HasColumnType("int");

                    b.HasKey("AreasOfInterestId", "InterestedLeadsId");

                    b.HasIndex("InterestedLeadsId");

                    b.ToTable("AreaLead");
                });

            modelBuilder.Entity("Clean.Architecture.Core.Domain.ActionPlanAggregate.ActionPlan", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"), 1L, 1);

                    b.Property<int>("ActionsCount")
                        .HasColumnType("int");

                    b.Property<bool>("AssignToLead")
                        .HasColumnType("bit");

                    b.Property<Guid>("BrokerId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("FirstActionDelay")
                        .HasColumnType("nvarchar(max)");

                    b.Property<int>("NotifsToListenTo")
                        .HasColumnType("int");

                    b.Property<bool>("StopPlanOnInteraction")
                        .HasColumnType("bit");

                    b.Property<DateTime>("TimeCreated")
                        .HasColumnType("datetime2");

                    b.Property<string>("Title")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<int>("Triggers")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex("BrokerId");

                    b.ToTable("ActionPlans");
                });

            modelBuilder.Entity("Clean.Architecture.Core.Domain.ActionPlanAggregate.ActionPlanAssociation", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"), 1L, 1);

                    b.Property<int?>("ActionPlanId")
                        .HasColumnType("int");

                    b.Property<DateTime>("ActionPlanTriggeredAt")
                        .HasColumnType("datetime2");

                    b.Property<string>("CustomDelay")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("FirstActionHangfireId")
                        .HasColumnType("nvarchar(max)");

                    b.Property<int>("LeadId")
                        .HasColumnType("int");

                    b.Property<int>("ThisActionPlanStatus")
                        .HasColumnType("int");

                    b.Property<int?>("TriggerNotificationId")
                        .HasColumnType("int");

                    b.Property<int?>("currentTrackedActionId")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex("ActionPlanId");

                    b.HasIndex("LeadId");

                    b.ToTable("ActionPlanAssociations");
                });

            modelBuilder.Entity("Clean.Architecture.Core.Domain.ActionPlanAggregate.Actions.ActionBase", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"), 1L, 1);

                    b.Property<int>("ActionLevel")
                        .HasColumnType("int");

                    b.Property<int>("ActionPlanId")
                        .HasColumnType("int");

                    b.Property<string>("ActionProperties")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Discriminator")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("NextActionDelay")
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("Id");

                    b.HasIndex("ActionPlanId");

                    b.ToTable("Actions");

                    b.HasDiscriminator<string>("Discriminator").HasValue("ActionBase");
                });

            modelBuilder.Entity("Clean.Architecture.Core.Domain.ActionPlanAggregate.ActionTracker", b =>
                {
                    b.Property<int>("ActionPlanAssociationId")
                        .HasColumnType("int");

                    b.Property<int>("TrackedActionId")
                        .HasColumnType("int");

                    b.Property<int?>("ActionResultId")
                        .HasColumnType("int");

                    b.Property<int>("ActionStatus")
                        .HasColumnType("int");

                    b.Property<string>("ActionStatusInfo")
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTime?>("ExecutionCompletedTime")
                        .HasColumnType("datetime2");

                    b.Property<string>("HangfireJobId")
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTime?>("HangfireScheduledStartTime")
                        .HasColumnType("datetime2");

                    b.HasKey("ActionPlanAssociationId", "TrackedActionId");

                    b.HasIndex("TrackedActionId");

                    b.ToTable("ActionTrackers");
                });

            modelBuilder.Entity("Clean.Architecture.Core.Domain.AgencyAggregate.Agency", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"), 1L, 1);

                    b.Property<string>("AdminStripeId")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("AgencyName")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("LastCheckoutSessionID")
                        .HasColumnType("nvarchar(max)");

                    b.Property<int>("NumberOfBrokersInDatabase")
                        .HasColumnType("int");

                    b.Property<int>("NumberOfBrokersInSubscription")
                        .HasColumnType("int");

                    b.Property<DateTime>("SignupDateTime")
                        .HasColumnType("datetime2");

                    b.Property<string>("StripeSubscriptionId")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("StripeSubscriptionStatus")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTime?>("SubscriptionLastValidDate")
                        .HasColumnType("date");

                    b.HasKey("Id");

                    b.ToTable("Agencies");
                });

            modelBuilder.Entity("Clean.Architecture.Core.Domain.AgencyAggregate.Area", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"), 1L, 1);

                    b.Property<int>("AgencyId")
                        .HasColumnType("int");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("PostalCode")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("Id");

                    b.HasIndex("AgencyId");

                    b.ToTable("Areas");
                });

            modelBuilder.Entity("Clean.Architecture.Core.Domain.AgencyAggregate.Listing", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"), 1L, 1);

                    b.Property<string>("Address")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<int>("AgencyId")
                        .HasColumnType("int");

                    b.Property<Guid?>("BrokerId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTime>("DateOfListing")
                        .HasColumnType("datetime2");

                    b.Property<int>("Price")
                        .HasColumnType("int");

                    b.Property<int>("Status")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex("AgencyId");

                    b.HasIndex("BrokerId");

                    b.ToTable("Listings");
                });

            modelBuilder.Entity("Clean.Architecture.Core.Domain.BrokerAggregate.Broker", b =>
                {
                    b.Property<Guid>("Id")
                        .HasColumnType("uniqueidentifier");

                    b.Property<bool>("AccountActive")
                        .HasColumnType("bit");

                    b.Property<int>("AgencyId")
                        .HasColumnType("int");

                    b.Property<DateTime>("Created")
                        .HasColumnType("datetime2");

                    b.Property<string>("FirstConnectedEmail")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("FirstName")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("LastName")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("LoginEmail")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<int?>("NotifsForActionPlans")
                        .HasColumnType("int");

                    b.Property<string>("PhoneNumber")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("SecondaryConnectedEmail")
                        .HasColumnType("nvarchar(max)");

                    b.Property<bool>("isAdmin")
                        .HasColumnType("bit");

                    b.HasKey("Id");

                    b.HasIndex("AgencyId");

                    b.ToTable("Brokers");
                });

            modelBuilder.Entity("Clean.Architecture.Core.Domain.BrokerAggregate.EmailTemplate", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"), 1L, 1);

                    b.Property<Guid>("BrokerId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("EmailTemplateSubject")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("EmailTemplateText")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("Id");

                    b.HasIndex("BrokerId");

                    b.ToTable("EmailTemplates");
                });

            modelBuilder.Entity("Clean.Architecture.Core.Domain.BrokerAggregate.SmsTemplate", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"), 1L, 1);

                    b.Property<Guid>("BrokerId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("TemplateText")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("Id");

                    b.HasIndex("BrokerId");

                    b.ToTable("SmsTemplates");
                });

            modelBuilder.Entity("Clean.Architecture.Core.Domain.BrokerAggregate.Tag", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"), 1L, 1);

                    b.Property<Guid>("BrokerId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("TagName")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("Id");

                    b.HasIndex("BrokerId");

                    b.ToTable("Tags");
                });

            modelBuilder.Entity("Clean.Architecture.Core.Domain.BrokerAggregate.ToDoTask", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"), 1L, 1);

                    b.Property<Guid>("BrokerId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<int?>("LeadId")
                        .HasColumnType("int");

                    b.Property<DateTime>("TaskDueDate")
                        .HasColumnType("datetime2");

                    b.Property<string>("TaskText")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("Id");

                    b.HasIndex("BrokerId");

                    b.HasIndex("LeadId");

                    b.ToTable("ToDoTasks");
                });

            modelBuilder.Entity("Clean.Architecture.Core.Domain.LeadAggregate.Lead", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"), 1L, 1);

                    b.Property<int>("AgencyId")
                        .HasColumnType("int");

                    b.Property<Guid?>("BrokerId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<int?>("Budget")
                        .HasColumnType("int");

                    b.Property<string>("Email")
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTime>("EntryDate")
                        .HasColumnType("datetime2");

                    b.Property<string>("LeadFirstName")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("LeadLastName")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("LeadStatus")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("PhoneNumber")
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("Id");

                    b.HasIndex("AgencyId");

                    b.HasIndex("BrokerId");

                    b.ToTable("Leads");
                });

            modelBuilder.Entity("Clean.Architecture.Core.Domain.LeadAggregate.LeadListing", b =>
                {
                    b.Property<int>("LeadId")
                        .HasColumnType("int");

                    b.Property<int>("ListingId")
                        .HasColumnType("int");

                    b.Property<string>("ClientComments")
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("LeadId", "ListingId");

                    b.HasIndex("ListingId");

                    b.ToTable("LeadListing");
                });

            modelBuilder.Entity("Clean.Architecture.Core.Domain.LeadAggregate.Note", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"), 1L, 1);

                    b.Property<int>("LeadId")
                        .HasColumnType("int");

                    b.Property<string>("NotesText")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("Id");

                    b.HasIndex("LeadId");

                    b.ToTable("Notes");
                });

            modelBuilder.Entity("Clean.Architecture.Core.Domain.NotificationAggregate.Notification", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"), 1L, 1);

                    b.Property<Guid>("BrokerId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<int?>("LeadId")
                        .HasColumnType("int");

                    b.Property<DateTime>("NotifCreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<string>("NotifData")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<int>("NotifHandlingStatus")
                        .HasColumnType("int");

                    b.Property<int>("NotifType")
                        .HasColumnType("int");

                    b.Property<bool>("NotifyBroker")
                        .HasColumnType("bit");

                    b.Property<bool>("ReadByBroker")
                        .HasColumnType("bit");

                    b.HasKey("Id");

                    b.HasIndex("LeadId");

                    b.ToTable("Notifications");
                });

            modelBuilder.Entity("LeadTag", b =>
                {
                    b.Property<int>("LeadsId")
                        .HasColumnType("int");

                    b.Property<int>("TagsId")
                        .HasColumnType("int");

                    b.HasKey("LeadsId", "TagsId");

                    b.HasIndex("TagsId");

                    b.ToTable("LeadTag");
                });

            modelBuilder.Entity("Clean.Architecture.Core.Domain.ActionPlanAggregate.Actions.ChangeLeadStatusAction", b =>
                {
                    b.HasBaseType("Clean.Architecture.Core.Domain.ActionPlanAggregate.Actions.ActionBase");

                    b.HasDiscriminator().HasValue("ChangeLeadStatusAction");
                });

            modelBuilder.Entity("Clean.Architecture.Core.Domain.ActionPlanAggregate.Actions.SendEmailAction", b =>
                {
                    b.HasBaseType("Clean.Architecture.Core.Domain.ActionPlanAggregate.Actions.ActionBase");

                    b.HasDiscriminator().HasValue("SendEmailAction");
                });

            modelBuilder.Entity("AreaLead", b =>
                {
                    b.HasOne("Clean.Architecture.Core.Domain.AgencyAggregate.Area", null)
                        .WithMany()
                        .HasForeignKey("AreasOfInterestId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("Clean.Architecture.Core.Domain.LeadAggregate.Lead", null)
                        .WithMany()
                        .HasForeignKey("InterestedLeadsId")
                        .OnDelete(DeleteBehavior.ClientCascade)
                        .IsRequired();
                });

            modelBuilder.Entity("Clean.Architecture.Core.Domain.ActionPlanAggregate.ActionPlan", b =>
                {
                    b.HasOne("Clean.Architecture.Core.Domain.BrokerAggregate.Broker", "broker")
                        .WithMany("ActionPlans")
                        .HasForeignKey("BrokerId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("broker");
                });

            modelBuilder.Entity("Clean.Architecture.Core.Domain.ActionPlanAggregate.ActionPlanAssociation", b =>
                {
                    b.HasOne("Clean.Architecture.Core.Domain.ActionPlanAggregate.ActionPlan", "ActionPlan")
                        .WithMany("ActionPlanAssociations")
                        .HasForeignKey("ActionPlanId");

                    b.HasOne("Clean.Architecture.Core.Domain.LeadAggregate.Lead", "lead")
                        .WithMany("ActionPlanAssociations")
                        .HasForeignKey("LeadId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("ActionPlan");

                    b.Navigation("lead");
                });

            modelBuilder.Entity("Clean.Architecture.Core.Domain.ActionPlanAggregate.Actions.ActionBase", b =>
                {
                    b.HasOne("Clean.Architecture.Core.Domain.ActionPlanAggregate.ActionPlan", "ActionPlan")
                        .WithMany("Actions")
                        .HasForeignKey("ActionPlanId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("ActionPlan");
                });

            modelBuilder.Entity("Clean.Architecture.Core.Domain.ActionPlanAggregate.ActionTracker", b =>
                {
                    b.HasOne("Clean.Architecture.Core.Domain.ActionPlanAggregate.ActionPlanAssociation", "ActionPlanAssociation")
                        .WithMany("ActionTrackers")
                        .HasForeignKey("ActionPlanAssociationId")
                        .OnDelete(DeleteBehavior.ClientCascade)
                        .IsRequired();

                    b.HasOne("Clean.Architecture.Core.Domain.ActionPlanAggregate.Actions.ActionBase", "TrackedAction")
                        .WithMany("ActionTrackers")
                        .HasForeignKey("TrackedActionId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("ActionPlanAssociation");

                    b.Navigation("TrackedAction");
                });

            modelBuilder.Entity("Clean.Architecture.Core.Domain.AgencyAggregate.Area", b =>
                {
                    b.HasOne("Clean.Architecture.Core.Domain.AgencyAggregate.Agency", "Agency")
                        .WithMany("Areas")
                        .HasForeignKey("AgencyId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Agency");
                });

            modelBuilder.Entity("Clean.Architecture.Core.Domain.AgencyAggregate.Listing", b =>
                {
                    b.HasOne("Clean.Architecture.Core.Domain.AgencyAggregate.Agency", "Agency")
                        .WithMany("AgencyListings")
                        .HasForeignKey("AgencyId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("Clean.Architecture.Core.Domain.BrokerAggregate.Broker", "AssignedBroker")
                        .WithMany("Listings")
                        .HasForeignKey("BrokerId");

                    b.Navigation("Agency");

                    b.Navigation("AssignedBroker");
                });

            modelBuilder.Entity("Clean.Architecture.Core.Domain.BrokerAggregate.Broker", b =>
                {
                    b.HasOne("Clean.Architecture.Core.Domain.AgencyAggregate.Agency", "Agency")
                        .WithMany("AgencyBrokers")
                        .HasForeignKey("AgencyId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Agency");
                });

            modelBuilder.Entity("Clean.Architecture.Core.Domain.BrokerAggregate.EmailTemplate", b =>
                {
                    b.HasOne("Clean.Architecture.Core.Domain.BrokerAggregate.Broker", "Broker")
                        .WithMany("EmailTemplates")
                        .HasForeignKey("BrokerId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Broker");
                });

            modelBuilder.Entity("Clean.Architecture.Core.Domain.BrokerAggregate.SmsTemplate", b =>
                {
                    b.HasOne("Clean.Architecture.Core.Domain.BrokerAggregate.Broker", "Broker")
                        .WithMany("SmsTemplates")
                        .HasForeignKey("BrokerId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Broker");
                });

            modelBuilder.Entity("Clean.Architecture.Core.Domain.BrokerAggregate.Tag", b =>
                {
                    b.HasOne("Clean.Architecture.Core.Domain.BrokerAggregate.Broker", "Broker")
                        .WithMany("BrokerTags")
                        .HasForeignKey("BrokerId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Broker");
                });

            modelBuilder.Entity("Clean.Architecture.Core.Domain.BrokerAggregate.ToDoTask", b =>
                {
                    b.HasOne("Clean.Architecture.Core.Domain.BrokerAggregate.Broker", "Broker")
                        .WithMany("Tasks")
                        .HasForeignKey("BrokerId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("Clean.Architecture.Core.Domain.LeadAggregate.Lead", "Lead")
                        .WithMany()
                        .HasForeignKey("LeadId");

                    b.Navigation("Broker");

                    b.Navigation("Lead");
                });

            modelBuilder.Entity("Clean.Architecture.Core.Domain.LeadAggregate.Lead", b =>
                {
                    b.HasOne("Clean.Architecture.Core.Domain.AgencyAggregate.Agency", "Agency")
                        .WithMany("Leads")
                        .HasForeignKey("AgencyId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("Clean.Architecture.Core.Domain.BrokerAggregate.Broker", "Broker")
                        .WithMany("Leads")
                        .HasForeignKey("BrokerId");

                    b.Navigation("Agency");

                    b.Navigation("Broker");
                });

            modelBuilder.Entity("Clean.Architecture.Core.Domain.LeadAggregate.LeadListing", b =>
                {
                    b.HasOne("Clean.Architecture.Core.Domain.LeadAggregate.Lead", "Lead")
                        .WithMany("ListingsOfInterest")
                        .HasForeignKey("LeadId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("Clean.Architecture.Core.Domain.AgencyAggregate.Listing", "Listing")
                        .WithMany("InterestedLeads")
                        .HasForeignKey("ListingId")
                        .OnDelete(DeleteBehavior.ClientCascade)
                        .IsRequired();

                    b.Navigation("Lead");

                    b.Navigation("Listing");
                });

            modelBuilder.Entity("Clean.Architecture.Core.Domain.LeadAggregate.Note", b =>
                {
                    b.HasOne("Clean.Architecture.Core.Domain.LeadAggregate.Lead", "Lead")
                        .WithMany("Notes")
                        .HasForeignKey("LeadId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Lead");
                });

            modelBuilder.Entity("Clean.Architecture.Core.Domain.NotificationAggregate.Notification", b =>
                {
                    b.HasOne("Clean.Architecture.Core.Domain.LeadAggregate.Lead", null)
                        .WithMany("LeadHistoryEvents")
                        .HasForeignKey("LeadId");
                });

            modelBuilder.Entity("LeadTag", b =>
                {
                    b.HasOne("Clean.Architecture.Core.Domain.LeadAggregate.Lead", null)
                        .WithMany()
                        .HasForeignKey("LeadsId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("Clean.Architecture.Core.Domain.BrokerAggregate.Tag", null)
                        .WithMany()
                        .HasForeignKey("TagsId")
                        .OnDelete(DeleteBehavior.ClientCascade)
                        .IsRequired();
                });

            modelBuilder.Entity("Clean.Architecture.Core.Domain.ActionPlanAggregate.ActionPlan", b =>
                {
                    b.Navigation("ActionPlanAssociations");

                    b.Navigation("Actions");
                });

            modelBuilder.Entity("Clean.Architecture.Core.Domain.ActionPlanAggregate.ActionPlanAssociation", b =>
                {
                    b.Navigation("ActionTrackers");
                });

            modelBuilder.Entity("Clean.Architecture.Core.Domain.ActionPlanAggregate.Actions.ActionBase", b =>
                {
                    b.Navigation("ActionTrackers");
                });

            modelBuilder.Entity("Clean.Architecture.Core.Domain.AgencyAggregate.Agency", b =>
                {
                    b.Navigation("AgencyBrokers");

                    b.Navigation("AgencyListings");

                    b.Navigation("Areas");

                    b.Navigation("Leads");
                });

            modelBuilder.Entity("Clean.Architecture.Core.Domain.AgencyAggregate.Listing", b =>
                {
                    b.Navigation("InterestedLeads");
                });

            modelBuilder.Entity("Clean.Architecture.Core.Domain.BrokerAggregate.Broker", b =>
                {
                    b.Navigation("ActionPlans");

                    b.Navigation("BrokerTags");

                    b.Navigation("EmailTemplates");

                    b.Navigation("Leads");

                    b.Navigation("Listings");

                    b.Navigation("SmsTemplates");

                    b.Navigation("Tasks");
                });

            modelBuilder.Entity("Clean.Architecture.Core.Domain.LeadAggregate.Lead", b =>
                {
                    b.Navigation("ActionPlanAssociations");

                    b.Navigation("LeadHistoryEvents");

                    b.Navigation("ListingsOfInterest");

                    b.Navigation("Notes");
                });
#pragma warning restore 612, 618
        }
    }
}
