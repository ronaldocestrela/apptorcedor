using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AppTorcedor.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class BenefitShippingStripePayment : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_Payments_Memberships_MembershipId",
            table: "Payments");

        migrationBuilder.AlterColumn<Guid>(
            name: "MembershipId",
            table: "Payments",
            type: "uniqueidentifier",
            nullable: true,
            oldClrType: typeof(Guid),
            oldType: "uniqueidentifier");

        migrationBuilder.AddForeignKey(
            name: "FK_Payments_Memberships_MembershipId",
            table: "Payments",
            column: "MembershipId",
            principalTable: "Memberships",
            principalColumn: "Id",
            onDelete: ReferentialAction.Cascade);

        migrationBuilder.AddColumn<Guid>(
            name: "ShippingPaymentId",
            table: "BenefitRedemptions",
            type: "uniqueidentifier",
            nullable: true);

        migrationBuilder.AddColumn<DateTimeOffset>(
            name: "ShippingPaidAtUtc",
            table: "BenefitRedemptions",
            type: "datetimeoffset",
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "IX_BenefitRedemptions_ShippingPaymentId",
            table: "BenefitRedemptions",
            column: "ShippingPaymentId");

        migrationBuilder.AddForeignKey(
            name: "FK_BenefitRedemptions_Payments_ShippingPaymentId",
            table: "BenefitRedemptions",
            column: "ShippingPaymentId",
            principalTable: "Payments",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_BenefitRedemptions_Payments_ShippingPaymentId",
            table: "BenefitRedemptions");

        migrationBuilder.DropIndex(
            name: "IX_BenefitRedemptions_ShippingPaymentId",
            table: "BenefitRedemptions");

        migrationBuilder.DropColumn(
            name: "ShippingPaymentId",
            table: "BenefitRedemptions");

        migrationBuilder.DropColumn(
            name: "ShippingPaidAtUtc",
            table: "BenefitRedemptions");

        migrationBuilder.Sql(
            """
            DELETE FROM Payments WHERE MembershipId IS NULL
            """);

        migrationBuilder.DropForeignKey(
            name: "FK_Payments_Memberships_MembershipId",
            table: "Payments");

        migrationBuilder.AlterColumn<Guid>(
            name: "MembershipId",
            table: "Payments",
            type: "uniqueidentifier",
            nullable: false,
            defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
            oldClrType: typeof(Guid),
            oldType: "uniqueidentifier",
            oldNullable: true);

        migrationBuilder.AddForeignKey(
            name: "FK_Payments_Memberships_MembershipId",
            table: "Payments",
            column: "MembershipId",
            principalTable: "Memberships",
            principalColumn: "Id",
            onDelete: ReferentialAction.Cascade);
    }
}
