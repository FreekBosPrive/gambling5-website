namespace Gambling5.Web.Models;

public class Gig
{
    public string Id { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Venue { get; set; } = string.Empty;
    public string Status { get; set; } = "confirmed"; // confirmed, public, tentative, cancelled
    public string? Description { get; set; }
    public bool IsPublic { get; set; } = true;
}
