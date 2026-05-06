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
        migrationBuilder.Sql(
            """
            IF EXISTS (
                SELECT 1
                FROM sys.indexes
                WHERE name = N'IX_BenefitRedemptions_ShippingPaymentId'
                  AND object_id = OBJECT_ID(N'[BenefitRedemptions]')
            )
            BEGIN
                DROP INDEX [IX_BenefitRedemptions_ShippingPaymentId] ON [BenefitRedemptions];
            END
            """);

        migrationBuilder.Sql(
            """
            IF NOT EXISTS (
                SELECT 1
                FROM sys.indexes
                WHERE name = N'IX_BenefitRedemptions_ShippingPaymentId'
                  AND object_id = OBJECT_ID(N'[BenefitRedemptions]')
            )
            BEGIN
                CREATE INDEX [IX_BenefitRedemptions_ShippingPaymentId]
                ON [BenefitRedemptions] ([ShippingPaymentId]);
            END
            """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        migrationBuilder.Sql(
            """
            IF EXISTS (
                SELECT 1
                FROM sys.indexes
                WHERE name = N'IX_BenefitRedemptions_ShippingPaymentId'
                  AND object_id = OBJECT_ID(N'[BenefitRedemptions]')
            )
            BEGIN
                DROP INDEX [IX_BenefitRedemptions_ShippingPaymentId] ON [BenefitRedemptions];
            END
            """);

        migrationBuilder.Sql(
            """
            IF NOT EXISTS (
                SELECT 1
                FROM sys.indexes
                WHERE name = N'IX_BenefitRedemptions_ShippingPaymentId'
                  AND object_id = OBJECT_ID(N'[BenefitRedemptions]')
            )
            BEGIN
                CREATE UNIQUE INDEX [IX_BenefitRedemptions_ShippingPaymentId]
                ON [BenefitRedemptions] ([ShippingPaymentId]);
            END
            """);
        }
    }
}
