using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using API.Models.Dto;
using Xunit;

namespace API.Tests;

public class SocialProfileAndFollowIntegrationTests : IClassFixture<ApiWebApplicationFactory>, IDisposable
{
    private readonly HttpClient _client;

    public SocialProfileAndFollowIntegrationTests(ApiWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    public void Dispose() => _client.Dispose();

    private static string UniqueName() => $"s{Guid.NewGuid():N}"[..14];

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
    public async Task Profile_update_exposes_headline_and_links_on_get_user()
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
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var put = await _client.PutAsJsonAsync("/api/users", new MemberUpdateDto
        {
            Headline = "Building calm software",
            ProfileLinks = "https://example.org/me",
            KnownAs = "Social Tester"
        });
        Assert.Equal(HttpStatusCode.NoContent, put.StatusCode);

        var profile = await _client.GetAsync($"/api/users/{name}");
        Assert.Equal(HttpStatusCode.OK, profile.StatusCode);
        var dto = await profile.Content.ReadFromJsonAsync<UserDto>();
        Assert.NotNull(dto);
        Assert.Equal("Building calm software", dto.Headline);
        Assert.Equal("https://example.org/me", dto.ProfileLinks);
        Assert.Equal("Social Tester", dto.KnownAs);
    }

    [Fact]
    public async Task Follow_mutual_connection_and_bookmark()
    {
        var a = UniqueName();
        var b = UniqueName();

        var regA = await _client.PostAsJsonAsync("/api/account/register", new RegisterDto
        {
            UserName = a,
            Email = $"{a}@test.com",
            Password = "Aa123456"
        });
        var regB = await _client.PostAsJsonAsync("/api/account/register", new RegisterDto
        {
            UserName = b,
            Email = $"{b}@test.com",
            Password = "Aa123456"
        });
        Assert.Equal(HttpStatusCode.OK, regA.StatusCode);
        Assert.Equal(HttpStatusCode.OK, regB.StatusCode);

        using var docA = JsonDocument.Parse(await regA.Content.ReadAsStringAsync());
        using var docB = JsonDocument.Parse(await regB.Content.ReadAsStringAsync());
        var idA = docA.RootElement.GetProperty("user").GetProperty("id").GetInt32();
        var idB = docB.RootElement.GetProperty("user").GetProperty("id").GetInt32();
        var tokenA = docA.RootElement.GetProperty("token").GetString();
        var tokenB = docB.RootElement.GetProperty("token").GetString();
        Assert.False(string.IsNullOrEmpty(tokenA));
        Assert.False(string.IsNullOrEmpty(tokenB));

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenA);
        var f1 = await _client.PostAsync($"/api/follow/{idB}", null);
        Assert.Equal(HttpStatusCode.OK, f1.StatusCode);

        var connectionsBefore = await _client.GetAsync("/api/users/connections");
        Assert.Equal(HttpStatusCode.OK, connectionsBefore.StatusCode);
        var listBefore = await connectionsBefore.Content.ReadFromJsonAsync<List<UserDto>>();
        Assert.NotNull(listBefore);
        Assert.DoesNotContain(listBefore, u => u.Id == idB);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenB);
        var f2 = await _client.PostAsync($"/api/follow/{idA}", null);
        Assert.Equal(HttpStatusCode.OK, f2.StatusCode);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenA);
        var connectionsAfter = await _client.GetAsync("/api/users/connections");
        Assert.Equal(HttpStatusCode.OK, connectionsAfter.StatusCode);
        var listAfter = await connectionsAfter.Content.ReadFromJsonAsync<List<UserDto>>();
        Assert.NotNull(listAfter);
        Assert.Contains(listAfter, u => u.Id == idB);

        var bm = await _client.PostAsync($"/api/bookmarks/{idB}", null);
        Assert.Equal(HttpStatusCode.OK, bm.StatusCode);
        var un = await _client.DeleteAsync($"/api/bookmarks/{idB}");
        Assert.Equal(HttpStatusCode.NoContent, un.StatusCode);
    }
}
