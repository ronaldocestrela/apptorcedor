using AppTorcedor.Application.Abstractions;
using AppTorcedor.Identity;
using AppTorcedor.Infrastructure.Entities;
using AppTorcedor.Infrastructure.Persistence;
using AppTorcedor.Infrastructure.Services.Tickets;
using Microsoft.EntityFrameworkCore;

namespace AppTorcedor.Infrastructure.Tests;

public sealed class TicketAdministrationServiceRequestStatusTests
{
    private static async Task<AppDbContext> CreateDbAsync()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new AppDbContext(options);
        await db.Database.EnsureCreatedAsync();
        return db;
    }

    private sealed class NoopLoyalty : ILoyaltyPointsTriggerPort
    {
        public Task AwardPointsForPaymentPaidAsync(Guid paymentId, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task AwardPointsForTicketPurchasedAsync(Guid ticketId, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task AwardPointsForTicketRedeemedAsync(Guid ticketId, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class FailTicketProvider : ITicketProvider
    {
        public Task<TicketProviderReserveResult> ReserveAsync(
            Guid ticketId,
            Guid gameId,
            Guid userId,
            CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException();

        public Task<TicketProviderPurchaseResult> PurchaseAsync(string externalTicketId, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException();

        public Task<TicketProviderSnapshot> GetAsync(string externalTicketId, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException();
    }

    [Fact]
    public async Task ListTicketsAsync_includes_membership_plan_name_and_request_status()
    {
        await using var db = await CreateDbAsync();
        var userId = Guid.NewGuid();
        var planId = Guid.NewGuid();
        var gameId = Guid.NewGuid();
        var ticketId = Guid.NewGuid();

        db.Users.Add(
            new ApplicationUser
            {
                Id = userId,
                UserName = "a@test",
                NormalizedUserName = "A@TEST",
                Email = "a@test",
                NormalizedEmail = "A@TEST",
                EmailConfirmed = true,
                Name = "Aluno",
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow,
            });
        db.MembershipPlans.Add(
            new MembershipPlanRecord
            {
                Id = planId,
                Name = "Plano Ouro",
                Price = 99m,
                BillingCycle = "Monthly",
                DiscountPercentage = 0,
                IsActive = true,
                IsPublished = true,
                PublishedAt = DateTimeOffset.UtcNow,
            });
        db.Memberships.Add(
            new MembershipRecord
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                PlanId = planId,
                Status = MembershipStatus.Ativo,
                StartDate = DateTimeOffset.UtcNow.AddDays(-10),
            });
        db.Games.Add(
            new GameRecord
            {
                Id = gameId,
                Opponent = "A",
                Competition = "B",
                GameDate = DateTimeOffset.UtcNow.AddDays(1),
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow,
            });
        db.Tickets.Add(
            new TicketRecord
            {
                Id = ticketId,
                UserId = userId,
                GameId = gameId,
                Status = TicketStatus.Reserved,
                RequestStatus = TicketRequestStatus.Issued,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow,
            });
        await db.SaveChangesAsync();

        var provider = new MockTicketProvider();
        var sut = new TicketAdministrationService(db, provider, new NoopLoyalty());
        var page = await sut.ListTicketsAsync(null, null, null, "Issued", 1, 20);
        var item = Assert.Single(page.Items);
        Assert.Equal("Plano Ouro", item.MembershipPlanName);
        Assert.Equal("Issued", item.RequestStatus);
    }

    [Fact]
    public async Task UpdateTicketRequestStatusAsync_toggles_status()
    {
        await using var db = await CreateDbAsync();
        var userId = Guid.NewGuid();
        var gameId = Guid.NewGuid();
        var ticketId = Guid.NewGuid();

        db.Users.Add(
            new ApplicationUser
            {
                Id = userId,
                UserName = "b@test",
                NormalizedUserName = "B@TEST",
                Email = "b@test",
                NormalizedEmail = "B@TEST",
                EmailConfirmed = true,
                Name = "B",
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow,
            });
        db.Games.Add(
            new GameRecord
            {
                Id = gameId,
                Opponent = "A",
                Competition = "B",
                GameDate = DateTimeOffset.UtcNow,
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow,
            });
        db.Tickets.Add(
            new TicketRecord
            {
                Id = ticketId,
                UserId = userId,
                GameId = gameId,
                Status = TicketStatus.Reserved,
                RequestStatus = TicketRequestStatus.Pending,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow,
            });
        await db.SaveChangesAsync();

        var provider = new FailTicketProvider();
        var sut = new TicketAdministrationService(db, provider, new NoopLoyalty());
        Assert.True((await sut.UpdateTicketRequestStatusAsync(ticketId, "Issued")).Ok);
        var row = await db.Tickets.AsNoTracking().SingleAsync();
        Assert.Equal(TicketRequestStatus.Issued, row.RequestStatus);
    }

    private sealed class MockTicketProvider : ITicketProvider
    {
        public Task<TicketProviderReserveResult> ReserveAsync(
            Guid ticketId,
            Guid gameId,
            Guid userId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new TicketProviderReserveResult("ext-1", "qr"));

        public Task<TicketProviderPurchaseResult> PurchaseAsync(
            string externalTicketId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new TicketProviderPurchaseResult(externalTicketId, "qr", "Purchased"));

        public Task<TicketProviderSnapshot> GetAsync(
            string externalTicketId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new TicketProviderSnapshot(externalTicketId, "qr", "Reserved"));
    }
}
