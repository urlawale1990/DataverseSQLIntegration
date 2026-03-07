using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using System.Net;
using System.Text.Json;

namespace DataverseSQLIntegration;

public class GetContactByEmail
{
    [Function("GetContactByEmail")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequestData req)
    {
        var response = req.CreateResponse();

        try
        {
            // Get email from query string
            var query = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
            string? email = query["email"];

            if (string.IsNullOrEmpty(email))
            {
                response.StatusCode = HttpStatusCode.BadRequest;
                await response.WriteAsJsonAsync(new
                {
                    Status = "Error",
                    Message = "Email parameter is required"
                });
                return response;
            }

            // Step 1: Get token
            var token = await TokenService.GetAccessToken();

            // Step 2: Get contact from Dataverse
            var contact = await DataverseService.GetContactByEmail(email, token);

            if (contact == null)
            {
                response.StatusCode = HttpStatusCode.NotFound;
                await response.WriteAsJsonAsync(new
                {
                    Status = "NotFound",
                    Message = "Contact not found in Dataverse"
                });
                return response;
            }

            // Step 3: Parse contact fields
            using var doc = JsonDocument.Parse(contact);
            var root = doc.RootElement;

            string fullName = root.TryGetProperty("fullname", out var fn)
                               ? fn.GetString() ?? "" : "";
            string emailAddr = root.TryGetProperty("emailaddress1", out var ea)
                               ? ea.GetString() ?? "" : "";
            string? phone = root.TryGetProperty("mobilephone", out var mp)
                               ? mp.GetString() : null;
            string? dvId = root.TryGetProperty("contactid", out var id)
                               ? id.GetString() : null;

            // Step 4: Sync to SQL
            await SqlService.UpsertContact(
                dvId, fullName, emailAddr, phone, "Dataverse->SQL");

            // Step 5: Return contact data
            response.StatusCode = HttpStatusCode.OK;
            response.Headers.Add("Content-Type", "application/json");
            await response.WriteStringAsync(contact);
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