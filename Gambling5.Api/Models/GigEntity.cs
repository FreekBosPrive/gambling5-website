using Azure;
using Azure.Data.Tables;

namespace Gambling5.Api.Models;

public class GigEntity : ITableEntity
{
    public string PartitionKey { get; set; } = "gigs";
    public string RowKey { get; set; } = string.Empty;
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }
    
    public DateTime Date { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Venue { get; set; } = string.Empty;
    public string Status { get; set; } = "confirmed";
    public string? Description { get; set; }
    public bool IsPublic { get; set; } = true;
}

public class GigDto
{
    public string Id { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Venue { get; set; } = string.Empty;
    public string Status { get; set; } = "confirmed";
    public string? Description { get; set; }
    public bool IsPublic { get; set; } = true;

    public static GigDto FromEntity(GigEntity entity) => new()
    {
        Id = entity.RowKey,
        Date = entity.Date,
        Title = entity.Title,
        Venue = entity.Venue,
        Status = entity.Status,
        Description = entity.Description,
        IsPublic = entity.IsPublic
    };
}
