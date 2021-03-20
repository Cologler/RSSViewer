﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using RSSViewer.RulesDb;

namespace RSSViewer.Migrations
{
    [DbContext(typeof(RulesDbContext))]
    partial class RulesDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "5.0.0");

            modelBuilder.Entity("RSSViewer.RulesDb.MatchRule", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("Argument")
                        .HasColumnType("TEXT");

                    b.Property<TimeSpan?>("AutoDisabledAfterLastMatched")
                        .HasColumnType("TEXT");

                    b.Property<TimeSpan?>("AutoExpiredAfterLastMatched")
                        .HasColumnType("TEXT");

                    b.Property<string>("HandlerId")
                        .HasColumnType("TEXT");

                    b.Property<bool>("IgnoreCase")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("IsDisabled")
                        .HasColumnType("INTEGER");

                    b.Property<DateTime>("LastMatched")
                        .HasColumnType("TEXT");

                    b.Property<int>("Mode")
                        .HasColumnType("INTEGER");

                    b.Property<string>("OnFeedId")
                        .HasColumnType("TEXT");

                    b.Property<int>("OrderCode")
                        .HasColumnType("INTEGER");

                    b.Property<int?>("ParentId")
                        .HasColumnType("INTEGER");

                    b.Property<int>("TotalMatchedCount")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.ToTable("MatchRules");
                });
#pragma warning restore 612, 618
        }
    }
}
