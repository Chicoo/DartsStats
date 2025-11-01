using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace DartsStats.Api.Services;

public class RedisCacheService : ICacheService
{
    private readonly IDistributedCache _distributedCache;
    private readonly ILogger<RedisCacheService> _logger;

    public RedisCacheService(IDistributedCache distributedCache, ILogger<RedisCacheService> logger)
    {
        _distributedCache = distributedCache;
        _logger = logger;
    }

    public async Task<T?> GetAsync<T>(string key) where T : class
    {
        try
        {
            var value = await _distributedCache.GetStringAsync(key);
            if (value == null)
                return null;

            return JsonSerializer.Deserialize<T>(value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting value from cache with key: {Key}", key);
            return null;
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null) where T : class
    {
        try
        {
            var options = new DistributedCacheEntryOptions();
            if (expiration.HasValue)
                options.SetAbsoluteExpiration(expiration.Value);
            else
                options.SetAbsoluteExpiration(TimeSpan.FromHours(1)); // Default 1 hour expiration

            var serializedValue = JsonSerializer.Serialize(value);
            await _distributedCache.SetStringAsync(key, serializedValue, options);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting value in cache with key: {Key}", key);
        }
    }

    public async Task RemoveAsync(string key)
    {
        try
        {
            await _distributedCache.RemoveAsync(key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing value from cache with key: {Key}", key);
        }
    }

    public async Task RemoveByPatternAsync(string pattern)
    {
        // Note: Redis pattern removal requires additional implementation
        // For now, we'll log that this would need Redis-specific commands
        _logger.LogWarning("Pattern-based cache removal not implemented in basic distributed cache. Pattern: {Pattern}", pattern);
        await Task.CompletedTask;
    }
}