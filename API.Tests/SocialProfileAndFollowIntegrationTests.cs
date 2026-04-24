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

    [Fact]
    public async Task Search_filters_by_query_and_hobby_ids()
    {
        var viewer = UniqueName();
        var target = UniqueName();
        var other = UniqueName();

        var regViewer = await _client.PostAsJsonAsync("/api/account/register", new RegisterDto
        {
            UserName = viewer,
            Email = $"{viewer}@test.com",
            Password = "Aa123456"
        });
        var regTarget = await _client.PostAsJsonAsync("/api/account/register", new RegisterDto
        {
            UserName = target,
            Email = $"{target}@test.com",
            Password = "Aa123456"
        });
        var regOther = await _client.PostAsJsonAsync("/api/account/register", new RegisterDto
        {
            UserName = other,
            Email = $"{other}@test.com",
            Password = "Aa123456"
        });

        Assert.Equal(HttpStatusCode.OK, regViewer.StatusCode);
        Assert.Equal(HttpStatusCode.OK, regTarget.StatusCode);
        Assert.Equal(HttpStatusCode.OK, regOther.StatusCode);

        var viewerToken = await LoginAndGetTokenAsync(_client, viewer, "Aa123456");
        var targetToken = await LoginAndGetTokenAsync(_client, target, "Aa123456");
        var otherToken = await LoginAndGetTokenAsync(_client, other, "Aa123456");
        Assert.False(string.IsNullOrWhiteSpace(viewerToken));
        Assert.False(string.IsNullOrWhiteSpace(targetToken));
        Assert.False(string.IsNullOrWhiteSpace(otherToken));

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", targetToken);
        var targetUpdate = await _client.PutAsJsonAsync("/api/users", new MemberUpdateDto
        {
            KnownAs = "Pixel Hero",
            Headline = "Unity gameplay engineer",
            HobbyIds = [1, 3]
        });
        Assert.Equal(HttpStatusCode.NoContent, targetUpdate.StatusCode);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", otherToken);
        var otherUpdate = await _client.PutAsJsonAsync("/api/users", new MemberUpdateDto
        {
            KnownAs = "Backend Coder",
            Headline = "API architect",
            HobbyIds = [2]
        });
        Assert.Equal(HttpStatusCode.NoContent, otherUpdate.StatusCode);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", viewerToken);

        var searchByKnownAs = await _client.GetAsync("/api/users/search?q=pixel&page=1&pageSize=20");
        Assert.Equal(HttpStatusCode.OK, searchByKnownAs.StatusCode);
        using (var doc = JsonDocument.Parse(await searchByKnownAs.Content.ReadAsStringAsync()))
        {
            var items = doc.RootElement.GetProperty("items");
            var names = items.EnumerateArray().Select(x => x.GetProperty("userName").GetString()).ToList();
            Assert.Contains(target, names);
            Assert.DoesNotContain(other, names);
        }

        var searchByHeadline = await _client.GetAsync("/api/users/search?q=unity&page=1&pageSize=20");
        Assert.Equal(HttpStatusCode.OK, searchByHeadline.StatusCode);
        using (var doc = JsonDocument.Parse(await searchByHeadline.Content.ReadAsStringAsync()))
        {
            var items = doc.RootElement.GetProperty("items");
            var names = items.EnumerateArray().Select(x => x.GetProperty("userName").GetString()).ToList();
            Assert.Contains(target, names);
            Assert.DoesNotContain(other, names);
        }

        var searchByHobby = await _client.GetAsync("/api/users/search?hobbyIds=1&page=1&pageSize=20");
        Assert.Equal(HttpStatusCode.OK, searchByHobby.StatusCode);
        using (var doc = JsonDocument.Parse(await searchByHobby.Content.ReadAsStringAsync()))
        {
            var items = doc.RootElement.GetProperty("items");
            var names = items.EnumerateArray().Select(x => x.GetProperty("userName").GetString()).ToList();
            Assert.Contains(target, names);
            Assert.DoesNotContain(other, names);
        }
    }

    [Fact]
    public async Task Suggestions_scores_by_shared_hobbies_mutual_connections_and_city()
    {
        var viewer = UniqueName();
        var common = UniqueName();
        var top = UniqueName();
        var low = UniqueName();

        var regViewer = await _client.PostAsJsonAsync("/api/account/register", new RegisterDto
        {
            UserName = viewer,
            Email = $"{viewer}@test.com",
            Password = "Aa123456"
        });
        var regCommon = await _client.PostAsJsonAsync("/api/account/register", new RegisterDto
        {
            UserName = common,
            Email = $"{common}@test.com",
            Password = "Aa123456"
        });
        var regTop = await _client.PostAsJsonAsync("/api/account/register", new RegisterDto
        {
            UserName = top,
            Email = $"{top}@test.com",
            Password = "Aa123456"
        });
        var regLow = await _client.PostAsJsonAsync("/api/account/register", new RegisterDto
        {
            UserName = low,
            Email = $"{low}@test.com",
            Password = "Aa123456"
        });

        Assert.Equal(HttpStatusCode.OK, regViewer.StatusCode);
        Assert.Equal(HttpStatusCode.OK, regCommon.StatusCode);
        Assert.Equal(HttpStatusCode.OK, regTop.StatusCode);
        Assert.Equal(HttpStatusCode.OK, regLow.StatusCode);

        using var docCommon = JsonDocument.Parse(await regCommon.Content.ReadAsStringAsync());
        using var docTop = JsonDocument.Parse(await regTop.Content.ReadAsStringAsync());
        using var docLow = JsonDocument.Parse(await regLow.Content.ReadAsStringAsync());
        var commonId = docCommon.RootElement.GetProperty("user").GetProperty("id").GetInt32();
        var topId = docTop.RootElement.GetProperty("user").GetProperty("id").GetInt32();
        var lowId = docLow.RootElement.GetProperty("user").GetProperty("id").GetInt32();

        var viewerToken = await LoginAndGetTokenAsync(_client, viewer, "Aa123456");
        var commonToken = await LoginAndGetTokenAsync(_client, common, "Aa123456");
        var topToken = await LoginAndGetTokenAsync(_client, top, "Aa123456");
        var lowToken = await LoginAndGetTokenAsync(_client, low, "Aa123456");
        Assert.False(string.IsNullOrWhiteSpace(viewerToken));
        Assert.False(string.IsNullOrWhiteSpace(commonToken));
        Assert.False(string.IsNullOrWhiteSpace(topToken));
        Assert.False(string.IsNullOrWhiteSpace(lowToken));

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", viewerToken);
        var viewerUpdate = await _client.PutAsJsonAsync("/api/users", new MemberUpdateDto
        {
            City = "Victoria",
            HobbyIds = [1, 2]
        });
        Assert.Equal(HttpStatusCode.NoContent, viewerUpdate.StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await _client.PostAsync($"/api/follow/{commonId}", null)).StatusCode);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", topToken);
        var topUpdate = await _client.PutAsJsonAsync("/api/users", new MemberUpdateDto
        {
            City = "Victoria",
            HobbyIds = [1]
        });
        Assert.Equal(HttpStatusCode.NoContent, topUpdate.StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await _client.PostAsync($"/api/follow/{commonId}", null)).StatusCode);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", lowToken);
        var lowUpdate = await _client.PutAsJsonAsync("/api/users", new MemberUpdateDto
        {
            City = "Calgary",
            HobbyIds = [2]
        });
        Assert.Equal(HttpStatusCode.NoContent, lowUpdate.StatusCode);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", viewerToken);
        var suggestions = await _client.GetAsync("/api/users/suggestions?page=1&pageSize=10");
        Assert.Equal(HttpStatusCode.OK, suggestions.StatusCode);

        using var suggestionsDoc = JsonDocument.Parse(await suggestions.Content.ReadAsStringAsync());
        var items = suggestionsDoc.RootElement.GetProperty("items").EnumerateArray().ToList();
        Assert.True(items.Count >= 2);
        var names = items.Select(x => x.GetProperty("userName").GetString()).ToList();
        var topIndex = names.IndexOf(top);
        var lowIndex = names.IndexOf(low);
        Assert.True(topIndex >= 0);
        Assert.True(lowIndex >= 0);
        Assert.True(topIndex < lowIndex, "Expected top candidate to rank ahead of low candidate.");
    }
}
