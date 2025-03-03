﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using StoreRobot.Models;

#nullable disable

namespace StoreRobot.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20240516161038_DigikalaLimitLogs")]
    partial class DigikalaLimitLogs
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder.HasAnnotation("ProductVersion", "6.0.3");

            modelBuilder.Entity("StoreRobot.Models.Commission", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<int>("CategoryId")
                        .HasColumnType("INTEGER");

                    b.Property<double>("CommissionPercent")
                        .HasColumnType("REAL");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.ToTable("Commissions");
                });

            modelBuilder.Entity("StoreRobot.Models.DigiKalaCookie", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("Domain")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<DateTime?>("Expire")
                        .HasColumnType("TEXT");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("Path")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("Value")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.ToTable("DigiKalaCookies");
                });

            modelBuilder.Entity("StoreRobot.Models.DigikalaLimitLog", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<DateTime>("CreateDateTime")
                        .HasColumnType("TEXT");

                    b.Property<bool>("Limited")
                        .HasColumnType("INTEGER");

                    b.Property<int>("RequestCount")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.ToTable("DigikalaLimitLogs");
                });

            modelBuilder.Entity("StoreRobot.Models.DigikalaLoginToken", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("access_token")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("expire_in")
                        .HasColumnType("TEXT");

                    b.Property<string>("refresh_token")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.ToTable("DigikalaLoginToken");
                });

            modelBuilder.Entity("StoreRobot.Models.Product", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<int>("CommisionMax")
                        .HasColumnType("INTEGER");

                    b.Property<int>("CommisionMin")
                        .HasColumnType("INTEGER");

                    b.Property<int>("DigiStatus")
                        .HasColumnType("INTEGER");

                    b.Property<int>("DigiStockQuantity")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Dkpc")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<int>("DomesticPrice")
                        .HasColumnType("INTEGER");

                    b.Property<string>("ExpiryDate")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("Guarantee")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<bool>("IsPakhshOff")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("IsSafirOff")
                        .HasColumnType("INTEGER");

                    b.Property<int>("JumpStep")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<int>("PakhshAndSafirStockQuantity")
                        .HasColumnType("INTEGER");

                    b.Property<int>("PakhshPriceInOff")
                        .HasColumnType("INTEGER");

                    b.Property<int>("ReferencePrice")
                        .HasColumnType("INTEGER");

                    b.Property<int>("SafirPriceInOff")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Sku")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<int>("StokeQuantity")
                        .HasColumnType("INTEGER");

                    b.Property<int>("UserPrice")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.ToTable("Products");
                });

            modelBuilder.Entity("StoreRobot.Models.ProductErrors", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("ErrorMessage")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("ErroredPrice")
                        .HasColumnType("TEXT");

                    b.Property<string>("FirstPrice")
                        .HasColumnType("TEXT");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<int>("ReportNumber")
                        .HasColumnType("INTEGER");

                    b.Property<string>("SKU")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("Stock")
                        .HasColumnType("TEXT");

                    b.Property<int>("Store")
                        .HasColumnType("INTEGER");

                    b.Property<string>("UpdateError")
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.ToTable("ProductErrors");
                });
#pragma warning restore 612, 618
        }
    }
}
