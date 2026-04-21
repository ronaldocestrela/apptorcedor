using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace AppTorcedor.Api.Tests;

public class AppWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("UseInMemoryDatabase", "true");
        builder.UseSetting("Testing:InMemoryDatabaseName", Guid.NewGuid().ToString("N"));
        builder.UseSetting("Jwt:Issuer", "test-issuer");
        builder.UseSetting("Jwt:Audience", "test-audience");
        builder.UseSetting("Jwt:Key", "unit-test-signing-key-min-32-bytes!!");
        builder.UseSetting("Jwt:AccessTokenMinutes", "15");
        builder.UseSetting("Jwt:RefreshTokenDays", "14");
        builder.UseSetting("Seed:AdminMaster:Email", "admin@test.local");
        builder.UseSetting("Seed:AdminMaster:Password", "TestPassword123!");
        builder.UseSetting("Testing:SeedSampleUsers", "true");
        builder.UseSetting("Testing:SeedMinimalLegalDocuments", "false");
        builder.UseSetting("Payments:Provider", "Mock");
        builder.UseSetting("Payments:WebhookSecret", "test-webhook-secret");
        builder.UseSetting("ProfilePhotos:Provider", "Local");
        builder.UseSetting("TeamShield:Provider", "Local");
        builder.UseSetting("OpponentLogos:Provider", "Local");
        builder.UseSetting("SupportTicketAttachments:Provider", "Local");
        builder.UseSetting("BenefitOfferBanner:Provider", "Local");
    }
}
