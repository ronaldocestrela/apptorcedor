using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AppTorcedor.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class BenefitOfferBanner : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BannerUrl",
                table: "BenefitOffers",
                type: "nvarchar(2048)",
                maxLength: 2048,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BannerUrl",
                table: "BenefitOffers");
        }
    }
}
