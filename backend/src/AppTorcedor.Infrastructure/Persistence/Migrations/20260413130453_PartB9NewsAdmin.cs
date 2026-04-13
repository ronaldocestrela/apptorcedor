using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AppTorcedor.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class PartB9NewsAdmin : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "NewsArticles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Summary = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    PublishedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UnpublishedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NewsArticles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "InAppNotifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NewsArticleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    PreviewText = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ScheduledAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    DispatchedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InAppNotifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InAppNotifications_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_InAppNotifications_NewsArticles_NewsArticleId",
                        column: x => x.NewsArticleId,
                        principalTable: "NewsArticles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InAppNotifications_NewsArticleId",
                table: "InAppNotifications",
                column: "NewsArticleId");

            migrationBuilder.CreateIndex(
                name: "IX_InAppNotifications_Status_ScheduledAt",
                table: "InAppNotifications",
                columns: new[] { "Status", "ScheduledAt" });

            migrationBuilder.CreateIndex(
                name: "IX_InAppNotifications_UserId_Status",
                table: "InAppNotifications",
                columns: new[] { "UserId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_NewsArticles_Status",
                table: "NewsArticles",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_NewsArticles_UpdatedAt",
                table: "NewsArticles",
                column: "UpdatedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InAppNotifications");

            migrationBuilder.DropTable(
                name: "NewsArticles");
        }
    }
}
