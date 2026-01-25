using Azure.Data.Tables;
using Gambling5.Api.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Gambling5.Api.Functions;

public class GigsFunctions
{
    private readonly ILogger<GigsFunctions> _logger;
    private readonly string _connectionString;

    public GigsFunctions(ILogger<GigsFunctions> logger)
    {
        _logger = logger;
        _connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage") ?? "UseDevelopmentStorage=true";
    }

    private async Task<TableClient> GetTableClientAsync()
    {
        var tableServiceClient = new TableServiceClient(_connectionString);
        var tableClient = tableServiceClient.GetTableClient("gigs");
        await tableClient.CreateIfNotExistsAsync();
        return tableClient;
    }

    [Function("GetGigs")]
    public async Task<IActionResult> GetGigs(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "gigs")] HttpRequest req)
    {
        _logger.LogInformation("Getting all gigs");

        try
        {
            var tableClient = await GetTableClientAsync();
            var gigs = new List<GigDto>();
            
            await foreach (var entity in tableClient.QueryAsync<GigEntity>())
            {
                gigs.Add(GigDto.FromEntity(entity));
            }

            return new OkObjectResult(gigs.OrderBy(g => g.Date).ToList());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching gigs: {Message}", ex.Message);
            return new ObjectResult(new { error = ex.Message }) { StatusCode = 500 };
        }
    }

    [Function("GetGig")]
    public async Task<IActionResult> GetGig(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "gigs/{id}")] HttpRequest req,
        string id)
    {
        _logger.LogInformation("Getting gig {Id}", id);

        try
        {
            var tableClient = await GetTableClientAsync();
            var response = await tableClient.GetEntityIfExistsAsync<GigEntity>("gigs", id);
            
            if (!response.HasValue || response.Value is null)
            {
                return new NotFoundResult();
            }

            return new OkObjectResult(GigDto.FromEntity(response.Value));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching gig {Id}", id);
            return new ObjectResult(new { error = ex.Message }) { StatusCode = 500 };
        }
    }

    [Function("CreateGig")]
    public async Task<IActionResult> CreateGig(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "gigs")] HttpRequest req)
    {
        _logger.LogInformation("Creating new gig");

        try
        {
            var tableClient = await GetTableClientAsync();
            var gig = await req.ReadFromJsonAsync<GigDto>();
            
            if (gig == null)
            {
                return new BadRequestObjectResult("Invalid gig data");
            }

            var entity = gig.ToEntity();
            await tableClient.UpsertEntityAsync(entity);

            return new CreatedResult($"/api/gigs/{entity.RowKey}", GigDto.FromEntity(entity));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating gig: {Message}", ex.Message);
            return new ObjectResult(new { error = ex.Message }) { StatusCode = 500 };
        }
    }

    [Function("DeleteGig")]
    public async Task<IActionResult> DeleteGig(
        [HttpTrigger(AuthorizationLevel.Function, "delete", Route = "gigs/{id}")] HttpRequest req,
        string id)
    {
        _logger.LogInformation("Deleting gig {Id}", id);

        try
        {
            var tableClient = await GetTableClientAsync();
            await tableClient.DeleteEntityAsync("gigs", id);
            return new NoContentResult();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting gig {Id}", id);
            return new ObjectResult(new { error = ex.Message }) { StatusCode = 500 };
        }
    }
}
