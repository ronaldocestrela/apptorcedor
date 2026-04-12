using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SocioTorcedor.Modules.Payments.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MemberGatewayRemoveStripeConnect : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Payments_ConnectStripeWebhookInbox");

            migrationBuilder.DropTable(
                name: "Payments_TenantStripeConnectAccounts");

            migrationBuilder.CreateTable(
                name: "Payments_MemberStripeWebhookInbox",
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
                    table.PrimaryKey("PK_Payments_MemberStripeWebhookInbox", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Payments_TenantMemberGatewayConfigurations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SelectedProvider = table.Column<int>(type: "int", nullable: false),
                    ProtectedCredentials = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Payments_TenantMemberGatewayConfigurations", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Payments_MemberStripeWebhookInbox_IdempotencyKey",
                table: "Payments_MemberStripeWebhookInbox",
                column: "IdempotencyKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Payments_TenantMemberGatewayConfigurations_TenantId",
                table: "Payments_TenantMemberGatewayConfigurations",
                column: "TenantId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Payments_MemberStripeWebhookInbox");

            migrationBuilder.DropTable(
                name: "Payments_TenantMemberGatewayConfigurations");

            migrationBuilder.CreateTable(
                name: "Payments_ConnectStripeWebhookInbox",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EventType = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    IdempotencyKey = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    LastError = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ProcessedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RawPayload = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Payments_ConnectStripeWebhookInbox", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Payments_TenantStripeConnectAccounts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ChargesEnabled = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DetailsSubmitted = table.Column<bool>(type: "bit", nullable: false),
                    OnboardingStatus = table.Column<int>(type: "int", nullable: false),
                    PayoutsEnabled = table.Column<bool>(type: "bit", nullable: false),
                    StripeAccountId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Payments_TenantStripeConnectAccounts", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Payments_ConnectStripeWebhookInbox_IdempotencyKey",
                table: "Payments_ConnectStripeWebhookInbox",
                column: "IdempotencyKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Payments_TenantStripeConnectAccounts_StripeAccountId",
                table: "Payments_TenantStripeConnectAccounts",
                column: "StripeAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_TenantStripeConnectAccounts_TenantId",
                table: "Payments_TenantStripeConnectAccounts",
                column: "TenantId",
                unique: true);
        }
    }
}
