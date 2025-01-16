using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PurchasingSystem.Migrations
{
    public partial class deleteColumnSubTotalQtyDifferenceDetail : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SubTotal",
                schema: "dbo",
                table: "WrhQtyDifferenceDetail");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "SubTotal",
                schema: "dbo",
                table: "WrhQtyDifferenceDetail",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);
        }
    }
}
