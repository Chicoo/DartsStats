namespace DartsStats.Api.Models;

public class CacheOptions
{
    public const string SectionName = "Cache";
    
    public int VenueExpirationHours { get; set; } = 24;
    public int DefaultExpirationHours { get; set; } = 1;
}