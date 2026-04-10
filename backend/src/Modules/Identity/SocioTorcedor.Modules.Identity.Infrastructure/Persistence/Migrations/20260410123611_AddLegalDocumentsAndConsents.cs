using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SocioTorcedor.Modules.Identity.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddLegalDocumentsAndConsents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LegalDocumentVersions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Kind = table.Column<int>(type: "int", nullable: false),
                    VersionNumber = table.Column<int>(type: "int", nullable: false),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PublishedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsCurrent = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LegalDocumentVersions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserLegalConsents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Kind = table.Column<int>(type: "int", nullable: false),
                    LegalDocumentVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AcceptedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IpAddress = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserLegalConsents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserLegalConsents_LegalDocumentVersions_LegalDocumentVersionId",
                        column: x => x.LegalDocumentVersionId,
                        principalTable: "LegalDocumentVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LegalDocumentVersions_Kind_IsCurrent",
                table: "LegalDocumentVersions",
                columns: new[] { "Kind", "IsCurrent" });

            migrationBuilder.CreateIndex(
                name: "IX_UserLegalConsents_LegalDocumentVersionId",
                table: "UserLegalConsents",
                column: "LegalDocumentVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_UserLegalConsents_UserId_Kind_AcceptedAtUtc",
                table: "UserLegalConsents",
                columns: new[] { "UserId", "Kind", "AcceptedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserLegalConsents");

            migrationBuilder.DropTable(
                name: "LegalDocumentVersions");
        }
    }
}
