using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SocioTorcedor.Modules.Membership.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialMembership : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MemberProfiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    CpfDigits = table.Column<string>(type: "nvarchar(11)", maxLength: 11, nullable: false),
                    DateOfBirth = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Gender = table.Column<int>(type: "int", nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Address_Street = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Address_Number = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Address_Complement = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    Address_Neighborhood = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Address_City = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Address_State = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: false),
                    Address_ZipCode = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MemberProfiles", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MemberProfiles_CpfDigits",
                table: "MemberProfiles",
                column: "CpfDigits",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MemberProfiles_UserId",
                table: "MemberProfiles",
                column: "UserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MemberProfiles");
        }
    }
}
