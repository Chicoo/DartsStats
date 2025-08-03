using Microsoft.AspNetCore.Mvc;
using System.Text.Json.Serialization;

namespace DartsStats.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class VenuesController : ControllerBase
{
    private readonly ILogger<VenuesController> _logger;
    private readonly HttpClient _httpClient;

    // Mapping of Premier League Darts nights to venue information and dates
    private static readonly Dictionary<string, (string City, string Venue, DateTime EventDate)> VenueMapping = new()
    {
        { "Night 1", ("Belfast", "SSE Arena, Belfast", new DateTime(2025, 2, 6)) },
        { "Night 2", ("Glasgow", "P&J Live", new DateTime(2025, 2, 13)) },
        { "Night 3", ("Dublin", "3Arena", new DateTime(2025, 2, 20)) },
        { "Night 4", ("Exeter", "Westpoint Arena", new DateTime(2025, 2, 27)) },
        { "Night 5", ("Brighton", "Brighton Centre", new DateTime(2025, 3, 6)) },
        { "Night 6", ("Nottingham", "Motorpoint Arena Nottingham", new DateTime(2025, 3, 13)) },
        { "Night 7", ("Cardiff", "Motorpoint Arena Cardiff", new DateTime(2025, 3, 20)) },
        { "Night 8", ("Newcastle", "Utilita Arena Newcastle", new DateTime(2025, 3, 27)) },
        { "Night 9", ("Berlin", "Uber Arena", new DateTime(2025, 4, 3)) },
        { "Night 10", ("Manchester", "AO Arena", new DateTime(2025, 4, 10)) },
        { "Night 11", ("Rotterdam", "Rotterdam Ahoy", new DateTime(2025, 4, 17)) },
        { "Night 12", ("Liverpool", "M&S Bank Arena", new DateTime(2025, 4, 24)) },
        { "Night 13", ("Birmingham", "Resorts World Arena", new DateTime(2025, 5, 1)) },
        { "Night 14", ("Leeds", "First Direct Arena", new DateTime(2025, 5, 8)) },
        { "Night 15", ("Aberdeen", "P&J Live", new DateTime(2025, 5, 15)) },
        { "Night 16", ("Sheffield", "Utilita Arena Sheffield", new DateTime(2025, 5, 22)) },
        { "Semi-Final 1", ("London", "The O2 Arena", new DateTime(2025, 5, 29)) },
        { "Semi-Final 2", ("London", "The O2 Arena", new DateTime(2025, 5, 29)) },
        { "Final", ("London", "The O2 Arena", new DateTime(2025, 5, 29)) }
    };

    public VenuesController(ILogger<VenuesController> logger, HttpClient httpClient)
    {
        _logger = logger;
        _httpClient = httpClient;
    }

