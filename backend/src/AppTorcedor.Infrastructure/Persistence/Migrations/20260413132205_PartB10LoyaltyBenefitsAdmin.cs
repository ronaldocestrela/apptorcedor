using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AppTorcedor.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class PartB10LoyaltyBenefitsAdmin : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BenefitPartners",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BenefitPartners", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LoyaltyCampaigns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    PublishedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UnpublishedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoyaltyCampaigns", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BenefitOffers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PartnerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    StartAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    EndAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BenefitOffers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BenefitOffers_BenefitPartners_PartnerId",
                        column: x => x.PartnerId,
                        principalTable: "BenefitPartners",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LoyaltyPointLedgerEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CampaignId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RuleId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Points = table.Column<int>(type: "int", nullable: false),
                    SourceType = table.Column<int>(type: "int", nullable: false),
                    SourceKey = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ActorUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoyaltyPointLedgerEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LoyaltyPointLedgerEntries_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LoyaltyPointLedgerEntries_LoyaltyCampaigns_CampaignId",
                        column: x => x.CampaignId,
                        principalTable: "LoyaltyCampaigns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "LoyaltyPointRules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CampaignId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Trigger = table.Column<int>(type: "int", nullable: false),
                    Points = table.Column<int>(type: "int", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoyaltyPointRules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LoyaltyPointRules_LoyaltyCampaigns_CampaignId",
                        column: x => x.CampaignId,
                        principalTable: "LoyaltyCampaigns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BenefitOfferMembershipStatusEligibilities",
                columns: table => new
                {
                    OfferId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BenefitOfferMembershipStatusEligibilities", x => new { x.OfferId, x.Status });
                    table.ForeignKey(
                        name: "FK_BenefitOfferMembershipStatusEligibilities_BenefitOffers_OfferId",
                        column: x => x.OfferId,
                        principalTable: "BenefitOffers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BenefitOfferPlanEligibilities",
                columns: table => new
                {
                    OfferId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlanId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BenefitOfferPlanEligibilities", x => new { x.OfferId, x.PlanId });
                    table.ForeignKey(
                        name: "FK_BenefitOfferPlanEligibilities_BenefitOffers_OfferId",
                        column: x => x.OfferId,
                        principalTable: "BenefitOffers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BenefitOfferPlanEligibilities_MembershipPlans_PlanId",
                        column: x => x.PlanId,
                        principalTable: "MembershipPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BenefitRedemptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OfferId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ActorUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BenefitRedemptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BenefitRedemptions_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BenefitRedemptions_BenefitOffers_OfferId",
                        column: x => x.OfferId,
                        principalTable: "BenefitOffers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BenefitOfferPlanEligibilities_PlanId",
                table: "BenefitOfferPlanEligibilities",
                column: "PlanId");

            migrationBuilder.CreateIndex(
                name: "IX_BenefitOffers_IsActive_StartAt_EndAt",
                table: "BenefitOffers",
                columns: new[] { "IsActive", "StartAt", "EndAt" });

            migrationBuilder.CreateIndex(
                name: "IX_BenefitOffers_PartnerId",
                table: "BenefitOffers",
                column: "PartnerId");

            migrationBuilder.CreateIndex(
                name: "IX_BenefitPartners_IsActive",
                table: "BenefitPartners",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_BenefitRedemptions_CreatedAt",
                table: "BenefitRedemptions",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_BenefitRedemptions_OfferId",
                table: "BenefitRedemptions",
                column: "OfferId");

            migrationBuilder.CreateIndex(
                name: "IX_BenefitRedemptions_UserId",
                table: "BenefitRedemptions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_LoyaltyCampaigns_Status",
                table: "LoyaltyCampaigns",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_LoyaltyPointLedgerEntries_CampaignId",
                table: "LoyaltyPointLedgerEntries",
                column: "CampaignId");

            migrationBuilder.CreateIndex(
                name: "IX_LoyaltyPointLedgerEntries_CreatedAt",
                table: "LoyaltyPointLedgerEntries",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_LoyaltyPointLedgerEntries_SourceType_SourceKey",
                table: "LoyaltyPointLedgerEntries",
                columns: new[] { "SourceType", "SourceKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LoyaltyPointLedgerEntries_UserId",
                table: "LoyaltyPointLedgerEntries",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_LoyaltyPointRules_CampaignId",
                table: "LoyaltyPointRules",
                column: "CampaignId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BenefitOfferMembershipStatusEligibilities");

            migrationBuilder.DropTable(
                name: "BenefitOfferPlanEligibilities");

            migrationBuilder.DropTable(
                name: "BenefitRedemptions");

            migrationBuilder.DropTable(
                name: "LoyaltyPointLedgerEntries");

            migrationBuilder.DropTable(
                name: "LoyaltyPointRules");

            migrationBuilder.DropTable(
                name: "BenefitOffers");

            migrationBuilder.DropTable(
                name: "LoyaltyCampaigns");

            migrationBuilder.DropTable(
                name: "BenefitPartners");
        }
    }
}
