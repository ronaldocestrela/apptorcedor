using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AppTorcedor.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class OpponentLogoGameLibrary : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "OpponentLogoUrl",
                table: "Games",
                type: "nvarchar(2048)",
                maxLength: 2048,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "OpponentLogoAssets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PublicUrl = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OpponentLogoAssets", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OpponentLogoAssets_CreatedAt",
                table: "OpponentLogoAssets",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_OpponentLogoAssets_PublicUrl",
                table: "OpponentLogoAssets",
                column: "PublicUrl",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OpponentLogoAssets");

            migrationBuilder.DropColumn(
                name: "OpponentLogoUrl",
                table: "Games");
        }
    }
}
