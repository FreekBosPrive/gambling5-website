using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Mail;
using System.Text.Json;
using Azure.Data.Tables;
using Gambling5.Api.Models;

namespace Gambling5.Api.Functions;

public class ContactFunctions
{
    private readonly ILogger<ContactFunctions> _logger;
    private const int DAILY_EMAIL_LIMIT = 25;

    public ContactFunctions(ILogger<ContactFunctions> logger)
    {
        _logger = logger;
    }

    [Function("SendContactEmail")]
    public async Task<HttpResponseData> SendContactEmail(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "contact")] HttpRequestData req)
    {
        _logger.LogInformation("Processing contact form submission");

        try
        {
            // Check daily email limit
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
                // Counter doesn't exist for today, will create new one
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

            // Get SMTP settings from environment variables (GoDaddy)
            var smtpHost = Environment.GetEnvironmentVariable("SMTP_HOST") ?? "smtpout.secureserver.net";
            var smtpPort = int.Parse(Environment.GetEnvironmentVariable("SMTP_PORT") ?? "587");
            var smtpUser = Environment.GetEnvironmentVariable("SMTP_USER") ?? "";
            var smtpPassword = Environment.GetEnvironmentVariable("SMTP_PASSWORD") ?? "";
            var toEmail = Environment.GetEnvironmentVariable("CONTACT_EMAIL") ?? "info@gambling5.de";

            if (string.IsNullOrEmpty(smtpUser) || string.IsNullOrEmpty(smtpPassword))
            {
                _logger.LogError("SMTP credentials not configured");
                var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
                await errorResponse.WriteStringAsync("Email service not configured");
                return errorResponse;
            }

            // Build email content
            var eventTypeDisplay = contact.EventType switch
            {
                "firmenfeier" => "Firmenfeier",
                "privatfest" => "Privatfest",
                "hochzeit" => "Hochzeit",
                "festival" => "Festival/Öffentlich",
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
Diese E-Mail wurde automatisch über das Kontaktformular auf www.gambling5.de gesendet.
";

            using var smtpClient = new SmtpClient(smtpHost, smtpPort)
            {
                Credentials = new NetworkCredential(smtpUser, smtpPassword),
                EnableSsl = true
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress(smtpUser, "G♠mblinG5 Website"),
                Subject = $"Kontaktanfrage von {contact.Name}",
                Body = emailBody,
                IsBodyHtml = false
            };
            mailMessage.To.Add(toEmail);
            mailMessage.ReplyToList.Add(new MailAddress(contact.Email, contact.Name));

            await smtpClient.SendMailAsync(mailMessage);

            // Increment daily counter
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
