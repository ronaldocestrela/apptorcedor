using Microsoft.EntityFrameworkCore;
using SocioTorcedor.BuildingBlocks.Domain.Abstractions;

namespace SocioTorcedor.BuildingBlocks.Infrastructure.Persistence;

public abstract class BaseDbContext : DbContext
{
    protected BaseDbContext(DbContextOptions options)
        : base(options)
    {
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var aggregates = ChangeTracker
            .Entries<AggregateRoot>()
            .Where(e => e.Entity.DomainEvents.Count > 0)
            .Select(e => e.Entity)
            .ToList();

        var domainEvents = aggregates
            .SelectMany(a => a.DomainEvents.ToList())
            .ToList();

        foreach (var aggregate in aggregates)
            aggregate.ClearDomainEvents();

        var result = await base.SaveChangesAsync(cancellationToken);

        if (domainEvents.Count > 0)
            await DispatchDomainEventsAsync(domainEvents, cancellationToken);

        return result;
    }

    protected virtual Task DispatchDomainEventsAsync(
        IReadOnlyList<IDomainEvent> domainEvents,
        CancellationToken cancellationToken)
        => Task.CompletedTask;
}
