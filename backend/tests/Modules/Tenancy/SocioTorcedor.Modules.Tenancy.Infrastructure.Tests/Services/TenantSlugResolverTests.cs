using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using SocioTorcedor.Modules.Tenancy.Domain.Entities;
using SocioTorcedor.Modules.Tenancy.Infrastructure.Persistence;
using SocioTorcedor.Modules.Tenancy.Infrastructure.Repositories;
using SocioTorcedor.Modules.Tenancy.Infrastructure.Services;

namespace SocioTorcedor.Modules.Tenancy.Infrastructure.Tests.Services;

public class TenantSlugResolverTests
{
    [Fact]
    public async Task ResolveAsync_returns_null_for_unknown_slug()
    {
        var options = new DbContextOptionsBuilder<MasterDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var ctx = new MasterDbContext(options);
        var repo = new TenantRepository(ctx);
        var resolver = new TenantSlugResolver(repo, new MemoryCache(new MemoryCacheOptions()));

        var result = await resolver.ResolveAsync("missing", CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task ResolveAsync_maps_tenant_and_uses_cache_on_second_call()
    {
        var dbName = Guid.NewGuid().ToString();
        var options = new DbContextOptionsBuilder<MasterDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;

        await using (var ctx = new MasterDbContext(options))
        {
            var tenant = Tenant.Create("FFC", "ffc", "cs", () => false);
            tenant.AddAllowedOrigin("https://ffc.app");
            ctx.Tenants.Add(tenant);
            await ctx.SaveChangesAsync();
        }

        await using var readCtx = new MasterDbContext(options);
        var repo = new TenantRepository(readCtx);
        var cache = new MemoryCache(new MemoryCacheOptions());
        var resolver = new TenantSlugResolver(repo, cache);

        var first = await resolver.ResolveAsync("ffc", CancellationToken.None);
        var second = await resolver.ResolveAsync("ffc", CancellationToken.None);

        first.Should().NotBeNull();
        first!.Slug.Should().Be("ffc");
        first.AllowedOrigins.Should().Contain("https://ffc.app");
        second.Should().BeEquivalentTo(first);
    }
}
