using System.Net;
using DartsStats.Api.Controllers;
using DartsStats.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

public sealed class VenueLocationTests
{
    [Fact]
    public async Task LegacyCachedResponseGetsCoordinatesWithoutWikipediaRequest()
    {
        var cache = new FakeCache { Value = new VenueInfo { Name = "OVO Hydro" } };
        var handler = new UnavailableWikipedia();
        var venue = Assert.IsType<VenueInfo>(Assert.IsType<OkObjectResult>(
            (await Create(cache, handler).GetVenueInfo("Night 02")).Result).Value);
        Assert.Equal(55.8603, venue.Latitude);
        Assert.Equal(-4.2849, venue.Longitude);
        Assert.Equal(0, handler.Requests);
        Assert.Equal("venue:Night 02:OVO Hydro", cache.LastKey);
    }

    [Fact]
    public async Task WikipediaFailureStillReturnsAndCachesCorrectGlasgowLocation()
    {
        var cache = new FakeCache();
        var handler = new UnavailableWikipedia();
        var venue = Assert.IsType<VenueInfo>(Assert.IsType<OkObjectResult>(
            (await Create(cache, handler).GetVenueInfo("Night 02")).Result).Value);
        Assert.Equal("OVO Hydro", venue.Name);
        Assert.Equal("Glasgow", venue.City);
        Assert.Equal(55.8603, venue.Latitude);
        Assert.Equal(-4.2849, venue.Longitude);
        Assert.Equal(new DateTime(2025, 2, 13), venue.Weather!.EventDate);
        Assert.Same(venue, cache.Value);
        Assert.Equal(2, handler.Requests);
    }

    [Fact]
    public async Task EveryMappedRoundHasValidCoordinates()
    {
        var rounds = Enumerable.Range(1, 16).Select(n => $"Night {n:00}")
            .Concat(["Semi-Final 1", "Semi-Final 2", "Final"]);
        foreach (var round in rounds)
        {
            var cache = new FakeCache { Value = new VenueInfo() };
            var venue = Assert.IsType<VenueInfo>(Assert.IsType<OkObjectResult>(
                (await Create(cache, new UnavailableWikipedia()).GetVenueInfo(round)).Result).Value);
            Assert.InRange(venue.Latitude!.Value, -90, 90);
            Assert.InRange(venue.Longitude!.Value, -180, 180);
        }
    }

    private static VenuesController Create(FakeCache cache, UnavailableWikipedia handler) => new(
        NullLogger<VenuesController>.Instance, new HttpClient(handler), cache,
        new ConfigurationBuilder().Build());

    private sealed class UnavailableWikipedia : HttpMessageHandler
    {
        public int Requests { get; private set; }
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            Requests++;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));
        }
    }

    private sealed class FakeCache : ICacheService
    {
        public object? Value { get; set; }
        public string? LastKey { get; private set; }
        public Task<T?> GetAsync<T>(string key) where T : class
        {
            LastKey = key;
            return Task.FromResult(Value as T);
        }
        public Task SetAsync<T>(string key, T value, TimeSpan? expiration = null) where T : class
        {
            Value = value;
            return Task.CompletedTask;
        }
        public Task RemoveAsync(string key) => Task.CompletedTask;
        public Task RemoveByPatternAsync(string pattern) => Task.CompletedTask;
    }
}
