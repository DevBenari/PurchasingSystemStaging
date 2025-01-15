using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PurchasingSystem.Migrations
{
    public partial class updateInApplicationUSer : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "LastActivityTime",
                table: "AspNetUsers",
                type: "datetimeoffset",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTime>(
                name: "LastActivityTime",
                table: "AspNetUsers",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTimeOffset),
                oldType: "datetimeoffset",
                oldNullable: true);
        }
    }
}
