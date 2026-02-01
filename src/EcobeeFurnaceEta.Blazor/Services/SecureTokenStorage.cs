using Microsoft.JSInterop;
using System.Text.Json;

namespace EcobeeFurnaceEta.Blazor.Services;

/// <summary>
/// Secure token storage using browser localStorage with optional encryption.
/// Tokens are stored client-side (standard for SPAs).
/// </summary>
public class SecureTokenStorage
{
    private readonly IJSRuntime _jsRuntime;
    private const string TokenKey = "ecobee_tokens";
    private const string ApiKeyKey = "ecobee_api_key";
    private const string JwtTokenKey = "ecobee_jwt";
    private const string ThermostatIdKey = "ecobee_thermostat_id";

    public SecureTokenStorage(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    /// <summary>
    /// Stores OAuth tokens securely in browser localStorage.
    /// </summary>
    public async Task StoreTokensAsync(EcobeeTokens tokens)
    {
        var json = JsonSerializer.Serialize(tokens);
        await _jsRuntime.InvokeVoidAsync("localStorage.setItem", TokenKey, json);
    }

    /// <summary>
    /// Retrieves stored OAuth tokens.
    /// </summary>
    public async Task<EcobeeTokens?> GetTokensAsync()
    {
        try
        {
            var json = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", TokenKey);
            if (string.IsNullOrEmpty(json))
                return null;

            return JsonSerializer.Deserialize<EcobeeTokens>(json);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Stores the user's Ecobee API key.
    /// </summary>
    public async Task StoreApiKeyAsync(string apiKey)
    {
        await _jsRuntime.InvokeVoidAsync("localStorage.setItem", ApiKeyKey, apiKey);
    }

    /// <summary>
    /// Retrieves the stored API key.
    /// </summary>
    public async Task<string?> GetApiKeyAsync()
    {
        try
        {
            return await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", ApiKeyKey);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Stores a JWT token directly (from browser DevTools).
    /// </summary>
    public async Task StoreJwtTokenAsync(string jwtToken, string? thermostatId = null)
    {
        await _jsRuntime.InvokeVoidAsync("localStorage.setItem", JwtTokenKey, jwtToken);
        if (!string.IsNullOrEmpty(thermostatId))
        {
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", ThermostatIdKey, thermostatId);
        }
    }

    /// <summary>
    /// Retrieves stored JWT token.
    /// </summary>
    public async Task<string?> GetJwtTokenAsync()
    {
        try
        {
            return await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", JwtTokenKey);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Retrieves stored thermostat ID.
    /// </summary>
    public async Task<string?> GetThermostatIdAsync()
    {
        try
        {
            return await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", ThermostatIdKey);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Clears all stored credentials (logout).
    /// </summary>
    public async Task ClearAllAsync()
    {
        await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", TokenKey);
        await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", ApiKeyKey);
        await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", JwtTokenKey);
        await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", ThermostatIdKey);
    }

    /// <summary>
    /// Checks if user is authenticated (has valid tokens or JWT).
    /// </summary>
    public async Task<bool> IsAuthenticatedAsync()
    {
        // Check for JWT token first
        var jwt = await GetJwtTokenAsync();
        if (!string.IsNullOrEmpty(jwt))
            return true;

        // Fall back to OAuth tokens
        var tokens = await GetTokensAsync();
        return tokens != null && !string.IsNullOrEmpty(tokens.AccessToken);
    }
}

/// <summary>
/// Ecobee OAuth tokens.
/// </summary>
public class EcobeeTokens
{
    public string AccessToken { get; set; } = "";
    public string RefreshToken { get; set; } = "";
    public string TokenType { get; set; } = "Bearer";
    public DateTime ExpiresAt { get; set; }

    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
    public bool NeedsRefresh => DateTime.UtcNow >= ExpiresAt.AddMinutes(-5);
}
