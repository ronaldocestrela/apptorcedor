using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SocioTorcedor.Modules.Payments.Infrastructure.Migrations.TenantPaymentsDb
{
    /// <inheritdoc />
    public partial class InitialPaymentsTenant : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Payments_MemberBillingSubscriptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MemberProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MemberPlanId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RecurringAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: false),
                    PaymentMethod = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ExternalCustomerId = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ExternalSubscriptionId = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NextBillingAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Payments_MemberBillingSubscriptions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Payments_MemberWebhookInbox",
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
                    table.PrimaryKey("PK_Payments_MemberWebhookInbox", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Payments_MemberBillingInvoices",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MemberBillingSubscriptionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: false),
                    PaymentMethod = table.Column<int>(type: "int", nullable: false),
                    DueAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ExternalInvoiceId = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    PixCopyPaste = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PaidAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Payments_MemberBillingInvoices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Payments_MemberBillingInvoices_Payments_MemberBillingSubscriptions_MemberBillingSubscriptionId",
                        column: x => x.MemberBillingSubscriptionId,
                        principalTable: "Payments_MemberBillingSubscriptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Payments_MemberBillingInvoices_MemberBillingSubscriptionId",
                table: "Payments_MemberBillingInvoices",
                column: "MemberBillingSubscriptionId");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_MemberBillingSubscriptions_ExternalSubscriptionId",
                table: "Payments_MemberBillingSubscriptions",
                column: "ExternalSubscriptionId");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_MemberBillingSubscriptions_MemberProfileId",
                table: "Payments_MemberBillingSubscriptions",
                column: "MemberProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_MemberWebhookInbox_IdempotencyKey",
                table: "Payments_MemberWebhookInbox",
                column: "IdempotencyKey",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Payments_MemberBillingInvoices");

            migrationBuilder.DropTable(
                name: "Payments_MemberWebhookInbox");

            migrationBuilder.DropTable(
                name: "Payments_MemberBillingSubscriptions");
        }
    }
}
