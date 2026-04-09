using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SocioTorcedor.Modules.Backoffice.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialBackoffice : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SaaSPlans",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    MonthlyPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    YearlyPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    MaxMembers = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SaaSPlans", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SaaSPlanFeatures",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SaaSPlanId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Key = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    Value = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SaaSPlanFeatures", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SaaSPlanFeatures_SaaSPlans_SaaSPlanId",
                        column: x => x.SaaSPlanId,
                        principalTable: "SaaSPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TenantPlans",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SaaSPlanId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    BillingCycle = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantPlans", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TenantPlans_SaaSPlans_SaaSPlanId",
                        column: x => x.SaaSPlanId,
                        principalTable: "SaaSPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SaaSPlanFeatures_SaaSPlanId",
                table: "SaaSPlanFeatures",
                column: "SaaSPlanId");

            migrationBuilder.CreateIndex(
                name: "IX_TenantPlans_SaaSPlanId",
                table: "TenantPlans",
                column: "SaaSPlanId");

            migrationBuilder.CreateIndex(
                name: "IX_TenantPlans_TenantId_Status",
                table: "TenantPlans",
                columns: new[] { "TenantId", "Status" },
                unique: true,
                filter: "[Status] = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SaaSPlanFeatures");

            migrationBuilder.DropTable(
                name: "TenantPlans");

            migrationBuilder.DropTable(
                name: "SaaSPlans");
        }
    }
}
