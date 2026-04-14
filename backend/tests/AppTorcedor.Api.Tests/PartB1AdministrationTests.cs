using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using AppTorcedor.Infrastructure.Testing;
using AppTorcedor.Identity;
using Xunit;

namespace AppTorcedor.Api.Tests;

public sealed class PartB1AdministrationTests(AppWebApplicationFactory factory) : IClassFixture<AppWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Staff_invite_flow_create_accept_list_deactivate()
    {
        var adminToken = await LoginAdminAsync();
        var staffEmail = $"staff-{Guid.NewGuid():N}@test.local";

        string? plainToken;
        using (var create = new HttpRequestMessage(HttpMethod.Post, "/api/admin/staff/invites"))
        {
            create.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
            create.Content = JsonContent.Create(
                new { email = staffEmail, name = "Staff Test", roles = new[] { SystemRoles.Operador } });
            var created = await _client.SendAsync(create);
            Assert.Equal(HttpStatusCode.OK, created.StatusCode);
            var inviteBody = await created.Content.ReadFromJsonAsync<CreateInviteResponseDto>();
            plainToken = inviteBody?.Token;
            Assert.False(string.IsNullOrEmpty(plainToken));
        }

        using (var listInv = new HttpRequestMessage(HttpMethod.Get, "/api/admin/staff/invites"))
        {
            listInv.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
            var listRes = await _client.SendAsync(listInv);
            Assert.Equal(HttpStatusCode.OK, listRes.StatusCode);
            var invites = await listRes.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal(JsonValueKind.Array, invites.ValueKind);
            var match = invites.EnumerateArray().FirstOrDefault(e => e.GetProperty("email").GetString() == staffEmail);
            Assert.NotEqual(default, match);
        }

        using (var create2 = new HttpRequestMessage(HttpMethod.Post, "/api/admin/staff/invites"))
        {
            create2.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
            create2.Content = JsonContent.Create(
                new { email = staffEmail, name = "Dup", roles = new[] { SystemRoles.Operador } });
            var dup = await _client.SendAsync(create2);
            Assert.Equal(HttpStatusCode.BadRequest, dup.StatusCode);
        }

        var accept = await _client.PostAsJsonAsync(
            "/api/auth/accept-staff-invite",
            new { token = plainToken, password = "StaffPass123!", name = (string?)null });
        Assert.Equal(HttpStatusCode.OK, accept.StatusCode);
        var auth = await accept.Content.ReadFromJsonAsync<AuthResponseDto>();
        Assert.NotNull(auth);
        Assert.Contains(SystemRoles.Operador, auth!.Roles);

        var acceptTwice = await _client.PostAsJsonAsync(
            "/api/auth/accept-staff-invite",
            new { token = plainToken, password = "StaffPass123!", name = (string?)null });
        Assert.Equal(HttpStatusCode.Unauthorized, acceptTwice.StatusCode);

        using (var listUsers = new HttpRequestMessage(HttpMethod.Get, "/api/admin/staff/users"))
        {
            listUsers.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
            var usersRes = await _client.SendAsync(listUsers);
            Assert.Equal(HttpStatusCode.OK, usersRes.StatusCode);
            var users = await usersRes.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Contains(
                users.EnumerateArray(),
                u => u.GetProperty("email").GetString() == staffEmail);
        }

        Guid staffUserId;
        using (var meReq = new HttpRequestMessage(HttpMethod.Get, "/api/auth/me"))
        {
            meReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
            var meRes = await _client.SendAsync(meReq);
            var me = await meRes.Content.ReadFromJsonAsync<MeResponseDto>();
            Assert.NotNull(me);
            staffUserId = me!.Id;
        }

        using (var patch = new HttpRequestMessage(HttpMethod.Patch, $"/api/admin/staff/users/{staffUserId}/active"))
        {
            patch.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
            patch.Content = JsonContent.Create(new { isActive = false });
            var patched = await _client.SendAsync(patch);
            Assert.Equal(HttpStatusCode.NoContent, patched.StatusCode);
        }

        var loginAfter = await _client.PostAsJsonAsync(
            "/api/auth/login",
            new { email = staffEmail, password = "StaffPass123!" });
        Assert.Equal(HttpStatusCode.Unauthorized, loginAfter.StatusCode);

        using (var inviteAfterUser = new HttpRequestMessage(HttpMethod.Post, "/api/admin/staff/invites"))
        {
            inviteAfterUser.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
            inviteAfterUser.Content = JsonContent.Create(
                new { email = staffEmail, name = "Again", roles = new[] { SystemRoles.Operador } });
            var again = await _client.SendAsync(inviteAfterUser);
            Assert.Equal(HttpStatusCode.BadRequest, again.StatusCode);
        }
    }

    [Fact]
    public async Task Torcedor_cannot_create_staff_invite()
    {
        var login = await _client.PostAsJsonAsync(
            "/api/auth/login",
            new { email = TestingSeedConstants.TorcedorEmail, password = "TestPassword123!" });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var tokens = await login.Content.ReadFromJsonAsync<AuthResponseDto>();
        Assert.NotNull(tokens);

        using var req = new HttpRequestMessage(HttpMethod.Post, "/api/admin/staff/invites");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokens!.AccessToken);
        req.Content = JsonContent.Create(
            new { email = "x@test.local", name = "X", roles = new[] { SystemRoles.Operador } });
        var res = await _client.SendAsync(req);
        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    [Fact]
    public async Task Admin_can_replace_role_permissions_and_read_dashboard()
    {
        var token = await LoginAdminAsync();

        using (var put = new HttpRequestMessage(HttpMethod.Put, "/api/admin/role-permissions"))
        {
            put.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            put.Content = JsonContent.Create(
                new
                {
                    roleName = SystemRoles.Administrador,
                    permissionNames = new[] { ApplicationPermissions.UsuariosVisualizar },
                });
            var putRes = await _client.SendAsync(put);
            Assert.Equal(HttpStatusCode.NoContent, putRes.StatusCode);
        }

        using (var get = new HttpRequestMessage(HttpMethod.Get, "/api/admin/role-permissions"))
        {
            get.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var getRes = await _client.SendAsync(get);
            Assert.Equal(HttpStatusCode.OK, getRes.StatusCode);
            var rows = await getRes.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Contains(
                rows.EnumerateArray(),
                r =>
                    r.GetProperty("roleName").GetString() == SystemRoles.Administrador
                    && r.GetProperty("permissionName").GetString() == ApplicationPermissions.UsuariosVisualizar);
        }

        using (var dash = new HttpRequestMessage(HttpMethod.Get, "/api/admin/dashboard"))
        {
            dash.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var dRes = await _client.SendAsync(dash);
            Assert.Equal(HttpStatusCode.OK, dRes.StatusCode);
            var body = await dRes.Content.ReadFromJsonAsync<DashboardDto>();
            Assert.NotNull(body);
            Assert.True(body!.ActiveMembersCount >= 0);
            Assert.True(body.DelinquentMembersCount >= 0);
            Assert.Equal(0, body.OpenSupportTickets);
        }
    }

    [Fact]
    public async Task Replace_master_role_permissions_empty_is_bad_request()
    {
        var token = await LoginAdminAsync();
        using var put = new HttpRequestMessage(HttpMethod.Put, "/api/admin/role-permissions");
        put.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        put.Content = JsonContent.Create(
            new { roleName = SystemRoles.AdministradorMaster, permissionNames = Array.Empty<string>() });
        var putRes = await _client.SendAsync(put);
        Assert.Equal(HttpStatusCode.BadRequest, putRes.StatusCode);
    }

    private async Task<string> LoginAdminAsync()
    {
        var login = await _client.PostAsJsonAsync(
            "/api/auth/login",
            new { email = "admin@test.local", password = "TestPassword123!" });
        login.EnsureSuccessStatusCode();
        var tokens = await login.Content.ReadFromJsonAsync<AuthResponseDto>();
        Assert.False(string.IsNullOrEmpty(tokens?.AccessToken));
        return tokens!.AccessToken;
    }

    private sealed record AuthResponseDto(
        string AccessToken,
        string RefreshToken,
        int ExpiresIn,
        IReadOnlyList<string> Roles);

    private sealed record MeResponseDto(
        Guid Id,
        string Email,
        string Name,
        IReadOnlyList<string> Roles,
        IReadOnlyList<string> Permissions,
        bool RequiresProfileCompletion);

    private sealed record CreateInviteResponseDto(Guid Id, string Token, DateTimeOffset ExpiresAt);

    private sealed record DashboardDto(int ActiveMembersCount, int DelinquentMembersCount, int OpenSupportTickets);
}
