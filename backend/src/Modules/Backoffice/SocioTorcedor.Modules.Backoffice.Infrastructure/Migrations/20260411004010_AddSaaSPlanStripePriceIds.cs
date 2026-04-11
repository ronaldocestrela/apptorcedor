using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SocioTorcedor.Modules.Backoffice.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSaaSPlanStripePriceIds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "StripePriceMonthlyId",
                table: "SaaSPlans",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StripePriceYearlyId",
                table: "SaaSPlans",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "StripePriceMonthlyId",
                table: "SaaSPlans");

            migrationBuilder.DropColumn(
                name: "StripePriceYearlyId",
                table: "SaaSPlans");
        }
    }
}
