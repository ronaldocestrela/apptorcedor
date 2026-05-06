using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AppTorcedor.Infrastructure.Persistence.Migrations
{
/// <inheritdoc />
public partial class BenefitRedemptionDeliveryAddress : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AlterColumn<string>(
            name: "ShirtDisplayName",
            table: "BenefitRedemptions",
            type: "nvarchar(10)",
            maxLength: 10,
            nullable: true,
            oldClrType: typeof(string),
            oldType: "nvarchar(40)",
            oldMaxLength: 40,
            oldNullable: true);

        migrationBuilder.AddColumn<string>(
            name: "DeliveryCep",
            table: "BenefitRedemptions",
            type: "nvarchar(8)",
            maxLength: 8,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "DeliveryNeighborhood",
            table: "BenefitRedemptions",
            type: "nvarchar(120)",
            maxLength: 120,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "DeliveryStreet",
            table: "BenefitRedemptions",
            type: "nvarchar(200)",
            maxLength: 200,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "DeliveryNumber",
            table: "BenefitRedemptions",
            type: "nvarchar(20)",
            maxLength: 20,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "DeliveryCity",
            table: "BenefitRedemptions",
            type: "nvarchar(120)",
            maxLength: 120,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "DeliveryState",
            table: "BenefitRedemptions",
            type: "nvarchar(2)",
            maxLength: 2,
            nullable: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "DeliveryCep",
            table: "BenefitRedemptions");

        migrationBuilder.DropColumn(
            name: "DeliveryNeighborhood",
            table: "BenefitRedemptions");

        migrationBuilder.DropColumn(
            name: "DeliveryStreet",
            table: "BenefitRedemptions");

        migrationBuilder.DropColumn(
            name: "DeliveryNumber",
            table: "BenefitRedemptions");

        migrationBuilder.DropColumn(
            name: "DeliveryCity",
            table: "BenefitRedemptions");

        migrationBuilder.DropColumn(
            name: "DeliveryState",
            table: "BenefitRedemptions");

        migrationBuilder.AlterColumn<string>(
            name: "ShirtDisplayName",
            table: "BenefitRedemptions",
            type: "nvarchar(40)",
            maxLength: 40,
            nullable: true,
            oldClrType: typeof(string),
            oldType: "nvarchar(10)",
            oldMaxLength: 10,
            oldNullable: true);
    }
}
}
