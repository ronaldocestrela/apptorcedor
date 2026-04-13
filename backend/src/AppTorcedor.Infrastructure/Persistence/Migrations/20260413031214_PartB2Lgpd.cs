using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AppTorcedor.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class PartB2Lgpd : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LegalDocuments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LegalDocuments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PrivacyRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Kind = table.Column<int>(type: "int", nullable: false),
                    SubjectUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequestedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ResultJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ErrorMessage = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PrivacyRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PrivacyRequests_AspNetUsers_RequestedByUserId",
                        column: x => x.RequestedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PrivacyRequests_AspNetUsers_SubjectUserId",
                        column: x => x.SubjectUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LegalDocumentVersions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LegalDocumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VersionNumber = table.Column<int>(type: "int", nullable: false),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    PublishedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LegalDocumentVersions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LegalDocumentVersions_LegalDocuments_LegalDocumentId",
                        column: x => x.LegalDocumentId,
                        principalTable: "LegalDocuments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserConsents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LegalDocumentVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AcceptedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ClientIp = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserConsents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserConsents_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserConsents_LegalDocumentVersions_LegalDocumentVersionId",
                        column: x => x.LegalDocumentVersionId,
                        principalTable: "LegalDocumentVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LegalDocuments_Type",
                table: "LegalDocuments",
                column: "Type",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LegalDocumentVersions_LegalDocumentId_VersionNumber",
                table: "LegalDocumentVersions",
                columns: new[] { "LegalDocumentId", "VersionNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PrivacyRequests_CreatedAt",
                table: "PrivacyRequests",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_PrivacyRequests_RequestedByUserId",
                table: "PrivacyRequests",
                column: "RequestedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PrivacyRequests_SubjectUserId",
                table: "PrivacyRequests",
                column: "SubjectUserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserConsents_LegalDocumentVersionId",
                table: "UserConsents",
                column: "LegalDocumentVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_UserConsents_UserId",
                table: "UserConsents",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserConsents_UserId_LegalDocumentVersionId",
                table: "UserConsents",
                columns: new[] { "UserId", "LegalDocumentVersionId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PrivacyRequests");

            migrationBuilder.DropTable(
                name: "UserConsents");

            migrationBuilder.DropTable(
                name: "LegalDocumentVersions");

            migrationBuilder.DropTable(
                name: "LegalDocuments");
        }
    }
}
