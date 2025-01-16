﻿using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PurchasingSystem.Migrations
{
    public partial class changeDateTimeOffsetInPR : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "ExpiredDate",
                schema: "dbo",
                table: "OrdPurchaseRequest",
                type: "datetimeoffset",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTime>(
                name: "ExpiredDate",
                schema: "dbo",
                table: "OrdPurchaseRequest",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateTimeOffset),
                oldType: "datetimeoffset");
        }
    }
}
