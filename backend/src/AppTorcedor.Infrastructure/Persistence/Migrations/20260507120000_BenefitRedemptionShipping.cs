using AppTorcedor.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AppTorcedor.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
[DbContext(typeof(AppDbContext))]
[Migration("20260507120000_BenefitRedemptionShipping")]
public partial class BenefitRedemptionShipping : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "ShippingMethod",
            table: "BenefitRedemptions",
            type: "nvarchar(20)",
            maxLength: 20,
            nullable: true);

        migrationBuilder.AddColumn<int>(
            name: "ShippingCarrierId",
            table: "BenefitRedemptions",
            type: "int",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "ShippingCarrierName",
            table: "BenefitRedemptions",
            type: "nvarchar(80)",
            maxLength: 80,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "ShippingServiceName",
            table: "BenefitRedemptions",
            type: "nvarchar(80)",
            maxLength: 80,
            nullable: true);

        migrationBuilder.AddColumn<decimal>(
            name: "ShippingPrice",
            table: "BenefitRedemptions",
            type: "decimal(10,2)",
            precision: 10,
            scale: 2,
            nullable: true);

        migrationBuilder.AddColumn<int>(
            name: "ShippingDeliveryDays",
            table: "BenefitRedemptions",
            type: "int",
            nullable: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "ShippingMethod", table: "BenefitRedemptions");
        migrationBuilder.DropColumn(name: "ShippingCarrierId", table: "BenefitRedemptions");
        migrationBuilder.DropColumn(name: "ShippingCarrierName", table: "BenefitRedemptions");
        migrationBuilder.DropColumn(name: "ShippingServiceName", table: "BenefitRedemptions");
        migrationBuilder.DropColumn(name: "ShippingPrice", table: "BenefitRedemptions");
        migrationBuilder.DropColumn(name: "ShippingDeliveryDays", table: "BenefitRedemptions");
    }
}
