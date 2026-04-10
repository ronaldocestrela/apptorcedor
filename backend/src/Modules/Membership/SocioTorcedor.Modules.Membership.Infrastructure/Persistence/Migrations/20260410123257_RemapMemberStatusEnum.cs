using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SocioTorcedor.Modules.Membership.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RemapMemberStatusEnum : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Legacy: Inactive=2 -> Canceled=3, Suspended=3 -> Suspended=4 (new enum layout).
            migrationBuilder.Sql(
                """
                UPDATE MemberProfiles SET Status = 3 WHERE Status = 2;
                UPDATE MemberProfiles SET Status = 4 WHERE Status = 3;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder) =>
            throw new NotSupportedException(
                "Reverting MemberStatus enum remap is not supported (ambiguous mapping between legacy Inactive and Suspended).");
    }
}
