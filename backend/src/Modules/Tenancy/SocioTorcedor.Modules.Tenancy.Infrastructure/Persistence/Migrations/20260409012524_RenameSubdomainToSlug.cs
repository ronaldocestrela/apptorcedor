using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SocioTorcedor.Modules.Tenancy.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RenameSubdomainToSlug : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Subdomain",
                table: "Tenants",
                newName: "Slug");

            migrationBuilder.RenameIndex(
                name: "IX_Tenants_Subdomain",
                table: "Tenants",
                newName: "IX_Tenants_Slug");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Slug",
                table: "Tenants",
                newName: "Subdomain");

            migrationBuilder.RenameIndex(
                name: "IX_Tenants_Slug",
                table: "Tenants",
                newName: "IX_Tenants_Subdomain");
        }
    }
}
