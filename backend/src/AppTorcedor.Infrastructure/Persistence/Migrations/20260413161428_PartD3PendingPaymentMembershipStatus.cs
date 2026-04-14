using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AppTorcedor.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class PartD3PendingPaymentMembershipStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Parte D.3: novo valor de enum MembershipStatus.PendingPayment (armazenado como int em Memberships.Status).
            // Não há alteração de esquema; migration registra a versão do modelo após a entrega.
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
