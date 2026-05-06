using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AppTorcedor.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SyncPendingModelChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_BenefitRedemptions_ShippingPaymentId",
                table: "BenefitRedemptions");

            migrationBuilder.CreateIndex(
                name: "IX_BenefitRedemptions_ShippingPaymentId",
                table: "BenefitRedemptions",
                column: "ShippingPaymentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_BenefitRedemptions_ShippingPaymentId",
                table: "BenefitRedemptions");

            migrationBuilder.CreateIndex(
                name: "IX_BenefitRedemptions_ShippingPaymentId",
                table: "BenefitRedemptions",
                column: "ShippingPaymentId",
                unique: true);
        }
    }
}
