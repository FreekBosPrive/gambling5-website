using System.Net.Http.Json;
using Gambling5.Web.Models;

namespace Gambling5.Web.Services;

public class GigService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiBaseUrl;

    public GigService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _apiBaseUrl = configuration["ApiBaseUrl"] ?? "http://localhost:7071/api";
    }

    public async Task<List<Gig>> GetUpcomingGigsAsync()
    {
        try
        {
            var gigs = await _httpClient.GetFromJsonAsync<List<Gig>>($"{_apiBaseUrl}/gigs");
            return gigs?.Where(g => g.Date >= DateTime.Today && g.IsPublic)
                       .OrderBy(g => g.Date)
                       .ToList() ?? new List<Gig>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching gigs: {ex.Message}");
            return new List<Gig>();
        }
    }

    public async Task<List<Gig>> GetAllGigsAsync()
    {
        try
        {
            var gigs = await _httpClient.GetFromJsonAsync<List<Gig>>($"{_apiBaseUrl}/gigs");
            return gigs?.OrderByDescending(g => g.Date).ToList() ?? new List<Gig>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching gigs: {ex.Message}");
            return new List<Gig>();
        }
    }
}
