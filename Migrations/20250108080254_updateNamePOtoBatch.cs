using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PurchasingSystem.Migrations
{
    public partial class updateNamePOtoBatch : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "PurchaseOrderNumber",
                schema: "dbo",
                table: "WrhProductReturn",
                newName: "BatchNumber");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "BatchNumber",
                schema: "dbo",
                table: "WrhProductReturn",
                newName: "PurchaseOrderNumber");
        }
    }
}
