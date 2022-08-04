﻿// <auto-generated />
using System;
using Clean.Architecture.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace Clean.Architecture.Infrastructure.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20220731225952_lol")]
    partial class lol
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
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

            modelBuilder.Entity("Clean.Architecture.Core.AgencyAggregate.Agency", b =>
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

            modelBuilder.Entity("Clean.Architecture.Core.AgencyAggregate.Area", b =>
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

                    b.Property<int>("PostalCode")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex("AgencyId");

                    b.ToTable("Areas");
                });

            modelBuilder.Entity("Clean.Architecture.Core.AgencyAggregate.Listing", b =>
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

            modelBuilder.Entity("Clean.Architecture.Core.BrokerAggregate.Broker", b =>
                {
                    b.Property<Guid>("Id")
                        .HasColumnType("uniqueidentifier");

                    b.Property<bool>("AccountActive")
                        .HasColumnType("bit");

                    b.Property<int>("AgencyId")
                        .HasColumnType("int");

                    b.Property<DateTime>("Created")
                        .HasColumnType("datetime2");

                    b.Property<string>("Email")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("FirstName")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("LastName")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("PhoneNumber")
                        .HasColumnType("nvarchar(max)");

                    b.Property<bool>("isAdmin")
                        .HasColumnType("bit");

                    b.HasKey("Id");

                    b.HasIndex("AgencyId");

                    b.ToTable("Brokers");
                });

            modelBuilder.Entity("Clean.Architecture.Core.BrokerAggregate.EmailTemplate", b =>
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

            modelBuilder.Entity("Clean.Architecture.Core.BrokerAggregate.SmsTemplate", b =>
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

            modelBuilder.Entity("Clean.Architecture.Core.BrokerAggregate.Tag", b =>
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

            modelBuilder.Entity("Clean.Architecture.Core.BrokerAggregate.ToDoTask", b =>
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

            modelBuilder.Entity("Clean.Architecture.Core.LeadAggregate.History", b =>
                {
                    b.Property<int>("HistoryId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("HistoryId"), 1L, 1);

                    b.Property<int?>("AgencyId")
                        .HasColumnType("int");

                    b.Property<int>("Event")
                        .HasColumnType("int");

                    b.Property<string>("EventDescription")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("EventSubject")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTime>("EventTimestamp")
                        .HasColumnType("datetime2");

                    b.Property<int>("LeadId")
                        .HasColumnType("int");

                    b.HasKey("HistoryId");

                    b.HasIndex("AgencyId");

                    b.HasIndex("LeadId");

                    b.ToTable("Histories");
                });

            modelBuilder.Entity("Clean.Architecture.Core.LeadAggregate.Lead", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"), 1L, 1);

                    b.Property<int>("AgencyId")
                        .HasColumnType("int");

                    b.Property<Guid?>("BrokerId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<int>("Budget")
                        .HasColumnType("int");

                    b.Property<string>("Email")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTime>("EntryDate")
                        .HasColumnType("datetime2");

                    b.Property<string>("LeadFirstName")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<int>("LeadLastName")
                        .HasColumnType("int");

                    b.Property<string>("LeadStatus")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("PhoneNumber")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("Id");

                    b.HasIndex("AgencyId");

                    b.HasIndex("BrokerId");

                    b.ToTable("Leads");
                });

            modelBuilder.Entity("Clean.Architecture.Core.LeadAggregate.Note", b =>
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

            modelBuilder.Entity("LeadListing", b =>
                {
                    b.Property<int>("InterestedLeadsId")
                        .HasColumnType("int");

                    b.Property<int>("ListingOfInterestId")
                        .HasColumnType("int");

                    b.HasKey("InterestedLeadsId", "ListingOfInterestId");

                    b.HasIndex("ListingOfInterestId");

                    b.ToTable("LeadListing");
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

            modelBuilder.Entity("AreaLead", b =>
                {
                    b.HasOne("Clean.Architecture.Core.AgencyAggregate.Area", null)
                        .WithMany()
                        .HasForeignKey("AreasOfInterestId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("Clean.Architecture.Core.LeadAggregate.Lead", null)
                        .WithMany()
                        .HasForeignKey("InterestedLeadsId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("Clean.Architecture.Core.AgencyAggregate.Area", b =>
                {
                    b.HasOne("Clean.Architecture.Core.AgencyAggregate.Agency", "Agency")
                        .WithMany("Areas")
                        .HasForeignKey("AgencyId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Agency");
                });

            modelBuilder.Entity("Clean.Architecture.Core.AgencyAggregate.Listing", b =>
                {
                    b.HasOne("Clean.Architecture.Core.AgencyAggregate.Agency", "Agency")
                        .WithMany("AgencyListings")
                        .HasForeignKey("AgencyId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("Clean.Architecture.Core.BrokerAggregate.Broker", "AssignedBroker")
                        .WithMany("Listings")
                        .HasForeignKey("BrokerId");

                    b.Navigation("Agency");

                    b.Navigation("AssignedBroker");
                });

            modelBuilder.Entity("Clean.Architecture.Core.BrokerAggregate.Broker", b =>
                {
                    b.HasOne("Clean.Architecture.Core.AgencyAggregate.Agency", "Agency")
                        .WithMany("AgencyBrokers")
                        .HasForeignKey("AgencyId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Agency");
                });

            modelBuilder.Entity("Clean.Architecture.Core.BrokerAggregate.EmailTemplate", b =>
                {
                    b.HasOne("Clean.Architecture.Core.BrokerAggregate.Broker", "Broker")
                        .WithMany("EmailTemplates")
                        .HasForeignKey("BrokerId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Broker");
                });

            modelBuilder.Entity("Clean.Architecture.Core.BrokerAggregate.SmsTemplate", b =>
                {
                    b.HasOne("Clean.Architecture.Core.BrokerAggregate.Broker", "Broker")
                        .WithMany("SmsTemplates")
                        .HasForeignKey("BrokerId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Broker");
                });

            modelBuilder.Entity("Clean.Architecture.Core.BrokerAggregate.Tag", b =>
                {
                    b.HasOne("Clean.Architecture.Core.BrokerAggregate.Broker", "Broker")
                        .WithMany("BrokerTags")
                        .HasForeignKey("BrokerId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Broker");
                });

            modelBuilder.Entity("Clean.Architecture.Core.BrokerAggregate.ToDoTask", b =>
                {
                    b.HasOne("Clean.Architecture.Core.BrokerAggregate.Broker", "Broker")
                        .WithMany("Tasks")
                        .HasForeignKey("BrokerId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("Clean.Architecture.Core.LeadAggregate.Lead", "Lead")
                        .WithMany()
                        .HasForeignKey("LeadId");

                    b.Navigation("Broker");

                    b.Navigation("Lead");
                });

            modelBuilder.Entity("Clean.Architecture.Core.LeadAggregate.History", b =>
                {
                    b.HasOne("Clean.Architecture.Core.AgencyAggregate.Agency", "Agency")
                        .WithMany()
                        .HasForeignKey("AgencyId");

                    b.HasOne("Clean.Architecture.Core.LeadAggregate.Lead", "Lead")
                        .WithMany("Histories")
                        .HasForeignKey("LeadId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Agency");

                    b.Navigation("Lead");
                });

            modelBuilder.Entity("Clean.Architecture.Core.LeadAggregate.Lead", b =>
                {
                    b.HasOne("Clean.Architecture.Core.AgencyAggregate.Agency", "Agency")
                        .WithMany("Leads")
                        .HasForeignKey("AgencyId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("Clean.Architecture.Core.BrokerAggregate.Broker", "Broker")
                        .WithMany("Leads")
                        .HasForeignKey("BrokerId");

                    b.Navigation("Agency");

                    b.Navigation("Broker");
                });

            modelBuilder.Entity("Clean.Architecture.Core.LeadAggregate.Note", b =>
                {
                    b.HasOne("Clean.Architecture.Core.LeadAggregate.Lead", "Lead")
                        .WithMany("Notes")
                        .HasForeignKey("LeadId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Lead");
                });

            modelBuilder.Entity("LeadListing", b =>
                {
                    b.HasOne("Clean.Architecture.Core.LeadAggregate.Lead", null)
                        .WithMany()
                        .HasForeignKey("InterestedLeadsId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("Clean.Architecture.Core.AgencyAggregate.Listing", null)
                        .WithMany()
                        .HasForeignKey("ListingOfInterestId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("LeadTag", b =>
                {
                    b.HasOne("Clean.Architecture.Core.LeadAggregate.Lead", null)
                        .WithMany()
                        .HasForeignKey("LeadsId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("Clean.Architecture.Core.BrokerAggregate.Tag", null)
                        .WithMany()
                        .HasForeignKey("TagsId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("Clean.Architecture.Core.AgencyAggregate.Agency", b =>
                {
                    b.Navigation("AgencyBrokers");

                    b.Navigation("AgencyListings");

                    b.Navigation("Areas");

                    b.Navigation("Leads");
                });

            modelBuilder.Entity("Clean.Architecture.Core.BrokerAggregate.Broker", b =>
                {
                    b.Navigation("BrokerTags");

                    b.Navigation("EmailTemplates");

                    b.Navigation("Leads");

                    b.Navigation("Listings");

                    b.Navigation("SmsTemplates");

                    b.Navigation("Tasks");
                });

            modelBuilder.Entity("Clean.Architecture.Core.LeadAggregate.Lead", b =>
                {
                    b.Navigation("Histories");

                    b.Navigation("Notes");
                });
#pragma warning restore 612, 618
        }
    }
}
