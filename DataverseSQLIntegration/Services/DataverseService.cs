using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace DataverseSQLIntegration;

public static class DataverseService
{
    // Get contact from Dataverse by email
    public static async Task<string?> GetContactByEmail(string email, string token)
    {
        var dataverseUrl = Environment.GetEnvironmentVariable("DataverseUrl");
        var apiUrl =
            $"{dataverseUrl}/api/data/v9.2/contacts" +
            $"?$select=contactid,fullname,emailaddress1,mobilephone" +
            $"&$filter=contains(emailaddress1, '{email}')";

        using var client = new HttpClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync(apiUrl);
        if (!response.IsSuccessStatusCode) return null;

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var contacts = doc.RootElement.GetProperty("value");
        if (contacts.GetArrayLength() == 0) return null;

        return contacts[0].GetRawText();
    }

    // Create contact in Dataverse
    public static async Task<string> CreateContact(
        ContactRequest contact, string token)
    {
        var dataverseUrl = Environment.GetEnvironmentVariable("DataverseUrl");
        var apiUrl = $"{dataverseUrl}/api/data/v9.2/contacts";

        var payload = new Dictionary<string, object?>
        {
            { "firstname",     contact.FirstName },
            { "lastname",      contact.LastName },
            { "emailaddress1", contact.EmailAddress1 },
            { "mobilephone",   contact.MobilePhone }
        };

        if (contact.DateOfBirth.HasValue)
            payload.Add("birthdate",
                contact.DateOfBirth.Value.ToString("yyyy-MM-dd"));

        if (!string.IsNullOrEmpty(contact.ParentAccountId))
            payload.Add("parentcustomerid_account@odata.bind",
                $"/accounts({contact.ParentAccountId})");

        if (contact.GenderCode.HasValue)
            payload.Add("gendercode", contact.GenderCode.Value);

        if (contact.DoNotEmail.HasValue)
            payload.Add("donotemail", contact.DoNotEmail.Value);

        using var client = new HttpClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        var content = new StringContent(
            JsonSerializer.Serialize(payload),
            Encoding.UTF8, "application/json");

        var response = await client.PostAsync(apiUrl, content);
        response.EnsureSuccessStatusCode();

        var entityUri = response.Headers.GetValues("OData-EntityId").First();
        return entityUri.Split('(')[1].TrimEnd(')');
    }

    // Update contact in Dataverse
    public static async Task UpdateContact(
        string contactId, ContactRequest contact, string token)
    {
        var dataverseUrl = Environment.GetEnvironmentVariable("DataverseUrl");
        var apiUrl = $"{dataverseUrl}/api/data/v9.2/contacts({contactId})";

        var payload = new Dictionary<string, object?>
        {
            { "firstname",     contact.FirstName },
            { "lastname",      contact.LastName },
            { "emailaddress1", contact.EmailAddress1 },
            { "mobilephone",   contact.MobilePhone }
        };

        using var client = new HttpClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        var content = new StringContent(
            JsonSerializer.Serialize(payload),
            Encoding.UTF8, "application/json");

        var request = new HttpRequestMessage(
            new HttpMethod("PATCH"), apiUrl)
        { Content = content };

        var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();
    }
}