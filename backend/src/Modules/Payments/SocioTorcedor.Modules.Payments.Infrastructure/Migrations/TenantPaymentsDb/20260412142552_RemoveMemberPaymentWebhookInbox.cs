using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SocioTorcedor.Modules.Payments.Infrastructure.Migrations.TenantPaymentsDb
{
    /// <inheritdoc />
    public partial class RemoveMemberPaymentWebhookInbox : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Payments_MemberWebhookInbox");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Payments_MemberWebhookInbox",
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
                    table.PrimaryKey("PK_Payments_MemberWebhookInbox", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Payments_MemberWebhookInbox_IdempotencyKey",
                table: "Payments_MemberWebhookInbox",
                column: "IdempotencyKey",
                unique: true);
        }
    }
}
