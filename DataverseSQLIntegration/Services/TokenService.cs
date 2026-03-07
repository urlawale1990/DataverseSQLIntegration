using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;

namespace DataverseSQLIntegration;

public static class TokenService
{
    public static async Task<string> GetAccessToken()
    {
        var tenantId = Environment.GetEnvironmentVariable("TenantId");
        var clientId = Environment.GetEnvironmentVariable("ClientId");
        var clientSecret = Environment.GetEnvironmentVariable("ClientSecret");
        var dataverseUrl = Environment.GetEnvironmentVariable("DataverseUrl");

        var tokenUrl =
            $"https://login.microsoftonline.com/{tenantId}/oauth2/v2.0/token";

        var body = new Dictionary<string, string>
        {
            { "client_id", clientId! },
            { "client_secret", clientSecret! },
            { "grant_type", "client_credentials" },
            { "scope", $"{dataverseUrl}/.default" }
        };

        using var client = new HttpClient();
        var response = await client.PostAsync(
            tokenUrl, new FormUrlEncodedContent(body));
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.GetProperty("access_token").GetString()!;
    }
}