    [HttpGet("{round}")]
    public async Task<ActionResult<VenueInfo>> GetVenueInfo(string round)
    {
        try
        {
            if (!VenueMapping.TryGetValue(round, out var venueData))
            {
                return NotFound($"No venue information found for round: {round}");
            }

            var (city, venue, eventDate) = venueData;
            
            // Try to fetch venue information from Wikipedia API
            var venueInfo = await FetchVenueInfoFromWikipedia(venue, city, eventDate);
            
            return Ok(venueInfo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching venue information for round {Round}", round);
            return StatusCode(500, "An error occurred while fetching venue information");
        }
    }

    private async Task<VenueInfo> FetchVenueInfoFromWikipedia(string venueName, string city, DateTime eventDate)
    {
        try
        {
            // Use Wikipedia REST API to get venue information
            var wikipediaUrl = $"https://en.wikipedia.org/api/rest_v1/page/summary/{Uri.EscapeDataString(venueName)}";
            
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "DartsStats/1.0 (Educational Project)");
            _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");

            var response = await _httpClient.GetAsync(wikipediaUrl);

            if (response.IsSuccessStatusCode)
            {
                var jsonContent = await response.Content.ReadAsStringAsync();
                var data = System.Text.Json.JsonSerializer.Deserialize<WikipediaSummaryResponse>(jsonContent);
                
                // Try to get a higher quality image if the thumbnail is too small
                var imageUrl = data?.Thumbnail?.Source;
                if (!string.IsNullOrEmpty(imageUrl))
                {
                    // Try to get a larger version of the image by replacing size parameters
                    imageUrl = imageUrl.Replace("/220px-", "/400px-").Replace("/300px-", "/400px-");
                }

                // Fetch weather information for the event date and location
                var weatherInfo = await FetchWeatherInfo(city, eventDate);
                
                return new VenueInfo
                {
                    Name = data?.Title ?? venueName,
                    City = city,
                    Capacity = ExtractCapacityFromText(data?.Extract) ?? ExtractCapacityFromText(data?.Description) ?? "Information not available",
                    Description = data?.Extract ?? $"{venueName} is a major entertainment venue located in {city}.",
                    Image = imageUrl,
                    Website = data?.ContentUrls?.Desktop?.Page,
                    Address = data?.Description,
                    Opened = ExtractOpeningYearFromDescription(data?.Extract),
                    Weather = weatherInfo
                };
            }
            else
            {
                // Fallback: try alternative Wikipedia API
                return await FetchVenueInfoFallback(venueName, city, eventDate);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch venue info from Wikipedia for {VenueName}", venueName);
            return await CreateFallbackVenueInfo(venueName, city, eventDate);
        }
    }

    private async Task<WeatherInfo?> FetchWeatherInfo(string city, DateTime eventDate)
    {
        // For demo purposes, I'll create sample weather data based on the city and date
        // In a real application, you would integrate with a weather API like OpenWeatherMap
        
        // Simulate different weather based on city and season
        var weather = GenerateSampleWeatherData(city, eventDate);
        
        return await Task.FromResult(weather);
    }

    private WeatherInfo GenerateSampleWeatherData(string city, DateTime eventDate)
    {
        // Generate realistic weather data based on location and season
        var random = new Random(city.GetHashCode() + eventDate.DayOfYear);
        
        // Base temperatures by season and location
        var baseTemp = eventDate.Month switch
        {
            1 or 2 or 12 => city.Contains("Berlin") || city.Contains("Aberdeen") ? random.Next(-2, 5) : random.Next(2, 8),
            3 or 4 or 11 => city.Contains("Berlin") || city.Contains("Aberdeen") ? random.Next(3, 12) : random.Next(8, 15),
            5 or 6 or 9 or 10 => city.Contains("Berlin") || city.Contains("Aberdeen") ? random.Next(10, 18) : random.Next(12, 20),
            _ => city.Contains("Berlin") || city.Contains("Aberdeen") ? random.Next(15, 25) : random.Next(18, 25)
        };

        var weatherConditions = new[]
        {
            ("Clear sky", "01d", 20, 4),
            ("Few clouds", "02d", 40, 6),
            ("Scattered clouds", "03d", 60, 8),
            ("Overcast", "04d", 75, 5),
            ("Light rain", "10d", 85, 3),
            ("Rain", "09d", 90, 2)
        };

        var condition = weatherConditions[random.Next(weatherConditions.Length)];
        
        return new WeatherInfo
        {
            Description = condition.Item1,
            Temperature = baseTemp + random.Next(-3, 4),
            Humidity = condition.Item3 + random.Next(-10, 11),
            WindSpeed = condition.Item4 + random.Next(-2, 3),
            Icon = condition.Item2,
            EventDate = eventDate
        };
    }

    private async Task<VenueInfo> FetchVenueInfoFallback(string venueName, string city, DateTime eventDate)
    {
        try
        {
            // Request larger thumbnail size for better image quality
            var fallbackUrl = $"https://en.wikipedia.org/w/api.php?action=query&format=json&prop=extracts|pageimages&exintro=1&explaintext=1&piprop=thumbnail&pithumbsize=400&titles={Uri.EscapeDataString(venueName)}";
            
            var response = await _httpClient.GetAsync(fallbackUrl);
            
            if (response.IsSuccessStatusCode)
            {
                var jsonContent = await response.Content.ReadAsStringAsync();
                var data = System.Text.Json.JsonSerializer.Deserialize<WikipediaQueryResponse>(jsonContent);
                
                return await ParseWikipediaQueryResponse(data, venueName, city, eventDate);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Fallback venue info fetch failed for {VenueName}", venueName);
        }

        return await CreateFallbackVenueInfo(venueName, city, eventDate);
    }

    private async Task<VenueInfo> ParseWikipediaQueryResponse(WikipediaQueryResponse? data, string venueName, string city, DateTime eventDate)
    {
        var pages = data?.Query?.Pages;
        if (pages == null || !pages.Any())
        {
            return await CreateFallbackVenueInfo(venueName, city, eventDate);
        }

        var page = pages.Values.FirstOrDefault();
        if (page == null || page.Missing == true)
        {
            return await CreateFallbackVenueInfo(venueName, city, eventDate);
        }

        var extract = page.Extract ?? "";
        var capacity = ExtractCapacityFromText(extract) ?? "Unknown";
        var opened = ExtractOpeningYearFromText(extract);
        var weatherInfo = await FetchWeatherInfo(city, eventDate);

        return new VenueInfo
        {
            Name = page.Title ?? venueName,
            City = city,
            Capacity = capacity,
            Description = extract.Length > 500 ? extract.Substring(0, 500) + "..." : extract,
            Image = page.Thumbnail?.Source,
            Opened = opened,
            Weather = weatherInfo
        };
    }

    private async Task<VenueInfo> CreateFallbackVenueInfo(string venueName, string city, DateTime eventDate)
    {
        var weatherInfo = await FetchWeatherInfo(city, eventDate);
        
        return new VenueInfo
        {
            Name = venueName,
            City = city,
            Capacity = "Information not available",
            Description = $"{venueName} is a major entertainment venue located in {city}. This venue regularly hosts Premier League Darts and other major sporting events.",
            Weather = weatherInfo
        };
    }

    private string? ExtractCapacityFromText(string? text)
    {
        if (string.IsNullOrEmpty(text))
            return null;

        // Enhanced patterns for capacity in Wikipedia text
        var patterns = new[]
        {
            @"(?:with\s+)?(?:a\s+)?capacity\s+of\s+([0-9,]+)",
            @"capacity[:\s]+(?:of\s+)?([0-9,]+)",
            @"seats?\s+([0-9,]+)",
            @"([0-9,]+)\s+(?:capacity|seats?)",
            @"([0-9,]+)\s+seating\s+capacity",
            @"seating\s+capacity\s+of\s+([0-9,]+)",
            @"can\s+(?:accommodate|hold)\s+(?:up\s+to\s+)?([0-9,]+)",
            @"holds?\s+(?:up\s+to\s+)?([0-9,]+)"
        };

        foreach (var pattern in patterns)
        {
            var match = System.Text.RegularExpressions.Regex.Match(text, pattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            if (match.Success)
            {
                var capacityStr = match.Groups[1].Value;
                // Format the capacity number nicely
                if (int.TryParse(capacityStr.Replace(",", ""), out int capacity))
                {
                    return capacity.ToString("N0");
                }
                return capacityStr;
            }
        }

        return null;
    }

    private string? ExtractOpeningYearFromDescription(string? description)
    {
        return ExtractOpeningYearFromText(description);
    }

    private string? ExtractOpeningYearFromText(string? text)
    {
        if (string.IsNullOrEmpty(text))
            return null;

        var patterns = new[]
        {
            @"opened?\s+(?:in\s+)?(\d{4})",
            @"built\s+(?:in\s+)?(\d{4})"
        };

        foreach (var pattern in patterns)
        {
            var match = System.Text.RegularExpressions.Regex.Match(text, pattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            if (match.Success)
            {
                return match.Groups[1].Value;
            }
        }

        return null;
    }
}

// DTOs for API responses
public class VenueInfo
{
    public string Name { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Capacity { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? Image { get; set; }
    public string? Website { get; set; }
    public string? Address { get; set; }
    public string? Opened { get; set; }
    public WeatherInfo? Weather { get; set; }
}

public class WeatherInfo
{
    public string? Description { get; set; }
    public double? Temperature { get; set; }
    public double? Humidity { get; set; }
    public double? WindSpeed { get; set; }
    public string? Icon { get; set; }
    public DateTime EventDate { get; set; }
}

// Wikipedia API response models
public class WikipediaSummaryResponse
{
    [JsonPropertyName("title")]
    public string? Title { get; set; }
    
    [JsonPropertyName("description")]
    public string? Description { get; set; }
    
    [JsonPropertyName("extract")]
    public string? Extract { get; set; }
    
    [JsonPropertyName("thumbnail")]
    public WikipediaThumbnail? Thumbnail { get; set; }
    
    [JsonPropertyName("content_urls")]
    public WikipediaContentUrls? ContentUrls { get; set; }
}

public class WikipediaThumbnail
{
    [JsonPropertyName("source")]
    public string? Source { get; set; }
}

public class WikipediaContentUrls
{
    [JsonPropertyName("desktop")]
    public WikipediaDesktop? Desktop { get; set; }
}

public class WikipediaDesktop
{
    [JsonPropertyName("page")]
    public string? Page { get; set; }
}

public class WikipediaQueryResponse
{
    [JsonPropertyName("query")]
    public WikipediaQuery? Query { get; set; }
}

public class WikipediaQuery
{
    [JsonPropertyName("pages")]
    public Dictionary<string, WikipediaPage>? Pages { get; set; }
}

public class WikipediaPage
{
    [JsonPropertyName("title")]
    public string? Title { get; set; }
    
    [JsonPropertyName("extract")]
    public string? Extract { get; set; }
    
    [JsonPropertyName("missing")]
    public bool? Missing { get; set; }
    
    [JsonPropertyName("thumbnail")]
    public WikipediaThumbnail? Thumbnail { get; set; }
}
