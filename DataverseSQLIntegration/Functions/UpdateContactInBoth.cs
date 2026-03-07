using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using System.Net;
using System.Text.Json;

namespace DataverseSQLIntegration;

public class UpdateContactInBoth
{
    [Function("UpdateContactInBoth")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req)
    {
        var response = req.CreateResponse();

        try
        {
            // Step 1: Read request body
            var body = await new StreamReader(req.Body).ReadToEndAsync();
            var contact = JsonSerializer.Deserialize<ContactRequest>(body,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (contact == null)
            {
                response.StatusCode = HttpStatusCode.BadRequest;
                await response.WriteAsJsonAsync(new
                {
                    Status = "Error",
                    Message = "Invalid request body"
                });
                return response;
            }

            // Step 2: Get token
            var token = await TokenService.GetAccessToken();

            // Step 3: Check if contact exists in Dataverse
            var existing = await DataverseService.GetContactByEmail(
                contact.EmailAddress1, token);

            if (existing == null)
            {
                response.StatusCode = HttpStatusCode.NotFound;
                await response.WriteAsJsonAsync(new
                {
                    Status = "NotFound",
                    Message = "Contact not found in Dataverse"
                });
                return response;
            }

            // Step 4: Get Dataverse contact ID
            using var doc = JsonDocument.Parse(existing);
            var root = doc.RootElement;
            string? dataverseId = root.TryGetProperty("contactid", out var id)
                                  ? id.GetString() : null;

            if (string.IsNullOrEmpty(dataverseId))
            {
                response.StatusCode = HttpStatusCode.InternalServerError;
                await response.WriteAsJsonAsync(new
                {
                    Status = "Error",
                    Message = "Could not retrieve Dataverse contact ID"
                });
                return response;
            }

            // Step 5: Update in Dataverse
            await DataverseService.UpdateContact(dataverseId, contact, token);

            // Step 6: Update in SQL
            string fullName = $"{contact.FirstName} {contact.LastName}".Trim();
            await SqlService.UpsertContact(
                dataverseId, fullName,
                contact.EmailAddress1, contact.MobilePhone,
                "Update->Both");

            response.StatusCode = HttpStatusCode.OK;
            await response.WriteAsJsonAsync(new
            {
                Status = "Success",
                Message = "Contact updated in both Dataverse and SQL",
                DataverseId = dataverseId,
                FullName = fullName,
                Email = contact.EmailAddress1
            });
        }
        catch (Exception ex)
        {
            response.StatusCode = HttpStatusCode.InternalServerError;
            await response.WriteAsJsonAsync(new
            {
                Status = "Error",
                Message = ex.Message
            });
        }

        return response;
    }
}