using Azure.Core;
using Azure.Data.Tables;
using Azure.Identity;
using Gambling5.Api.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Gambling5.Api.Functions;

public class ContactFunctions
{
    private readonly ILogger<ContactFunctions> _logger;
    private const int DAILY_EMAIL_LIMIT = 25;
    private static readonly HttpClient HttpClient = new();
    private static readonly TokenCredential GraphCredential = new DefaultAzureCredential();
    private static readonly string[] GraphScopes = ["https://graph.microsoft.com/.default"];

    public ContactFunctions(ILogger<ContactFunctions> logger)
    {
        _logger = logger;
    }

    [Function("SendContactEmail")]
    public async Task<HttpResponseData> SendContactEmail(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "contact")] HttpRequestData req)
    {
        _logger.LogInformation("Processing contact form submission");

        try
        {
            var connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage") ?? "";
            var tableClient = new TableClient(connectionString, "emailcounter");
            await tableClient.CreateIfNotExistsAsync();

            var today = DateTime.UtcNow.ToString("yyyy-MM-dd");
            EmailCounterEntity? counter = null;

            try
            {
                var counterResponse = await tableClient.GetEntityAsync<EmailCounterEntity>("counter", today);
                counter = counterResponse.Value;
            }
            catch (Azure.RequestFailedException ex) when (ex.Status == 404)
            {
                // Counter doesn't exist for today, will create a new one after sending.
            }

            if (counter != null && counter.Count >= DAILY_EMAIL_LIMIT)
            {
                _logger.LogWarning("Daily email limit reached: {Count}/{Limit}", counter.Count, DAILY_EMAIL_LIMIT);
                var limitResponse = req.CreateResponse(HttpStatusCode.TooManyRequests);
                await limitResponse.WriteStringAsync("Daily email limit reached. Please try again tomorrow or contact us directly.");
                return limitResponse;
            }

            var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var contact = JsonSerializer.Deserialize<ContactRequest>(requestBody, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (contact == null || string.IsNullOrEmpty(contact.Name) ||
                string.IsNullOrEmpty(contact.Email) || string.IsNullOrEmpty(contact.Message))
            {
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteStringAsync("Name, Email, and Message are required");
                return badResponse;
            }

            var senderEmail = Environment.GetEnvironmentVariable("GRAPH_SENDER_EMAIL") ?? "info@gambling5.de";
            var toEmail = Environment.GetEnvironmentVariable("CONTACT_EMAIL") ?? "info@gambling5.de";

            if (string.IsNullOrEmpty(senderEmail) || string.IsNullOrEmpty(toEmail))
            {
                _logger.LogError("Graph mail sender or recipient is not configured");
                var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
                await errorResponse.WriteStringAsync("Email service not configured");
                return errorResponse;
            }

            var eventTypeDisplay = contact.EventType switch
            {
                "firmenfeier" => "Firmenfeier",
                "privatfest" => "Privatfest",
                "kneipe" => "Kneipenabend",
                "festival" => "Festival/Oeffentlich",
                "sonstiges" => "Sonstiges",
                _ => "Nicht angegeben"
            };

            var emailBody = $@"
Neue Kontaktanfrage von der Website:

Name: {contact.Name}
E-Mail: {contact.Email}
Telefon: {contact.Phone ?? "Nicht angegeben"}
Art der Veranstaltung: {eventTypeDisplay}
Datum: {contact.EventDate ?? "Nicht angegeben"}

Nachricht:
{contact.Message}

---
Diese E-Mail wurde automatisch ueber das Kontaktformular auf www.gambling5.de gesendet.
";

            var graphToken = await GraphCredential.GetTokenAsync(
                new TokenRequestContext(GraphScopes),
                req.FunctionContext.CancellationToken);

            var graphPayload = new
            {
                message = new
                {
                    subject = $"Kontaktanfrage von {contact.Name}",
                    body = new
                    {
                        contentType = "Text",
                        content = emailBody
                    },
                    toRecipients = new[]
                    {
                        new
                        {
                            emailAddress = new
                            {
                                address = toEmail
                            }
                        }
                    },
                    replyTo = new[]
                    {
                        new
                        {
                            emailAddress = new
                            {
                                address = contact.Email,
                                name = contact.Name
                            }
                        }
                    }
                },
                saveToSentItems = false
            };

            using var graphRequest = new HttpRequestMessage(
                HttpMethod.Post,
                $"https://graph.microsoft.com/v1.0/users/{Uri.EscapeDataString(senderEmail)}/sendMail");
            graphRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", graphToken.Token);
            graphRequest.Content = new StringContent(
                JsonSerializer.Serialize(graphPayload),
                Encoding.UTF8,
                "application/json");

            using var graphResponse = await HttpClient.SendAsync(
                graphRequest,
                req.FunctionContext.CancellationToken);

            if (!graphResponse.IsSuccessStatusCode)
            {
                var graphError = await graphResponse.Content.ReadAsStringAsync(req.FunctionContext.CancellationToken);
                _logger.LogError(
                    "Graph sendMail failed with status {StatusCode}: {GraphError}",
                    graphResponse.StatusCode,
                    graphError);

                var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
                await errorResponse.WriteStringAsync("Error sending email through Microsoft Graph");
                return errorResponse;
            }

            if (counter == null)
            {
                counter = new EmailCounterEntity
                {
                    PartitionKey = "counter",
                    RowKey = today,
                    Count = 1
                };
                await tableClient.UpsertEntityAsync(counter);
            }
            else
            {
                counter.Count++;
                await tableClient.UpdateEntityAsync(counter, counter.ETag);
            }

            _logger.LogInformation("Contact email sent successfully from {Email}. Daily count: {Count}/{Limit}",
                contact.Email, counter.Count, DAILY_EMAIL_LIMIT);

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(new { success = true, message = "Email sent successfully" });
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending contact email");
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteStringAsync($"Error sending email: {ex.Message}");
            return errorResponse;
        }
    }
}

public class EmailCounterEntity : ITableEntity
{
    public string PartitionKey { get; set; } = "counter";
    public string RowKey { get; set; } = "";
    public DateTimeOffset? Timestamp { get; set; }
    public Azure.ETag ETag { get; set; }
    public int Count { get; set; }
}
