namespace Gambling5.Api.Models;

public class ContactRequest
{
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public string? Phone { get; set; }
    public string? EventType { get; set; }
    public string? EventDate { get; set; }
    public string Message { get; set; } = "";
}
