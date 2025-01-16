﻿using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PurchasingSystem.Migrations
{
    public partial class addColumnUserStatus : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ApproveStatusUser1",
                schema: "dbo",
                table: "OrdApproval",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ApproveStatusUser2",
                schema: "dbo",
                table: "OrdApproval",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ApproveStatusUser3",
                schema: "dbo",
                table: "OrdApproval",
                type: "nvarchar(max)",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ApproveStatusUser1",
                schema: "dbo",
                table: "OrdApproval");

            migrationBuilder.DropColumn(
                name: "ApproveStatusUser2",
                schema: "dbo",
                table: "OrdApproval");

            migrationBuilder.DropColumn(
                name: "ApproveStatusUser3",
                schema: "dbo",
                table: "OrdApproval");
        }
    }
}
