using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AppTorcedor.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class PartB5PlansAdmin : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsPublished",
                table: "MembershipPlans",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "PublishedAt",
                table: "MembershipPlans",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RulesNotes",
                table: "MembershipPlans",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Summary",
                table: "MembershipPlans",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "MembershipPlanBenefits",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlanId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MembershipPlanBenefits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MembershipPlanBenefits_MembershipPlans_PlanId",
                        column: x => x.PlanId,
                        principalTable: "MembershipPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MembershipPlans_IsPublished",
                table: "MembershipPlans",
                column: "IsPublished");

            migrationBuilder.CreateIndex(
                name: "IX_MembershipPlanBenefits_PlanId",
                table: "MembershipPlanBenefits",
                column: "PlanId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MembershipPlanBenefits");

            migrationBuilder.DropIndex(
                name: "IX_MembershipPlans_IsPublished",
                table: "MembershipPlans");

            migrationBuilder.DropColumn(
                name: "IsPublished",
                table: "MembershipPlans");

            migrationBuilder.DropColumn(
                name: "PublishedAt",
                table: "MembershipPlans");

            migrationBuilder.DropColumn(
                name: "RulesNotes",
                table: "MembershipPlans");

            migrationBuilder.DropColumn(
                name: "Summary",
                table: "MembershipPlans");
        }
    }
}
