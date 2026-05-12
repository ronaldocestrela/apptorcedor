using AppTorcedor.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AppTorcedor.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
[DbContext(typeof(AppDbContext))]
[Migration("20260511120000_PartnerApiKeys")]
public partial class PartnerApiKeys : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "PartnerApiKeys",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                KeyHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                KeyPrefix = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                LastUsedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_PartnerApiKeys", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_PartnerApiKeys_KeyHash",
            table: "PartnerApiKeys",
            column: "KeyHash",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_PartnerApiKeys_IsActive",
            table: "PartnerApiKeys",
            column: "IsActive");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "PartnerApiKeys");
    }
}
