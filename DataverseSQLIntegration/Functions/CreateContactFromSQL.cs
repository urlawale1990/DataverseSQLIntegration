using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using System.Net;
using System.Text.Json;

namespace DataverseSQLIntegration;

public class CreateContactFromSQL
{
    [Function("CreateContactFromSQL")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req)
    {
        var response = req.CreateResponse();

        try
        {
            // Step 1: Read request body
            var body = await new StreamReader(req.Body).ReadToEndAsync();
            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;

            string? email = root.TryGetProperty("email", out var e)
                            ? e.GetString() : null;

            if (string.IsNullOrEmpty(email))
            {
                response.StatusCode = HttpStatusCode.BadRequest;
                await response.WriteAsJsonAsync(new
                {
                    Status = "Error",
                    Message = "Email is required in request body"
                });
                return response;
            }

            // Step 2: Get contact from SQL
            var sqlContact = await SqlService.GetContactByEmail(email);

            if (sqlContact == null)
            {
                response.StatusCode = HttpStatusCode.NotFound;
                await response.WriteAsJsonAsync(new
                {
                    Status = "NotFound",
                    Message = "Contact not found in SQL database"
                });
                return response;
            }

            // Step 3: Get token
            var token = await TokenService.GetAccessToken();

            // Step 4: Split full name into first/last
            var parts = sqlContact.FullName.Split(' ', 2);
            var contact = new ContactRequest
            {
                FirstName = parts[0],
                LastName = parts.Length > 1 ? parts[1] : "",
                EmailAddress1 = sqlContact.Email,
                MobilePhone = sqlContact.MobilePhone
            };

            // Step 5: Create contact in Dataverse
            var dataverseId = await DataverseService.CreateContact(contact, token);

            // Step 6: Update SQL with Dataverse ID
            await SqlService.UpsertContact(
                dataverseId, sqlContact.FullName,
                sqlContact.Email, sqlContact.MobilePhone,
                "SQL->Dataverse");

            response.StatusCode = HttpStatusCode.Created;
            await response.WriteAsJsonAsync(new
            {
                Status = "Success",
                Message = "Contact synced from SQL to Dataverse",
                DataverseId = dataverseId,
                Email = sqlContact.Email,
                FullName = sqlContact.FullName
            });
        }
        catch (HttpRequestException ex)
        {
            response.StatusCode = HttpStatusCode.BadRequest;
            await response.WriteAsJsonAsync(new
            {
                Status = "DataverseError",
                Message = ex.Message
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