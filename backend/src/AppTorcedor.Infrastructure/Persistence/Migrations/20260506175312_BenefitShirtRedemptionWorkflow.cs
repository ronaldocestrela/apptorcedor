using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AppTorcedor.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class BenefitShirtRedemptionWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "RejectionReason",
                table: "BenefitRedemptions",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ReviewedAtUtc",
                table: "BenefitRedemptions",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ReviewedByUserId",
                table: "BenefitRedemptions",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ShirtDisplayName",
                table: "BenefitRedemptions",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ShirtModel",
                table: "BenefitRedemptions",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ShirtNumber",
                table: "BenefitRedemptions",
                type: "nvarchar(8)",
                maxLength: 8,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ShirtSize",
                table: "BenefitRedemptions",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "BenefitRedemptions",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<bool>(
                name: "IsShirtCustomizationOffer",
                table: "BenefitOffers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "BenefitShirtCatalogOptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OfferId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Kind = table.Column<int>(type: "int", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BenefitShirtCatalogOptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BenefitShirtCatalogOptions_BenefitOffers_OfferId",
                        column: x => x.OfferId,
                        principalTable: "BenefitOffers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BenefitRedemptions_Status",
                table: "BenefitRedemptions",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_BenefitShirtCatalogOptions_OfferId",
                table: "BenefitShirtCatalogOptions",
                column: "OfferId");

            migrationBuilder.CreateIndex(
                name: "IX_BenefitShirtCatalogOptions_OfferId_Kind_Value",
                table: "BenefitShirtCatalogOptions",
                columns: new[] { "OfferId", "Kind", "Value" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BenefitShirtCatalogOptions");

            migrationBuilder.DropIndex(
                name: "IX_BenefitRedemptions_Status",
                table: "BenefitRedemptions");

            migrationBuilder.DropColumn(
                name: "RejectionReason",
                table: "BenefitRedemptions");

            migrationBuilder.DropColumn(
                name: "ReviewedAtUtc",
                table: "BenefitRedemptions");

            migrationBuilder.DropColumn(
                name: "ReviewedByUserId",
                table: "BenefitRedemptions");

            migrationBuilder.DropColumn(
                name: "ShirtDisplayName",
                table: "BenefitRedemptions");

            migrationBuilder.DropColumn(
                name: "ShirtModel",
                table: "BenefitRedemptions");

            migrationBuilder.DropColumn(
                name: "ShirtNumber",
                table: "BenefitRedemptions");

            migrationBuilder.DropColumn(
                name: "ShirtSize",
                table: "BenefitRedemptions");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "BenefitRedemptions");

            migrationBuilder.DropColumn(
                name: "IsShirtCustomizationOffer",
                table: "BenefitOffers");
        }
    }
}
