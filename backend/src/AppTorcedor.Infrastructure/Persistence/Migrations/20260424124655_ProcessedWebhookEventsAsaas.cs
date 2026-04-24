using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AppTorcedor.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class ProcessedWebhookEventsAsaas : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.RenameTable(
            name: "ProcessedStripeWebhookEvents",
            newName: "ProcessedWebhookEvents");

        migrationBuilder.RenameIndex(
            name: "IX_ProcessedStripeWebhookEvents_ProcessedAtUtc",
            table: "ProcessedWebhookEvents",
            newName: "IX_ProcessedWebhookEvents_ProcessedAtUtc");

        migrationBuilder.AddColumn<string>(
            name: "Provider",
            table: "ProcessedWebhookEvents",
            type: "nvarchar(32)",
            maxLength: 32,
            nullable: false,
            defaultValue: "Stripe");

        migrationBuilder.DropPrimaryKey(
            name: "PK_ProcessedStripeWebhookEvents",
            table: "ProcessedWebhookEvents");

        migrationBuilder.AddPrimaryKey(
            name: "PK_ProcessedWebhookEvents",
            table: "ProcessedWebhookEvents",
            columns: new[] { "Provider", "EventId" });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropPrimaryKey(
            name: "PK_ProcessedWebhookEvents",
            table: "ProcessedWebhookEvents");

        migrationBuilder.DropColumn(
            name: "Provider",
            table: "ProcessedWebhookEvents");

        migrationBuilder.AddPrimaryKey(
            name: "PK_ProcessedStripeWebhookEvents",
            table: "ProcessedWebhookEvents",
            columns: new[] { "EventId" });

        migrationBuilder.RenameIndex(
            name: "IX_ProcessedWebhookEvents_ProcessedAtUtc",
            table: "ProcessedWebhookEvents",
            newName: "IX_ProcessedStripeWebhookEvents_ProcessedAtUtc");

        migrationBuilder.RenameTable(
            name: "ProcessedWebhookEvents",
            newName: "ProcessedStripeWebhookEvents");
    }
}
