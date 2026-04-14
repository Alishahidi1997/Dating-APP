using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using API.Models.Dto;
using Xunit;

namespace API.Tests;

public class AccountAndSubscriptionsIntegrationTests : IClassFixture<ApiWebApplicationFactory>, IDisposable
{
    private readonly HttpClient _client;

    public AccountAndSubscriptionsIntegrationTests(ApiWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    public void Dispose() => _client.Dispose();

    private static string UniqueName() => $"u{Guid.NewGuid():N}"[..14];

    private static async Task<string?> LoginAndGetTokenAsync(HttpClient client, string userName, string password)
    {
        var login = await client.PostAsJsonAsync("/api/account/login", new LoginDto
        {
            UserName = userName,
            Password = password
        });
        login.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await login.Content.ReadAsStringAsync());
        return doc.RootElement.GetProperty("token").GetString();
    }

    [Fact]
    public async Task Register_login_and_plans_are_ok()
    {
        var name = UniqueName();
        var reg = await _client.PostAsJsonAsync("/api/account/register", new RegisterDto
        {
            UserName = name,
            Email = $"{name}@test.com",
            Password = "Aa123456"
        });
        Assert.Equal(HttpStatusCode.OK, reg.StatusCode);

        var token = await LoginAndGetTokenAsync(_client, name, "Aa123456");
        Assert.False(string.IsNullOrEmpty(token));

        var plans = await _client.GetAsync("/api/subscriptions/plans");
        Assert.Equal(HttpStatusCode.OK, plans.StatusCode);
        var list = await plans.Content.ReadFromJsonAsync<List<SubscriptionPlanDto>>();
        Assert.NotNull(list);
        Assert.Contains(list, p => p.Id == 1 && p.Name == "Free");
        Assert.Contains(list, p => p.Id == 2);
    }

    [Fact]
    public async Task Free_user_gets_403_on_followers_list()
    {
        var name = UniqueName();
        await _client.PostAsJsonAsync("/api/account/register", new RegisterDto
        {
            UserName = name,
            Email = $"{name}@test.com",
            Password = "Aa123456"
        });

        var token = await LoginAndGetTokenAsync(_client, name, "Aa123456");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var followers = await _client.GetAsync("/api/users/following?list=followers");
        Assert.Equal(HttpStatusCode.Forbidden, followers.StatusCode);
    }

    [Fact]
    public async Task Subscribe_then_followers_list_allowed()
    {
        var name = UniqueName();
        await _client.PostAsJsonAsync("/api/account/register", new RegisterDto
        {
            UserName = name,
            Email = $"{name}@test.com",
            Password = "Aa123456"
        });

        var token = await LoginAndGetTokenAsync(_client, name, "Aa123456");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var sub = await _client.PostAsJsonAsync("/api/subscriptions/subscribe", new SubscribeDto
        {
            PlanId = 2,
            DurationDays = 30,
            AutoRenew = false,
            RenewalDays = 30
        });
        Assert.Equal(HttpStatusCode.NoContent, sub.StatusCode);

        var followers = await _client.GetAsync("/api/users/following?list=followers");
        Assert.Equal(HttpStatusCode.OK, followers.StatusCode);

        var me = await _client.GetAsync("/api/subscriptions/me");
        Assert.Equal(HttpStatusCode.OK, me.StatusCode);
        var summary = await me.Content.ReadFromJsonAsync<SubscriptionSummaryDto>();
        Assert.NotNull(summary);
        Assert.True(summary.SeeFollowersList);
        Assert.True(summary.IsPaidPlanActive);
    }
}
