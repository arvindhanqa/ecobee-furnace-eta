using EcobeeFurnaceEta.Blazor.Models;
using Microsoft.JSInterop;
using System.Text.Json;

namespace EcobeeFurnaceEta.Blazor.Services;

/// <summary>
/// Caches runtime stats locally to prevent data loss between refreshes.
/// </summary>
public class RuntimeStatsCache
{
    private readonly IJSRuntime _jsRuntime;
    private const string CacheKey = "ecobee_runtime_stats_cache";
    private RuntimeStats? _memoryCache;

    public RuntimeStatsCache(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    /// <summary>
    /// Gets cached runtime stats, or null if not available.
    /// </summary>
    public async Task<RuntimeStats?> GetCachedStatsAsync()
    {
        if (_memoryCache != null)
        {
            return _memoryCache;
        }

        try
        {
            var json = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", CacheKey);
            if (!string.IsNullOrEmpty(json))
            {
                _memoryCache = JsonSerializer.Deserialize<RuntimeStats>(json);
                return _memoryCache;
            }
        }
        catch
        {
            // Ignore errors reading from cache
        }

        return null;
    }

    /// <summary>
    /// Saves runtime stats to cache.
    /// </summary>
    public async Task SaveStatsAsync(RuntimeStats stats)
    {
        _memoryCache = stats;

        try
        {
            var json = JsonSerializer.Serialize(stats);
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", CacheKey, json);
        }
        catch
        {
            // Ignore errors writing to cache
        }
    }

    /// <summary>
    /// Merges new stats with cached stats, preserving 24h data if new data is empty.
    /// </summary>
    public async Task<RuntimeStats> MergeWithCacheAsync(RuntimeStats newStats)
    {
        var cached = await GetCachedStatsAsync();

        if (cached == null)
        {
            await SaveStatsAsync(newStats);
            return newStats;
        }

        // If new data has valid 24h stats, use them
        // Otherwise preserve cached values
        if (newStats.TotalHeatingMinutes24h == 0 && cached.TotalHeatingMinutes24h > 0)
        {
            newStats.TotalHeatingMinutes24h = cached.TotalHeatingMinutes24h;
        }

        if (newStats.TotalCoolingMinutes24h == 0 && cached.TotalCoolingMinutes24h > 0)
        {
            newStats.TotalCoolingMinutes24h = cached.TotalCoolingMinutes24h;
        }

        if (newStats.HeatingCycles24h == 0 && cached.HeatingCycles24h > 0)
        {
            newStats.HeatingCycles24h = cached.HeatingCycles24h;
        }

        if (newStats.AvgOutdoorTemp24h == 0 && cached.AvgOutdoorTemp24h != 0)
        {
            newStats.AvgOutdoorTemp24h = cached.AvgOutdoorTemp24h;
        }

        if (newStats.ProjectedHeatingMinutesTomorrow == 0 && cached.ProjectedHeatingMinutesTomorrow > 0)
        {
            newStats.ProjectedHeatingMinutesTomorrow = cached.ProjectedHeatingMinutesTomorrow;
        }

        if (newStats.ForecastedAvgTempTomorrow == 0 && cached.ForecastedAvgTempTomorrow != 0)
        {
            newStats.ForecastedAvgTempTomorrow = cached.ForecastedAvgTempTomorrow;
        }

        if (newStats.AvgHeatRetentionMinutes == 0 && cached.AvgHeatRetentionMinutes > 0)
        {
            newStats.AvgHeatRetentionMinutes = cached.AvgHeatRetentionMinutes;
        }

        if (newStats.HeatLossRatePerHour == 0 && cached.HeatLossRatePerHour > 0)
        {
            newStats.HeatLossRatePerHour = cached.HeatLossRatePerHour;
        }

        // Save the merged result
        await SaveStatsAsync(newStats);

        return newStats;
    }

    /// <summary>
    /// Clears all cached data.
    /// </summary>
    public async Task ClearCacheAsync()
    {
        _memoryCache = null;

        try
        {
            await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", CacheKey);
        }
        catch
        {
            // Ignore errors
        }
    }
}
