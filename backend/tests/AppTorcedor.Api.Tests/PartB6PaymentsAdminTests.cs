using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using AppTorcedor.Application.Abstractions;
using AppTorcedor.Application.Modules.Administration.Payments;
using AppTorcedor.Identity;
using AppTorcedor.Infrastructure.Entities;
using AppTorcedor.Infrastructure.Persistence;
using AppTorcedor.Infrastructure.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AppTorcedor.Api.Tests;

public sealed class PartB6PaymentsAdminTests(AppWebApplicationFactory factory) : IClassFixture<AppWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task List_payments_requires_pagamentos_visualizar()
    {
        var token = await LoginTorcedorAsync();
        using var req = new HttpRequestMessage(HttpMethod.Get, "/api/admin/payments");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var res = await _client.SendAsync(req);
        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    [Fact]
    public async Task List_payments_returns_ok_for_admin()
    {
        var token = await LoginAdminAsync();
        using var req = new HttpRequestMessage(HttpMethod.Get, "/api/admin/payments?pageSize=50");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var res = await _client.SendAsync(req);
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var body = await res.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(body.TryGetProperty("items", out var items));
        Assert.Equal(JsonValueKind.Array, items.ValueKind);
    }

    [Fact]
    public async Task Delinquency_sweep_marks_overdue_and_membership_inadimplente()
    {
        var paymentId = Guid.NewGuid();
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var m = await db.Memberships.FirstAsync(x => x.Id == TestingSeedConstants.SampleMembershipId);
            m.Status = MembershipStatus.Ativo;
            var now = DateTimeOffset.UtcNow;
            db.Payments.Add(
                new PaymentRecord
                {
                    Id = paymentId,
                    UserId = TestingSeedConstants.SampleMemberUserId,
                    MembershipId = TestingSeedConstants.SampleMembershipId,
                    Amount = 42m,
                    Status = PaymentChargeStatuses.Pending,
                    DueDate = now.AddDays(-2),
                    PaidAt = null,
                    PaymentMethod = "Pix",
                    CreatedAt = now,
                    UpdatedAt = now,
                });
            await db.SaveChangesAsync();
        }

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var sweep = scope.ServiceProvider.GetRequiredService<IPaymentDelinquencySweep>();
            var r = await sweep.RunAsync();
            Assert.True(r.PaymentsMarkedOverdue >= 1);
            Assert.True(r.MembershipsMarkedDelinquent >= 1);
        }

        var token = await LoginAdminAsync();
        using var get = new HttpRequestMessage(
            HttpMethod.Get,
            $"/api/admin/memberships/{TestingSeedConstants.SampleMembershipId}");
        get.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var res = await _client.SendAsync(get);
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var body = await res.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Inadimplente", body.GetProperty("status").GetString());

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Payments.Remove(await db.Payments.SingleAsync(p => p.Id == paymentId));
            var m = await db.Memberships.FirstAsync(x => x.Id == TestingSeedConstants.SampleMembershipId);
            m.Status = MembershipStatus.NaoAssociado;
            await db.SaveChangesAsync();
        }
    }

    [Fact]
    public async Task Conciliate_payment_reactivates_membership_when_no_open_charges()
    {
        var paymentId = Guid.NewGuid();
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var m = await db.Memberships.FirstAsync(x => x.Id == TestingSeedConstants.SampleMembershipId);
            m.Status = MembershipStatus.Inadimplente;
            var now = DateTimeOffset.UtcNow;
            db.Payments.Add(
                new PaymentRecord
                {
                    Id = paymentId,
                    UserId = TestingSeedConstants.SampleMemberUserId,
                    MembershipId = TestingSeedConstants.SampleMembershipId,
                    Amount = 10m,
                    Status = PaymentChargeStatuses.Overdue,
                    DueDate = now.AddDays(-1),
                    CreatedAt = now,
                    UpdatedAt = now,
                });
            await db.SaveChangesAsync();
        }

        var token = await LoginAdminAsync();
        using var post = new HttpRequestMessage(HttpMethod.Post, $"/api/admin/payments/{paymentId}/conciliate");
        post.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        post.Content = JsonContent.Create(new { paidAt = (DateTimeOffset?)null });
        var conc = await _client.SendAsync(post);
        Assert.Equal(HttpStatusCode.NoContent, conc.StatusCode);

        using var get = new HttpRequestMessage(
            HttpMethod.Get,
            $"/api/admin/memberships/{TestingSeedConstants.SampleMembershipId}");
        get.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var res = await _client.SendAsync(get);
        var body = await res.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Ativo", body.GetProperty("status").GetString());

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Payments.Remove(await db.Payments.SingleAsync(p => p.Id == paymentId));
            var m = await db.Memberships.FirstAsync(x => x.Id == TestingSeedConstants.SampleMembershipId);
            m.Status = MembershipStatus.NaoAssociado;
            await db.SaveChangesAsync();
        }
    }

    [Fact]
    public async Task Refund_paid_payment_roundtrip()
    {
        var paymentId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Payments.Add(
                new PaymentRecord
                {
                    Id = paymentId,
                    UserId = TestingSeedConstants.SampleMemberUserId,
                    MembershipId = TestingSeedConstants.SampleMembershipId,
                    Amount = 5m,
                    Status = PaymentChargeStatuses.Paid,
                    DueDate = now.AddDays(-1),
                    PaidAt = now,
                    CreatedAt = now,
                    UpdatedAt = now,
                });
            await db.SaveChangesAsync();
        }

        var token = await LoginAdminAsync();
        using var post = new HttpRequestMessage(HttpMethod.Post, $"/api/admin/payments/{paymentId}/refund");
        post.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        post.Content = JsonContent.Create(new { reason = "Test refund" });
        var refund = await _client.SendAsync(post);
        Assert.Equal(HttpStatusCode.NoContent, refund.StatusCode);

        using var get = new HttpRequestMessage(HttpMethod.Get, $"/api/admin/payments/{paymentId}");
        get.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var res = await _client.SendAsync(get);
        var detail = await res.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(PaymentChargeStatuses.Refunded, detail.GetProperty("status").GetString());

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Payments.Remove(await db.Payments.SingleAsync(p => p.Id == paymentId));
            await db.SaveChangesAsync();
        }
    }

    [Fact]
    public async Task Conciliate_paid_payment_returns_bad_request()
    {
        var paymentId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Payments.Add(
                new PaymentRecord
                {
                    Id = paymentId,
                    UserId = TestingSeedConstants.SampleMemberUserId,
                    MembershipId = TestingSeedConstants.SampleMembershipId,
                    Amount = 5m,
                    Status = PaymentChargeStatuses.Paid,
                    DueDate = now,
                    PaidAt = now,
                    CreatedAt = now,
                    UpdatedAt = now,
                });
            await db.SaveChangesAsync();
        }

        var token = await LoginAdminAsync();
        using var post = new HttpRequestMessage(HttpMethod.Post, $"/api/admin/payments/{paymentId}/conciliate");
        post.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        post.Content = JsonContent.Create(new { });
        var res = await _client.SendAsync(post);
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Payments.Remove(await db.Payments.SingleAsync(p => p.Id == paymentId));
            await db.SaveChangesAsync();
        }
    }

    private async Task<string> LoginTorcedorAsync()
    {
        using var login = new HttpRequestMessage(HttpMethod.Post, "/api/auth/login")
        {
            Content = JsonContent.Create(
                new { email = TestingSeedConstants.TorcedorEmail, password = "TestPassword123!" }),
        };
        var res = await _client.SendAsync(login);
        res.EnsureSuccessStatusCode();
        var body = await res.Content.ReadFromJsonAsync<JsonElement>();
        return body.GetProperty("accessToken").GetString()!;
    }

    private async Task<string> LoginAdminAsync()
    {
        using var login = new HttpRequestMessage(HttpMethod.Post, "/api/auth/login")
        {
            Content = JsonContent.Create(new { email = "admin@test.local", password = "TestPassword123!" }),
        };
        var res = await _client.SendAsync(login);
        res.EnsureSuccessStatusCode();
        var body = await res.Content.ReadFromJsonAsync<JsonElement>();
        return body.GetProperty("accessToken").GetString()!;
    }
}
