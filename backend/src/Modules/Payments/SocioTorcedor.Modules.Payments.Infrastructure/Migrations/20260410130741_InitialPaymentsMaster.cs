using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SocioTorcedor.Modules.Payments.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialPaymentsMaster : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Payments_TenantBillingSubscriptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantPlanId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SaaSPlanId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BillingCycle = table.Column<int>(type: "int", nullable: false),
                    RecurringAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ExternalCustomerId = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ExternalSubscriptionId = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NextBillingAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Payments_TenantBillingSubscriptions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Payments_TenantWebhookInbox",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IdempotencyKey = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    EventType = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    RawPayload = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ProcessedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastError = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Payments_TenantWebhookInbox", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Payments_TenantBillingInvoices",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantBillingSubscriptionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: false),
                    DueAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ExternalInvoiceId = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    PaidAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Payments_TenantBillingInvoices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Payments_TenantBillingInvoices_Payments_TenantBillingSubscriptions_TenantBillingSubscriptionId",
                        column: x => x.TenantBillingSubscriptionId,
                        principalTable: "Payments_TenantBillingSubscriptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Payments_TenantBillingInvoices_TenantBillingSubscriptionId",
                table: "Payments_TenantBillingInvoices",
                column: "TenantBillingSubscriptionId");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_TenantBillingSubscriptions_ExternalSubscriptionId",
                table: "Payments_TenantBillingSubscriptions",
                column: "ExternalSubscriptionId");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_TenantBillingSubscriptions_TenantId",
                table: "Payments_TenantBillingSubscriptions",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_TenantWebhookInbox_IdempotencyKey",
                table: "Payments_TenantWebhookInbox",
                column: "IdempotencyKey",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Payments_TenantBillingInvoices");

            migrationBuilder.DropTable(
                name: "Payments_TenantWebhookInbox");

            migrationBuilder.DropTable(
                name: "Payments_TenantBillingSubscriptions");
        }
    }
}
