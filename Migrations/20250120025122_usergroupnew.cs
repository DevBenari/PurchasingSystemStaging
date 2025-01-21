﻿using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PurchasingSystem.Migrations
{
    public partial class usergroupnew : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AspNetGroupUser",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DepartemenId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreateDateTime = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreateBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UpdateDateTime = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdateBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DeleteDateTime = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    DeleteBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsCancel = table.Column<bool>(type: "bit", nullable: false),
                    IsDelete = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetGroupUser", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AspNetGroupUser",
                schema: "dbo");
        }
    }
}
